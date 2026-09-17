using KOPunisher;

internal static class InventoryTooltipTests
{
    public static int Run()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
        const string potion = "Inventory\nPotion of Soul\nRegular item\nWeight : 1.50\n*1,920 MP Recovery*";
        var regular = InventoryTooltip.Read(potion, potion, false);
        Check(regular.Readable && regular.Name == "Potion of Soul" && regular.Level == null,
            "Recognized regular item is not an unreadable tooltip");
        Check(!InventoryTooltip.NeedsUpgrade(regular, 5), "Regular items cannot enter upgrade queue");
        const string first = "Iron Bow (+1)\nUpgrade Item\nAttack Power: 72\nRequired Dexterity: 94";
        var item = InventoryTooltip.Read(first, first, false);
        Check(item.Readable && item.Name == "Iron Bow" && item.Level == 1 && item.RawText.Contains("Dexterity"), "Fresh tooltip title and raw lines");
        Check(InventoryTooltip.Read(first, first, true).Readable, "A tooltip wins over an incorrect empty reference");
        Check(InventoryTooltip.Read("", "", true).Status.StartsWith("Boş"), "No text AND calibrated blank appearance required");
        Check(!InventoryTooltip.Read("", "", false).Status.StartsWith("Boş"), "No tooltip alone is not empty proof");
        Check(!InventoryTooltip.Read("Attack Power +10", "Attack Power +10", false).Readable, "A stat is not an upgrade title");
        Check(!InventoryTooltip.Read("Iron Bow (+O)", "Iron Bow (+O)", false).Readable, "No guessed OCR numeral correction");
        Check(!InventoryTooltip.Read(first, first.Replace("+1", "+2"), false).Readable, "Inconsistent consecutive readings block selection");
        Check(!InventoryTooltip.Read(first, first.Replace("Iron", "Wooden"), false).Readable, "Unstable item identity blocked");
        Check(!InventoryTooltip.Read(first + "\nSword (+2)", first + "\nSword (+2)", false).Readable, "Multiple tooltip headers are ambiguous");
        Check(!InventoryTooltip.Read(first, "", true).Readable, "Lost tooltip cannot reuse the first result");
        Check(!InventoryTooltip.Read("Iron Bow (+99)", "Iron Bow (+99)", false).Readable, "Out-of-range grade blocked");
        Check(InventoryTooltip.ParseHeader("  Iron   Bow ( + 5 )  ") == ("Iron Bow", 5), "Whitespace normalization only");
        var five = InventoryTooltip.Read(first.Replace("+1", "+5"), first.Replace("+1", "+5"), false);
        Check(InventoryTooltip.NeedsUpgrade(item, 5) && !InventoryTooltip.NeedsUpgrade(five, 5), "Target reached items excluded");
        Check(!InventoryTooltip.NeedsUpgrade(InventoryTooltip.Read("", "", false), 5), "Unknown grade excluded");
        Check(!InventoryTooltip.NeedsUpgrade(item, 0) && !InventoryTooltip.NeedsUpgrade(item, 11), "Target bounds");
        Check(InventoryTooltip.SameItem(item, item) && !InventoryTooltip.SameItem(item, five), "Confirmation checks fresh exact name and grade");
        Check(InventoryTooltip.AfterAttempt(item, item, 5).StartsWith("Dur"), "Unchanged grade does not authorize another attempt");
        Check(InventoryTooltip.AfterAttempt(item, five, 5).StartsWith("Dur"), "Unexpected grade jump requires inspection");
        var four = InventoryTooltip.Read(first.Replace("+1", "+4"), first.Replace("+1", "+4"), false);
        Check(InventoryTooltip.AfterAttempt(four, five, 5) == "Hedefe ulaştı", "Verified final step stops queue");
        var two = InventoryTooltip.Read(first.Replace("+1", "+2"), first.Replace("+1", "+2"), false);
        Check(InventoryTooltip.AfterAttempt(item, two, 5) == "Sonraki tura uygun", "Verified single increment allows later round");
        Check(InventoryTooltip.AfterAttempt(item, InventoryTooltip.Read("", "", true), 5).StartsWith("Dur"), "Empty alone does not prove loss after anvil transfer");
        const string helmet = "Inventory\nPriest Chitin Shell Helmet(+1)\nUpgrade Item\nPriest Armor\nDurability : 10937 / 10937 (100%)\nDefense Ability : 72\nRequired Intelligence : 168 (124)\nItem Grade : High Class\n*Priest Chitin Shell Helmet*";
        var armor = InventoryTooltip.Read(helmet, helmet, false);
        Check(armor.Name == "Priest Chitin Shell Helmet" && armor.Level == 1 && armor.Kind == "Upgrade Item" && armor.Upgradeable,
            "Screenshot helmet transcription: title/grade/type separated from attributes");
        Check(InventoryTooltip.NeedsUpgrade(armor, 5), "Only explicitly typed upgrade gear qualifies");
        foreach (string name in new[] { "Blessed Elemental Scroll", "Blessed Enchant Scroll(HP)" })
        {
            string panel = $"Inventory\n{name}\nRegular item\n*Add HP bonus to the item*\n(Higher success rate)*";
            var scroll = InventoryTooltip.Read(panel, panel, false);
            Check(scroll.Name == name && scroll.Level == null && scroll.Kind == "Regular item" && !scroll.Upgradeable,
                "Screenshot scroll transcription preserves parentheses and has no level");
        }
        const string pathos = "Inventory\nAttack Aurora (Pathos' Glove)-\nLimited Edition\nCospre\nDurability:7200/7200 (100%)\nCospre option : Damage +2% increase\nCospre option : Damage to Priest +4% increase\nExpiration : 2021-04-10 06:47\nCannot be traded or sold";
        var cosmetic = InventoryTooltip.Read(pathos, pathos, false);
        Check(cosmetic.Name == "Attack Aurora (Pathos' Glove)-Limited Edition" && cosmetic.Level == null && cosmetic.Kind == "Cospre",
            "Screenshot multiline cosmetic title does not read damage percentages as levels");
        Check(!InventoryTooltip.NeedsUpgrade(cosmetic, 5), "Cospre never selected for ordinary upgrade");
        Check(!InventoryTooltip.SameItem(armor, armor with { Kind = "Regular item" }), "Confirmation also checks item kind");
        var unknownType = InventoryTooltip.Read("Iron Bow (+1)", "Iron Bow (+1)", false);
        Check(unknownType.Readable && !InventoryTooltip.NeedsUpgrade(unknownType, 5), "Missing type is never inferred from a level");
        const string typedNoLevel = "Inventory\nIron Bow\nUpgrade Item\nWeight : 3";
        var missingGrade = InventoryTooltip.Read(typedNoLevel, typedNoLevel, false);
        Check(missingGrade.Readable && missingGrade.Level == null && !missingGrade.Upgradeable, "Missing grade stays unknown, not zero");
        const string conflict = "Inventory\nIron Bow(+1)\nUpgrade Item\nRegular item";
        Check(!InventoryTooltip.Read(conflict, conflict, false).Readable, "Conflicting type markers are ambiguous");
        Check(!InventoryTooltip.Read(potion, potion.Replace("Regular item", "Cospre"), false).Readable,
            "Changing type between readings is rejected");
        const string junk = "Inventory\nWeight : 1.50\nPotion of Soul\nRegular item";
        Check(!InventoryTooltip.Read(junk, junk, false).Readable, "Background attributes cannot prefix the title");
        const string malformed = "Inventory\nHelmet(+O)\nUpgrade Item";
        Check(!InventoryTooltip.Read(malformed, malformed, false).Readable, "Typed tooltip does not guess OCR O as level zero");
        Console.WriteLine($"PASS {passed} inventory tooltip checks (no Windows mouse/OCR runtime exercised)");
        return passed;
    }
}
