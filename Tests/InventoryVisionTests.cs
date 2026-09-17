using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using KOPunisher;
public static class InventoryVisionTests
{
    private static int count;
    private static void Check(bool ok,string name) { if(!ok)throw new Exception(name);count++; }
    private static (int w,int h,byte[] rgb) Load(string name)
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Fixtures","Inventory",name+".rgb.gz");
        using var f=File.OpenRead(path);using var gz=new GZipStream(f,CompressionMode.Decompress);using var r=new BinaryReader(gz);
        int w=r.ReadInt32(),h=r.ReadInt32();return(w,h,r.ReadBytes(checked(w*h*3)));
    }
    private static (int w,int h,byte[] rgb) Transform((int w,int h,byte[] rgb) f,double s,int dx,int dy)
    {
        int w=(int)(f.w*s)+dx,h=(int)(f.h*s)+dy;byte[] a=new byte[w*h*3];
        for(int y=dy;y<h;y++)for(int x=dx;x<w;x++)
        {int source=((int)((y-dy)/s)*f.w+(int)((x-dx)/s))*3;Buffer.BlockCopy(f.rgb,source,a,(y*w+x)*3,3);}
        return(w,h,a);
    }
    private static Rectangle? Tip((int w,int h,byte[] rgb) f)=>InventoryVision.FindTooltip(f.w,f.h,f.rgb);
    private static InventoryDetection? Inv((int w,int h,byte[] rgb) f)=>InventoryVision.FindInventory(f.w,f.h,f.rgb);
    private static (int w,int h,byte[] rgb) Crop((int w,int h,byte[] rgb) f,Rectangle b)
    {
        byte[] a=new byte[b.Width*b.Height*3];
        for(int y=0;y<b.Height;y++)Buffer.BlockCopy(f.rgb,((b.Y+y)*f.w+b.X)*3,a,y*b.Width*3,b.Width*3);
        return(b.Width,b.Height,a);
    }
    public static int Run()
    {
        count=0;
        foreach(string name in new[]{"elemental","no-tooltip","helmet","potion","enchant-hp","pathos"})
        {
            var f=Load(name);var tip=Tip(f);var inv=Inv(f);
            Console.WriteLine($"vision {name}: inventory={inv}, tooltip={tip}");
            Check((tip!=null)==(name!="no-tooltip"),name+" tooltip presence");
            Check((inv!=null)==(name!="pathos"),name+" inventory presence");
            if(inv!=null)Check(Math.Abs(inv.Bounds.X-1552)<=2&&Math.Abs(inv.Bounds.Y-418)<=2&&inv.Columns==7&&inv.Rows==4,name+" grid bounds");
            if(tip is Rectangle b)
            {
                Check(b.Width>=300&&b.Height>=170,name+" tooltip bounds");
                Check(InventoryVision.FindTooltip(f.w,f.h,f.rgb,new Point(b.Right+30,b.Bottom+30))==tip,name+" hover left/above");
                var moved=Transform(f,1,23,17);var mt=Tip(moved);
                Check(mt!=null&&mt.Value.X==b.X+23&&mt.Value.Y==b.Y+17,name+" translated tooltip");
            }
        }
        var clean=Load("no-tooltip");
        foreach(double s in new[]{1.25,1.5,2.0})
        {
            var resized=Transform(clean,s,16,12);var inv=Inv(resized);
            Console.WriteLine($"vision scale {s}: {inv}");
            Check(inv!=null,"scaled inventory "+s);
            var tp=Transform(Load("pathos"),s,16,12);Check(Tip(tp)!=null,"scaled tooltip "+s);
        }
        var p=Load("pathos");
        byte[] clipped=new byte[p.w*270*3];Buffer.BlockCopy(p.rgb,0,clipped,0,clipped.Length);
        Check(InventoryVision.FindTooltip(p.w,270,clipped)!=null,"bottom clipped tooltip");
        Check(InventoryVision.FindInventory(400,400,new byte[400*400*3])==null,"unknown black inventory");
        Check(InventoryVision.FindTooltip(400,400,new byte[400*400*3])==null,"unknown black tooltip");
        Check(!InventoryVision.IsEmptySlot(49,49,new byte[49*49*3],new Rectangle(0,0,49,49)),"black is not empty texture");
        for(int row=0;row<4;row++)for(int col=0;col<7;col++)
        {
            var slot=new Rectangle(1552+49*col,418+49*row,49,49);
            bool empty=InventoryVision.IsEmptySlot(clean.w,clean.h,clean.rgb,slot);
            if(row==0)Check(!empty,"item not empty "+col);
        }
        foreach(Action action in new Action[]{()=>InventoryVision.FindInventory(1,1,new byte[2]),()=>InventoryVision.FindTooltip(-1,1,Array.Empty<byte>()),()=>InventoryVision.IsEmptySlot(1,1,new byte[3],new Rectangle(-1,0,1,1)),()=>InventoryVision.FindTooltip(int.MaxValue,int.MaxValue,Array.Empty<byte>()),()=>InventoryVision.FindInventory(1,1,null!)})
        {bool threw=false;try{action();}catch(ArgumentException){threw=true;}Check(threw,"malformed rejected");}
        Check(InventoryVision.IsEmptySlot(clean.w,clean.h,clean.rgb,new Rectangle(1552,565,49,49)),"known empty full texture");
        byte[] darkItem=(byte[])clean.rgb.Clone();
        darkItem[((565+24)*clean.w+1552+24)*3]=35;
        Check(!InventoryVision.IsEmptySlot(clean.w,clean.h,darkItem,new Rectangle(1552,565,49,49)),"small dark item mark is not empty");
        var cropped=Crop(clean,new Rectangle(1500,70,420,570));var ci=Inv(cropped);
        Check(ci!=null&&ci.Bounds.X==52&&ci.Bounds.Y==348,"cropped inventory without Bags");
        byte[] noGrid=(byte[])clean.rgb.Clone();
        for(int y=418;y<615;y++)Array.Clear(noGrid,(y*clean.w+1550)*3,350*3);
        Check(InventoryVision.FindInventory(clean.w,clean.h,noGrid)==null,"header without lattice rejected");
        var duplicate=Transform(clean,1,clean.w,0);
        for(int y=0;y<clean.h;y++)Buffer.BlockCopy(clean.rgb,y*clean.w*3,duplicate.rgb,y*duplicate.w*3,clean.w*3);
        Check(Inv(duplicate)==null,"two inventories ambiguous");
        var doubleTip=Transform(p,1,p.w+30,0);
        for(int y=0;y<p.h;y++)Buffer.BlockCopy(p.rgb,y*p.w*3,doubleTip.rgb,y*doubleTip.w*3,p.w*3);
        Check(Tip(doubleTip)==null,"two tooltips ambiguous");
        Check(Inv(Transform(clean,.75,0,0))==null,"unsupported inventory downscale fails closed");
        foreach(string name in new[]{"elemental","helmet","potion","enchant-hp"})
        {
            var f=Load(name);var b=Tip(f)!.Value;
            var c=Crop(f,b);Check(Tip(c)!=null,name+" tooltip tight crop");
        }
        byte[] noIcon=(byte[])p.rgb.Clone();
        for(int y=30;y<75;y++)Array.Clear(noIcon,(y*p.w+10)*3,45*3);
        Check(InventoryVision.FindTooltip(p.w,p.h,noIcon)==null,"rules and text without icon rejected");
        byte[] fake=new byte[400*300*3];
        foreach(int y in new[]{30,31,125,126,220})for(int x=10;x<300;x++)
        {int q=(y*400+x)*3;fake[q]=136;fake[q+1]=119;fake[q+2]=68;}
        Check(InventoryVision.FindTooltip(400,300,fake)==null,"gold rules on black are not tooltip");
        foreach(Action action in new Action[]{
            ()=>InventoryVision.FindTooltip(1,1,new byte[2]),
            ()=>InventoryVision.IsEmptySlot(1,1,new byte[4],new Rectangle(0,0,1,1)),
            ()=>InventoryVision.IsEmptySlot(1,1,new byte[3],new Rectangle(int.MaxValue,0,49,49)),
            ()=>InventoryVision.FindInventory(int.MaxValue,int.MaxValue,Array.Empty<byte>()),
            ()=>InventoryVision.FindTooltip(1,1,null!),
            ()=>InventoryVision.IsEmptySlot(1,1,null!,new Rectangle(0,0,1,1))})
        {
            bool threw=false;try{action();}catch(ArgumentException){threw=true;}Check(threw,"malformed API input");
        }
        var wide = (w: p.w + 140, h: p.h, rgb: new byte[(p.w + 140) * p.h * 3]);
        for (int y = 0; y < p.h; y++)
        {
            int split = p.w - 30;
            Buffer.BlockCopy(p.rgb, y * p.w * 3, wide.rgb, y * wide.w * 3, split * 3);
            for (int x = 0; x < 140; x++)
                Buffer.BlockCopy(p.rgb, (y * p.w + split) * 3, wide.rgb, (y * wide.w + split + x) * 3, 3);
            Buffer.BlockCopy(p.rgb, (y * p.w + split) * 3, wide.rgb, (y * wide.w + split + 140) * 3, 30 * 3);
        }
        Check(Tip(wide) is Rectangle wideBounds && wideBounds.Width >= 450, "wide tooltip retains native icon scale");
        var live = Load("live-inventory");
        for (int slot = 1; slot <= 28; slot++)
        {
            bool expectedEmpty = slot is >= 10 and <= 16 or 22 or 23;
            var bounds = new Rectangle((slot - 1) % 7 * 49, (slot - 1) / 7 * 49, 49, 49);
            Check(InventoryVision.IsEmptySlot(live.w, live.h, live.rgb, bounds) == expectedEmpty, "live slot emptiness " + slot);
        }
        return count;
    }
}
