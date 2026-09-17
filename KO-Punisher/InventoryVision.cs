using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace KOPunisher;

public sealed record InventoryDetection(Rectangle Bounds, int Columns, int Rows);

/// <summary>Fail-closed recognizer for the supplied gold/teal skin; consumes packed RGB, never OCR.</summary>
public static class InventoryVision
{
    private static readonly double[] Scales = { 1, 1.25, 1.5, 2 };
    private static readonly int[] Header =
    {
        4,4,8,1,1,14,4,117,83,4,24,4,124,88,4,34,4,135,95,4,44,4,143,102,4,
        54,4,151,109,4,64,4,159,115,5,74,4,167,121,6,84,4,176,128,7,94,4,182,133,7,
        104,4,178,129,7,114,4,170,124,7,124,4,163,118,6,134,4,157,113,5,144,4,150,107,4,
        154,4,145,105,4,164,4,137,97,4,174,4,131,94,4,184,4,124,89,4,194,4,95,67,3,
        4,9,144,100,8,14,9,153,109,8,24,9,138,92,7,34,9,152,108,8,44,9,152,108,8,
        54,9,150,106,8,64,9,152,108,8,74,9,152,108,8,84,9,152,108,8,94,9,151,107,8,
        104,9,151,107,8,114,9,152,108,8,124,9,151,107,8,134,9,152,108,8,144,9,152,108,8,
        154,9,151,107,8,164,9,151,107,8,174,9,152,108,8,184,9,152,108,8,194,9,152,108,8,
        4,14,110,72,9,14,14,106,69,8,24,14,109,71,9,34,14,111,72,9,44,14,114,74,9,
        54,14,112,73,9,64,14,113,73,9,74,14,119,77,10,84,14,112,70,9,94,14,114,72,9,
        104,14,116,75,9,114,14,126,81,10,124,14,118,75,10,134,14,118,76,10,144,14,108,70,9,
        154,14,111,72,9,164,14,109,71,9,174,14,109,71,9,184,14,112,73,9,194,14,50,37,10,
        4,19,88,54,8,14,19,73,44,6,24,19,89,55,8,34,19,90,55,8,44,19,86,53,8,
        54,19,88,54,8,64,19,88,54,8,74,19,96,58,8,84,19,176,184,104,94,19,16,10,1,
        104,19,98,103,59,114,19,94,100,58,124,19,202,216,125,134,19,85,52,8,144,19,92,57,8,
        154,19,88,54,8,164,19,89,55,8,174,19,89,55,8,184,19,87,54,8,194,19,53,38,13,
        4,24,66,37,7,14,24,64,35,7,24,24,66,37,7,34,24,64,35,7,44,24,65,36,7,
        54,24,65,36,7,64,24,67,37,7,74,24,71,39,7,84,24,70,38,7,94,24,67,37,7,
        104,24,69,38,7,114,24,70,39,7,124,24,66,37,7,134,24,65,36,7,144,24,67,37,7,
        154,24,66,37,7,164,24,66,37,7,174,24,67,37,7,184,24,67,37,7,194,24,67,37,7,
        4,29,40,10,5,14,29,205,152,2,24,29,205,152,2,34,29,205,152,2,44,29,205,152,2,
        54,29,205,152,2,64,29,205,152,2,74,29,205,152,2,84,29,205,152,2,94,29,205,152,2,
        104,29,205,152,2,114,29,205,152,2,124,29,205,152,2,134,29,205,152,2,144,29,205,152,2,
        154,29,205,152,2,164,29,205,152,2,174,29,205,152,2,184,29,205,152,2,194,29,205,152,2,
        4,34,28,21,6,14,34,71,46,6,24,34,71,46,6,34,34,71,46,6,44,34,71,46,6,
        54,34,71,46,6,64,34,71,46,6,74,34,71,46,6,84,34,71,46,6,94,34,71,46,6,
        104,34,71,46,6,114,34,71,46,6,124,34,71,46,6,134,34,71,46,6,144,34,71,46,6,
        154,34,71,46,6,164,34,71,46,6,174,34,71,46,6,184,34,71,46,6,194,34,71,46,6,
        62,15,111,70,9,64,15,103,65,9,66,15,112,71,9,68,15,118,74,10,70,15,110,69,9,
        72,15,120,75,10,74,15,119,75,10,76,15,114,71,10,78,15,0,0,0,80,15,168,179,103,
        82,15,23,15,2,84,15,0,0,0,86,15,0,0,0,88,15,0,0,0,90,15,0,0,0,
        92,15,32,20,3,94,15,34,21,3,96,15,0,0,0,98,15,0,0,0,100,15,23,15,2,
        102,15,0,0,0,104,15,0,0,0,106,15,10,6,1,108,15,168,179,103,110,15,0,0,0,
        112,15,0,0,0,114,15,0,0,0,116,15,77,48,6,118,15,0,0,0,120,15,0,0,0,
        122,15,0,0,0,124,15,34,21,3,126,15,33,21,3,128,15,112,69,9,130,15,126,79,11,
        132,15,119,75,10,134,15,107,68,9,136,15,106,67,9,138,15,108,68,9,140,15,106,68,9,
        142,15,100,64,8,144,15,101,64,8,62,17,89,55,8,64,17,94,58,9,66,17,94,58,9,
        68,17,103,63,9,70,17,73,44,7,72,17,103,63,9,74,17,101,62,9,76,17,116,71,10,
        78,17,40,25,4,80,17,179,186,105,82,17,42,26,4,84,17,202,216,125,86,17,94,100,58,
        88,17,0,0,0,90,17,133,142,82,92,17,67,66,35,94,17,5,3,1,96,17,94,100,58,
        98,17,0,0,0,100,17,0,0,0,102,17,202,216,125,104,17,94,100,58,106,17,2,1,0,
        108,17,168,179,103,110,17,0,0,0,112,17,168,179,103,114,17,94,100,58,116,17,5,3,1,
        118,17,202,216,125,120,17,0,0,0,122,17,168,179,103,124,17,64,63,34,126,17,21,13,2,
        128,17,118,72,11,130,17,106,64,10,132,17,112,69,10,134,17,103,63,9,136,17,107,66,10,
        138,17,101,62,9,140,17,100,62,9,142,17,99,61,9,144,17,95,59,9,62,19,87,54,8,
        64,19,88,54,8,66,19,90,55,8,68,19,97,59,9,70,19,90,55,8,72,19,83,51,8,
        74,19,96,58,8,76,19,102,62,9,78,19,0,0,0,80,19,168,179,103,82,19,11,7,1,
        84,19,176,184,104,86,19,99,103,59,88,19,9,5,1,90,19,133,142,82,92,19,202,216,125,
        94,19,16,10,1,96,19,94,100,58,98,19,0,0,0,100,19,0,0,0,102,19,177,185,104,
        104,19,98,103,59,106,19,5,3,0,108,19,168,179,103,110,19,0,0,0,112,19,168,179,103,
        114,19,94,100,58,116,19,4,2,0,118,19,177,185,104,120,19,86,52,8,122,19,133,142,82,
        124,19,202,216,125,126,19,73,44,6,128,19,89,54,8,130,19,91,55,8,132,19,82,50,7,
        134,19,85,52,8,136,19,94,57,8,138,19,95,58,8,140,19,93,57,8,142,19,89,54,8,
        144,19,92,57,8,62,21,83,49,8,64,21,83,49,8,66,21,84,49,8,68,21,85,50,8,
        70,21,84,49,8,72,21,73,43,7,74,21,81,47,8,76,21,86,50,9,78,21,0,0,0,
        80,21,0,0,0,82,21,14,8,1,84,21,47,27,5,86,21,25,15,2,88,21,23,14,2,
        90,21,0,0,0,92,21,0,0,0,94,21,83,49,8,96,21,0,0,0,98,21,0,0,0,
        100,21,0,0,0,102,21,51,30,5,104,21,23,14,2,106,21,24,14,2,108,21,0,0,0,
        110,21,0,0,0,112,21,0,0,0,114,21,0,0,0,116,21,43,25,4,118,21,47,28,5,
        120,21,81,48,8,122,21,53,57,33,124,21,0,0,0,126,21,83,49,8,128,21,82,48,8,
        130,21,84,50,8,132,21,90,53,9,134,21,84,49,8,136,21,87,51,9,138,21,83,49,8,
        140,21,84,50,8,142,21,81,48,8,144,21,82,48,8,62,23,72,41,7,64,23,73,41,7,
        66,23,74,42,7,68,23,72,41,7,70,23,73,41,7,72,23,74,42,7,74,23,76,43,7,
        76,23,76,43,7,78,23,78,44,8,80,23,79,44,8,82,23,82,46,8,84,23,54,30,5,
        86,23,73,41,7,88,23,76,43,7,90,23,75,43,7,92,23,74,42,7,94,23,73,41,7,
        96,23,72,41,7,98,23,72,41,7,100,23,73,42,7,102,23,73,41,7,104,23,75,42,7,
        106,23,69,39,7,108,23,77,43,7,110,23,79,44,7,112,23,62,35,6,114,23,73,41,7,
        116,23,63,36,6,118,23,66,38,6,120,23,71,40,7,122,23,42,24,4,124,23,43,25,4,
        126,23,72,41,7,128,23,72,41,7,130,23,67,38,6,132,23,68,38,7,134,23,70,39,7,
        136,23,78,44,7,138,23,72,41,7,140,23,73,42,7,142,23,73,41,7,144,23,72,41,7,
        62,25,63,34,7,64,25,63,34,7,66,25,63,34,7,68,25,63,34,7,70,25,64,34,7,
        72,25,65,35,7,74,25,65,35,7,76,25,68,36,7,78,25,67,36,7,80,25,68,36,7,
        82,25,68,37,7,84,25,68,36,8,86,25,65,35,7,88,25,59,31,7,90,25,64,34,7,
        92,25,64,34,7,94,25,62,34,7,96,25,63,34,7,98,25,63,34,7,100,25,63,34,7,
        102,25,63,34,7,104,25,64,35,7,106,25,66,35,7,108,25,67,36,7,110,25,64,34,7,
        112,25,64,34,7,114,25,65,35,7,116,25,65,35,7,118,25,62,33,7,120,25,62,33,7,
        122,25,63,34,7,124,25,63,34,7,126,25,63,34,7,128,25,63,34,7,130,25,62,33,7,
        132,25,61,33,7,134,25,63,34,7,136,25,64,34,7,138,25,64,34,7,140,25,63,34,7,
        142,25,63,34,7,144,25,63,34,7
    };
    private static readonly int[] Lattice =
    {
        3,0,1,1,0,23,0,1,1,0,45,0,1,1,0,52,0,1,1,0,72,0,1,1,0,
        94,0,1,1,0,101,0,1,1,0,121,0,1,1,0,143,0,1,1,0,150,0,1,1,0,
        170,0,1,1,0,192,0,1,1,0,199,0,1,1,0,219,0,1,1,0,241,0,1,1,0,
        248,0,1,1,0,268,0,1,1,0,290,0,1,1,0,297,0,1,1,0,317,0,1,1,0,
        339,0,1,1,0,3,49,1,1,0,23,49,1,1,0,45,49,1,1,0,52,49,1,1,0,
        72,49,1,1,0,94,49,1,1,0,101,49,1,1,0,121,49,1,1,0,143,49,1,1,0,
        150,49,1,1,0,170,49,1,1,0,192,49,1,1,0,199,49,1,1,0,219,49,1,1,0,
        241,49,1,1,0,248,49,1,1,0,268,49,1,1,0,290,49,1,1,0,297,49,1,1,0,
        317,49,1,1,0,339,49,1,1,0,3,98,1,1,0,23,98,1,1,0,45,98,1,1,0,
        52,98,1,1,0,72,98,1,1,0,94,98,1,1,0,101,98,1,1,0,121,98,1,1,0,
        143,98,1,1,0,150,98,1,1,0,170,98,1,1,0,192,98,1,1,0,199,98,1,1,0,
        219,98,1,1,0,241,98,1,1,0,248,98,1,1,0,268,98,1,1,0,290,98,1,1,0,
        297,98,1,1,0,317,98,1,1,0,339,98,1,1,0,3,147,1,1,0,23,147,1,1,0,
        45,147,1,1,0,52,147,1,1,0,72,147,1,1,0,94,147,1,1,0,101,147,1,1,0,
        121,147,1,1,0,143,147,1,1,0,150,147,1,1,0,170,147,1,1,0,192,147,1,1,0,
        199,147,1,1,0,219,147,1,1,0,241,147,1,1,0,248,147,1,1,0,268,147,1,1,0,
        290,147,1,1,0,297,147,1,1,0,317,147,1,1,0,339,147,1,1,0,3,196,50,49,43,
        23,196,6,6,3,45,196,7,7,8,52,196,10,8,7,72,196,8,9,3,94,196,11,11,7,
        101,196,10,9,8,121,196,11,6,3,143,196,9,7,5,150,196,8,9,3,170,196,7,8,2,
        192,196,9,7,3,199,196,11,6,4,219,196,10,10,2,241,196,6,6,3,248,196,12,11,4,
        268,196,8,10,6,290,196,9,8,4,297,196,7,8,3,317,196,7,7,8,339,196,1,2,1
    };
    private static readonly int[] Seams =
    {
        0,10,30,28,20,0,25,30,28,20,0,40,30,28,20,49,10,44,42,29,49,25,44,42,29,
        49,40,44,42,29,98,10,45,43,29,98,25,44,42,29,98,40,44,42,29,147,10,45,43,29,
        147,25,44,42,29,147,40,44,42,29,196,10,45,44,30,196,25,44,43,30,196,40,44,43,30,
        245,10,45,43,29,245,25,43,41,29,245,40,43,41,29,294,10,42,41,27,294,25,43,41,28,
        294,40,43,41,28,343,10,28,27,18,343,25,29,28,19,343,40,28,27,18,0,59,30,28,20,
        0,74,30,28,20,0,89,30,29,20,49,59,44,42,29,49,74,45,43,30,49,89,44,42,29,
        98,59,44,42,29,98,74,43,42,29,98,89,44,42,29,147,59,43,42,29,147,74,43,42,29,
        147,89,44,42,29,196,59,44,43,30,196,74,44,43,30,196,89,44,42,30,245,59,44,42,29,
        245,74,44,42,29,245,89,44,42,29,294,59,43,41,28,294,74,43,41,28,294,89,43,41,28,
        343,59,27,27,18,343,74,29,27,19,343,89,30,28,19,0,108,29,28,20,0,123,29,28,20,
        0,138,29,28,20,49,108,44,42,29,49,123,44,42,29,49,138,43,42,29,98,108,44,42,29,
        98,123,44,42,29,98,138,43,42,29,147,108,44,42,29,147,123,44,42,29,147,138,43,42,29,
        196,108,44,43,30,196,123,44,43,30,196,138,44,42,30,245,108,44,42,29,245,123,44,42,29,
        245,138,43,42,29,294,108,43,41,27,294,123,42,40,27,294,138,43,40,27,343,108,29,28,19,
        343,123,29,27,19,343,138,29,27,18,0,157,30,28,20,0,172,30,29,20,0,187,29,28,20,
        49,157,44,42,29,49,172,43,42,29,49,187,43,42,29,98,157,44,42,29,98,172,44,42,29,
        98,187,44,42,29,147,157,43,42,29,147,172,44,42,29,147,187,43,42,29,196,157,44,42,30,
        196,172,44,43,30,196,187,44,42,30,245,157,43,41,29,245,172,43,42,29,245,187,43,42,29,
        294,157,42,40,27,294,172,43,41,27,294,187,42,40,27,343,157,31,31,20,343,172,30,30,19,
        343,187,23,22,13
    };
    private static readonly byte[] EmptyPixels = Convert.FromBase64String("FBQPFBQQFBQPExMPExMPFRUQFxcSFBQQFBQQFBQRFRURFBQQExMQFBMQFRUQExMQFBQQFBUQExMQFRURFRURFBURFBQQFBQQExMQFBQQFRQRFhYQFhYQFhQQFxYQFBQQFBQQFBQPFBQQFBQQFRQQFBMPEhIOEhIOEhIOEhIOEhIOExIOEREOFBMPExMPEhIPExIPExIPExIPExIPExIPEhIPEhIPEhIPEhIPExIOExIPEhIPExIPEhIPEhIPEhIPEhIPExMPFRMPFRQPFhUPExIPEhIPEhEOEhIOExIOERIOFBIOERENEA8NERANERENERENEBAMERENERANEBANEhINERENERENERENEA8NEBENERENEhMOERENERENERINERENEBAMERENERENERENERENERENERIOFBMOFBIOExMOEREOEhIOEA8NERANEBENERANEhENDg4LDw8MDg4LDg0LEBAMDw8LEBAMDw4LDg0KEA8LDw4LEA8MDw0LDg0KEQ8MDw4KDw8LDw8LDw8LDw8LEBALDw8LEBAMEBAMDw8LDw8LEBAMEBAMFBENEhIMEhIMEBAMDg4LEA8MDg4LDw8MDw8LEBAMDQ0KDQ0KDw4LDw4LDg0KDg0LDgwKDg0KDg0KDg0KDQ0KDAwJDg0KDAwKDQwKDg0KDQwKDQ0JDg0JDg0KDw4KDw4KDg4KDg0KDg0KDg0KDg0KDg0KDg0JDg4LDg4LDg4KDgwKDg0KDg4LDw8LDQ4LDw8KDAwICwsIDg0KDQ0JDQ0JDAsJDAsJDAwJDQ0JDAwJDAsJDAsIDAwJDAsJDQwJDAsJDQwJDAsJCwsJDQ0JDQ0JDg0JDg0JDQ0JDQ0JDQ0JDQ0JDg4JDQ0JDQ0JDQ0JDQ0JDAwIDAsIDQ4IDQ0JDQwJDQ4JCwsIDQ0JDAwJDQ0ICwsIDAwICwsICwsIDAsICwoHCwoICwoICwsICwsICwoHCwsICwoHCwoHCwoICwsHDAwIDAwIDAwIDw4IDAwIDAwIDAwIDAwIDAwIDAwIDAwIDQ0ICwsIDAwICwoIDAwIDAwIDAwHCgsHDQ0JDA0JDAwICgoHCwoHDQwICwsIDAsIDAsICgkHDAsIDAsICwoHCgoHDAsHCwoHCwoHCwoICwoHDQwHDAwHDAwHDAwHDAwHDAwHCwoGDAoHDAwIDQsHDAwICwwICwoHCgsHDAwIDAwICwsIDAwHCgoHCgsHCgsHCgoGCQkGCQkGCQkGCwoHCwoHDAsHCwkHCwoHCgkGCQkGCQkGCQkGCQkGCgkHDAsHCgoGCwsGCwoHCgsGDAsHCwsGCwsGCwwHCQkGCgkHCwoHCQkGCgkGCgkGCwsHCwsHCwsHCwoHDAsHCQkGCAgFCQkFCAgFCAgFCQgFCgkGCwoHDAoHCgkGCggGCQgGCQgFCgkGCQgFCQgFCAgFCQkGCwsHCgsGCgoHCgkFCwsHCwsGCgoFCgoFCwsGCgkGCgkGCgkGCgkFCQoHCQkFCwkHCQoGCQgGCQkFCQkGCgoGCAgFCQgFCQgFCAgFCAgFCQkGCwkGCgkGCgkGCgkGCQgFCQgFCQgFCAgFCggFCQgFCggFCgkGCgkGCgoGCwoHCgoGCgoGCgsGCwsGCgoFCQkGCQoGCgkGCgoGCggFCggFCgoGCgkGCQkGCQgFCQkGCQkGCQkFCAgFCQgFCQgFCAgGCQkGCQkGCgkGCQoGCAgFCwkFCQgFCQgGCQgFCQgFCQkFCQgFCgoGCgoGCgoFCgsGCgoFCgoFCgsGCgoGCgoFCwoGCQkFCgkGCgoGCgoGCgoGCgoGCgoGCwoGCQgFCQkGCQkGCAgFCQkGCAgFCQgFCAgFCQkGCQgGCAgGCAgFBwcECAgFCAcFCAgFCQgFCQgFCQkGCQgFCQgGCAgGCgsGCwsHCwoGCwsGCQkFCwsGCgoGCgoGCgkGCgoGCwoGCgoGCgoGCgoGCgoGCgoGCgoFCQkGCQkFCQgFCQcECQcEBwcECAgECAgEBwcECAgFCAgFCAkFCQcECQcEBwcECAcEBwcECgkFCAgECAgFCAcFBwcFCQoFCQkFCQkFCQkFCQkFCQkFCQkFCAkFCQkFCQkFCQkFCQkFCgoFCgoFCQkFCggFCQgFCAgFCQcEBwcEBwcECAgEBwcEBwcEBwcEBwcEBwcEBwcECQcFCgkFBwcECQkFCAgFCAcFBwcFCQgFCAcFBwcFCQkFCQkFCQkFCQkFCQgFCQkFCQkFCQkFCQkFCgoFCAgECAkFCQkFCgoFCgoFCQgFCQkFCAgFCAcEBwcEBwcECAcEBwcECAgEBwcEBwcEBwcEBwcECAkFCAgECAgECAcEBwcECAcFCAcFCAcECQkFCAgFBwcFCQkFCgoFCgoFCQkFCQkFCQkFCQkFCQoFCQkFCQkFCQkFCgkFCQkFCQkFCgoFCAgFCAgFCQcECAcEBwcECAgEBgcECAgEBwcEBgcECAcFCQgGCgoFCAgECAcECQgFBwcECAcFCAgFCQkFCAcFCAcECAcEBwcECAgFCgoFCQkFCwkFCQkFCgkFCgoFCggECQkFCQgFCAgECQkFCAcECAgFCAgFCAgFCAcEBwcEBwcEBwcFBwcDCAcECAcECAcECQkFCgoFCQkFCQgFBwgEBwcEBwcECQkFCQkFCQkFCQkFCgoFCQkFCQkFCQkFCgkGCQkGCwkFCQkFCQkECwsFCQgFCwsFCQgFCQgFCAcFCAcFCQkFCAgFCAgFCAcECAgEBwcECgoFCQgECQkFCAcFBwcEBwcFCQkFCQkFCgkFCgoGCgkFCgkFCQkFCQkFCQkFCQkFCQkFCQkFCQkFCQkFCgsGCgoGCQkFCQkFCQkFCQkFCgoFCQcFCAgECQgFCQgFCQgFCgkFCAgFCQkFCAcEBwcECAgECQkFCQkFCQkECAcFBwcECAgECgoFCQkFCgoFCgkFCQkFCAcECQcECAgECgkFCgkFCAkFCQkFCQkFCgoGCgkGCgoGCQgFCAcFCQkFCQkFCQkFCAcFCQkFBwcFCQcFBwcECAgFCAgFCQgFCQgEBwcFCQkFCQkFCQkFCQoFBwcEBwcECQkFCAkFCQkFCQkFCgkFCQkFCQkFCgkFCQgFCQkFCQkFCgkFCQkFCQkFCgkFCQkFCgkFCAcFCQgFCgkFCgkFCQkFCgkFBwcFCAcFCAcFCAcECAgFCQkFCQkGBwcECgkFCQkFCQoFCwkFCQoFBgcEBwcECQgFCAcFCQkFCQgFCQkFCgoFCQkFCQkFCQgFCQkFCgoFCQkFCQkECQkFCQkFCQkFCQkFCQgFCQcFCQkFCQkFCAgFBwcECAcFCQgECAcFCAcFCAgFCAgFCAgFCgkFCQkFCQoFCgoFCgoFBwcFBwcECAcEBwgFCgcECQkFCQkGCQkFCQkFCAcFCQgFCgoFCgoFCQkFCQgFCQgFCQgECQkFCgoFCAkFCAcFCgoFCQkFCQkFCQkFBwcECAcECAcECAgFBwcFCQkFCAgFCgkGCQkFCQkFCQoGCwsFBwcFCAgFCAgFCAgFCAcECQcFCgoGCQkGCQoFCAcECQcFCQkFCQkFCgkFCgkFCQkFCQkFCQgFCQkFCAkECQkFCQkFCgoFCQkFCQkFCQkFCAgFCAcECQgECAcFBwcECQkFCQkFCAgFCQkFCQkFCgoGCQkFCAgFCAcFCQgFCAcFBwcECAgFCAcFCAgFCQkGCAgFCgkFCQkFCAcFCQkFCQgFCAcFCQcECAcECQkFCQkFCgkFCQkFCQkFCQkFCQkFCgkFCQkFCAcFBwgECAcFCAgECAgFCAgFCAgFCQkGCgoFCgsGCAgECAcFCAgFCAcFCAcFBwcECAgFCgkFCAgFCQoGCAcFCAgFCQkFCAgFCQkFCgkFCQcEBwcEBwcECQkFCQgECgkFCQkFCQkFCQkFCgkFCQkFCgkGCAcECAcFCAgFBwcFCAgFCAgFCAkFCQkGCQkFCQoFCQcECAgFCAgFCAcFCAcFBwcECQkFCQkFCQgFCQkFCQgFCQkFCQkFCQkFCQkFCQkFBwcFBwcECAkECQkFCQkECQkECQkFCQkFCQkFCQkFCQoFCQoFCgoFCAcFCAcFCAcECQgFCAgFCAkFCAgFCAcFCAkFCQkGCAgFCggFCAcFBwcEBwcECQkFCQgFCQkFCQkFCQgFCQgFCggFCQkFCQkFCgkFCQcFBwcECgoECQgECQkECQkECQkFBwcECgkFCQkFCgoFCgoGCgoFCQgFBwcFCAgECAgFCAgFCAgFCQkFCQkFCQkGBwcECAgEBwcEBwcECAcEBwcECgkFCgkFCQoGCgoGCggFCQkFCQkFCQkFCQkFCQkFCQkFCAkFCwoFCgoFCwsGCgkFCQkFBwcECAcEBwYECAcFCQkECgoFCwoGCggFCAcFCQgFCAgFBwgFCQkEBwcECQgFCAgFBgcECQgEBwgFCAcECQgFCQkFCQkFCgkGCgkGCgkFCQkFCQkFCQgFCQgFCQcECQkFCgkFCgoGCgoFCgoGCQkGCQkFCQkGCAgFCAgECgoFCQkGCgoFCQoFCgoFCQkFCQgFCQkGCAgFCAgFCAcECAgEBwcEBwgEBwcEBwcEBwcECAcFCQcFCQkFCQkFCgkFCgkFCgoFCQkFCAcECQcECQgFCQkFCQkFCwoFCgoFCwoFCQkFCwoFCgoFCQkFCgoGCgkGCgoGCQkGCQkGCQkGCQkFCAgFCAgFCAgFCAcEBgYDCgoGCgoGCQkGCQkFCAcECAcECQgFCQcFCgkGCgkGCQcFCQkFCAcFCQcFCQcFCggFCgkFCQkGCQkFCgoFCgkFCQkFCQkGCgoGCgkGCwsGCQoGCQkGCwoGCQkGCQkGCQkGCQkFCQgFCQgFCAgFCQcECQkGCgkGCgoGCwoGCQkFCQkFCQkFCgoFCQkFCgkGCQgFCgoFCAcFCQcFCQgFCQgFCQkFCgoFCQkFCgkFCQkFCwoFCQkFCgoFDA0ICQkGCwsHCgoGCQkFCgoGCgoGCQkGCQkFCAkFCQgFCQkFCAgFCQkFCQkFCQkFCQkGCQkGCgkGCQkFCgkGCgkGCgoFCQkFCQkFCAcECAcFCAcFCAcECgoGCQkGCgkGCgoFCQkFCggFCQkFCQoFCgoGCwoHCgoGCwsGCwsGCQkGCgoGCQkGCQoGCQkFCAgFCAgFCAgFCAgFCQkFCQkFCgkFCgkFCQkGCgkGCQkFCgkGCgkGCgoGCwoGCgoGCgkGCAgFCQgFCQgFCQkGCQkGCQkGCQkGCQkFCQkGCQkFCgoGCgoGCgoGCgoGCgoGCQkFCgoFCQkFCQkFCAkFCgkFCAgECQgFCAgFCAgFCggFCQkFCgoFCQkFCQkGCgkGCQkFCgoGCgkGCwoGCgkGCgoFCQkFCQkGCQkFCQkGCQkGCQkFCgoFCQkGCQkFCQkFCQkFCQkGCQoGCgoGCQkGCgkGCQkGCAcFCQoFCQkFCQkGCgoFCAgFCAgFCAgFCgoGCgkGCQgFCQkFCgkFBwcECQgFCQkFCQkFCQkGCQkFCQkFCQkFCQkFCQkFCQkFCQkGCQkFCQkFCQkFCQkFCQkFCQkFCQkFCgkGCgkGCQkFCgoGCAkFBwcFCAcFBwcFCAkECAgECAkFCAgFCAgFCQkFCQoGCgoGCgkGCgkGCQgFCQkFCAgFCQgFCQgFCgkGCQkGCgkGCQkGCQkGCQkGCAgFCQgFCgkFCQgFCQgFCQgFCQgFCQkFCQkFCQkFCQkFCQgFCAgFCAgFCAgFCAgFCAgFCAgFCQgFCQkFCAgFCAgFCAgF");
    private static readonly int[] Empty =
    {
        7,7,18,18,14,11,7,19,18,14,15,7,18,18,15,19,7,19,18,15,23,7,18,18,15,
        27,7,18,18,15,31,7,18,18,15,35,7,21,20,15,39,7,18,17,14,7,11,11,11,8,
        11,11,12,11,9,15,11,12,12,9,19,11,12,11,9,23,11,12,11,9,27,11,14,13,9,
        31,11,13,13,9,35,11,13,13,9,39,11,12,11,8,7,15,8,8,5,11,15,9,8,5,
        15,15,10,9,6,19,15,10,9,6,23,15,9,9,6,27,15,10,9,5,31,15,10,10,5,
        35,15,10,9,6,39,15,11,9,7,7,19,9,8,5,11,19,8,8,4,15,19,8,8,5,
        19,19,7,7,4,23,19,8,8,4,27,19,9,10,5,31,19,9,9,5,35,19,9,9,5,
        39,19,10,10,5,7,23,8,7,4,11,23,7,7,3,15,23,9,9,5,19,23,7,8,4,
        23,23,9,9,5,27,23,9,9,5,31,23,9,9,6,35,23,11,11,5,39,23,9,8,5,
        7,27,7,7,4,11,27,11,9,5,15,27,9,8,5,19,27,9,9,5,23,27,9,8,5,
        27,27,9,9,4,31,27,9,9,5,35,27,9,9,5,39,27,9,8,4,7,31,9,9,6,
        11,31,8,7,5,15,31,7,7,4,19,31,9,10,6,23,31,8,8,5,27,31,7,7,4,
        31,31,10,9,5,35,31,10,9,5,39,31,8,7,5,7,35,9,9,4,11,35,6,7,4,
        15,35,9,8,5,19,35,10,9,6,23,35,9,8,5,27,35,10,9,5,31,35,9,9,6,
        35,35,8,8,4,39,35,9,10,5,7,39,9,9,5,11,39,9,9,6,15,39,10,9,6,
        19,39,8,7,4,23,39,10,10,6,27,39,9,9,5,31,39,10,10,6,35,39,11,11,6,
        39,39,9,10,6
    };
    private static void Validate(int w, int h, byte[] rgb)
    {
        ArgumentNullException.ThrowIfNull(rgb);
        if (w <= 0 || h <= 0 || (rgb.Length % 3 != 0 || (long)w * h != rgb.Length / 3))
            throw new ArgumentException("Dimensions must describe the complete packed RGB frame.");
    }
    private static int P(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
    private static bool Match(int w, int h, byte[] rgb, int ox, int oy, double s, int[] template, int tolerance, double fraction)
    {
        int good = 0, total = template.Length / 5;
        for (int i = 0; i < template.Length; i += 5)
        {
            int x = ox + P(template[i] * s), y = oy + P(template[i+1] * s);
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            int p = (y*w+x)*3;
            int error = Math.Abs(rgb[p]-template[i+2])+Math.Abs(rgb[p+1]-template[i+3])+Math.Abs(rgb[p+2]-template[i+4]);
            if (s > 1 && !ReferenceEquals(template, Empty))
                for (int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                {
                    if(x+dx<0||y+dy<0||x+dx>=w||y+dy>=h)continue;
                    int n=((y+dy)*w+x+dx)*3;
                    error=Math.Min(error,Math.Abs(rgb[n]-template[i+2])+Math.Abs(rgb[n+1]-template[i+3])+Math.Abs(rgb[n+2]-template[i+4]));
                }
            if (error <= tolerance) good++;
            if (good + total - i/5 - 1 < total*fraction) return false;
        }
        return good >= total*fraction;
    }
    public static InventoryDetection? FindInventory(int width, int height, byte[] rgb)
    {
        Validate(width,height,rgb);
        var found = new List<Rectangle>();
        foreach (double s in Scales)
        for (int y = 0; y < height-P(40*s); y++)
        for (int x = 0; x < width-P(204*s); x++)
        {
            // Cheap distinctive gold sample before the full header signature (including title).
            int q=((y+P(Header[1]*s))*width+x+P(Header[0]*s))*3;
            if (Math.Abs(rgb[q]-Header[2])+Math.Abs(rgb[q+1]-Header[3])+Math.Abs(rgb[q+2]-Header[4])>18) continue;
            if (!Match(width,height,rgb,x,y,s,Header,30,.92)) continue;
            var grid = new Rectangle(x-P(71*s),y+P(330*s),P(343*s),P(196*s));
            if (grid.Left<0 || grid.Top<0 || grid.Right>width || grid.Bottom>height) continue;
            if (!Match(width,height,rgb,grid.X,grid.Y,s,Lattice,65,.65)) continue;
            if (!Match(width,height,rgb,grid.X,grid.Y,s,Seams,45,.65)) continue;
            if (!found.Any(r => Math.Abs(r.X-grid.X)<P(10*s) && Math.Abs(r.Y-grid.Y)<P(10*s) && Math.Abs(r.Width-grid.Width)<4)) found.Add(grid);
        }
        return found.Count==1 ? new InventoryDetection(found[0],7,4) : null;
    }
    private sealed record Line(int X,int Y,int Width);
    private static bool Gold(byte[] a,int p) => Math.Abs(a[p]-136)+Math.Abs(a[p+1]-119)+Math.Abs(a[p+2]-68)<=12;
    public static Rectangle? FindTooltip(int width,int height,byte[] rgb,Point? hover = null)
    {
        Validate(width,height,rgb);
        var lines=new List<Line>();
        for(int y=0;y<height;y++)
        {
            int start=-1;
            for(int x=0;x<=width;x++)
            {
                bool gold=x<width && Gold(rgb,(y*width+x)*3);
                if(gold && start<0) start=x;
                if(!gold && start>=0)
                {
                    if(x-start>=210 && x-start<=630) lines.Add(new Line(start,y,x-start));
                    start=-1;
                }
            }
        }
        var candidates=new List<Rectangle>();
        foreach(var top in lines)
        foreach(double s in Scales)
        {
            if(lines.Any(l=>l.X==top.X && l.Width==top.Width && l.Y<top.Y && top.Y-l.Y<4)) continue;
            // Description length changes panel width independently of the UI scale.
            var group=lines.Where(l=>Math.Abs(l.X-top.X)<=2 && Math.Abs(l.Width-top.Width)<=2 && l.Y>=top.Y && l.Y-top.Y<650*s).ToList();
            // The first two rules enclose the icon/title/category header. Not every gold line is a tooltip.
            var headerRule = group.FirstOrDefault(l=>l.Y-top.Y>=10*s);
            if(headerRule==null || headerRule.Y-top.Y<88*s || headerRule.Y-top.Y>115*s) continue;
            if(top.Y<P(20*s)) continue;
            int bright=0,dark=0,samples=0,title=0;
            for(int yy=P(5*s);yy<P(38*s);yy+=2)
            for(int xx=P(49*s);xx<top.Width-5;xx+=2)
            {
                int x=top.X+xx,y=top.Y+yy;if(x>=width||y>=height)continue;
                int q=(y*width+x)*3;
                if(Math.Max(rgb[q],Math.Max(rgb[q+1],rgb[q+2]))>120)title++;
            }
            for(int yy=P(18*s);yy<P(40*s);yy+=Math.Max(1,P(3*s)))
            for(int xx=P(10*s);xx<P(36*s);xx+=Math.Max(1,P(3*s)))
            {
                int x=top.X+xx,y=top.Y+yy;if(x>=width||y>=height)continue;
                int p=(y*width+x)*3;samples++;
                if(Math.Max(rgb[p],Math.Max(rgb[p+1],rgb[p+2]))>100)bright++;
            }
            // Semi-transparent black panel is judged statistically, not by exact black pixels.
            int n=0;
            for(int yy=3;yy<P(90*s);yy+=3)
            for(int xx=P(52*s);xx<top.Width-3;xx+=5)
            {
                int x=top.X+xx,y=top.Y+yy;if(x>=width||y>=height)continue;
                int p=(y*width+x)*3;n++;
                if(rgb[p]+rgb[p+1]+rgb[p+2]<180)dark++;
            }
            if(samples==0 || bright<samples*.12 || title<5 || n==0 || dark<n*.65)continue;
            int bottom=group.Max(l=>l.Y)+P(28*s);
            var bounds=Rectangle.Intersect(new Rectangle(top.X-P(5*s),top.Y-P(24*s),top.Width+P(20*s),bottom-(top.Y-P(24*s))),new Rectangle(0,0,width,height));
            if(hover is Point hp && (hp.X<bounds.Left-P(650*s)||hp.X>bounds.Right+P(650*s)||hp.Y<bounds.Top-P(650*s)||hp.Y>bounds.Bottom+P(650*s)))continue;
            if(!candidates.Any(r=>r.IntersectsWith(bounds)))candidates.Add(bounds);
        }
        return candidates.Count==1?candidates[0]:null;
    }
    public static bool IsEmptySlot(int width,int height,byte[] rgb,Rectangle slot)
    {
        Validate(width,height,rgb);
        if(slot.Width<=0||slot.Height<=0||slot.Left<0||slot.Top<0||(long)slot.X+slot.Width>width||(long)slot.Y+slot.Height>height)
            throw new ArgumentException("Slot must be a nonempty rectangle inside the frame.",nameof(slot));
        double s=slot.Width/49.0;
        if(Math.Abs(slot.Height-slot.Width)>1 || !Scales.Any(v=>Math.Abs(v-s)<.025))return false;
        // Exact dark texture evidence only. Darkness alone is never enough.
        if (s != 1 || !Match(width,height,rgb,slot.X,slot.Y,s,Empty,9,1)) return false;
        for(int y=0;y<38;y++)for(int x=0;x<38;x++)
        {
            int q=((slot.Y+6+y)*width+slot.X+6+x)*3, t=(y*38+x)*3;
            if(Math.Abs(rgb[q]-EmptyPixels[t])+Math.Abs(rgb[q+1]-EmptyPixels[t+1])+Math.Abs(rgb[q+2]-EmptyPixels[t+2])>9)return false;
        }
        return true;
    }
}
