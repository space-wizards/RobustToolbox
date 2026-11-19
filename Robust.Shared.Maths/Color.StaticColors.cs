using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Maths;

public partial struct Color
{
    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
    /// </summary>
    public static Color Transparent => new(255, 255, 255, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
    /// </summary>
    public static Color AliceBlue => new(240, 248, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
    /// </summary>
    public static Color AntiqueWhite => new(250, 235, 215);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
    /// </summary>
    public static Color Aqua => new(0, 255, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
    /// </summary>
    public static Color Aquamarine => new(127, 255, 212);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
    /// </summary>
    public static Color Azure => new(240, 255, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
    /// </summary>
    public static Color Beige => new(245, 245, 220);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
    /// </summary>
    public static Color Bisque => new(255, 228, 196);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
    /// </summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
    /// </summary>
    public static Color BlanchedAlmond => new(255, 235, 205);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
    /// </summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
    /// </summary>
    public static Color BlueViolet => new(138, 43, 226);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
    /// </summary>
    public static Color Brown => new(165, 42, 42);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
    /// </summary>
    public static Color BurlyWood => new(222, 184, 135);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
    /// </summary>
    public static Color CadetBlue => new(95, 158, 160);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
    /// </summary>
    public static Color Chartreuse => new(127, 255, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
    /// </summary>
    public static Color Chocolate => new(210, 105, 30);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
    /// </summary>
    public static Color Coral => new(255, 127, 80);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
    /// </summary>
    public static Color CornflowerBlue => new(100, 149, 237);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
    /// </summary>
    public static Color Cornsilk => new(255, 248, 220);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
    /// </summary>
    public static Color Crimson => new(220, 20, 60);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
    /// </summary>
    public static Color Cyan => new(0, 255, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
    /// </summary>
    public static Color DarkBlue => new(0, 0, 139);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
    /// </summary>
    public static Color DarkCyan => new(0, 139, 139);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
    /// </summary>
    public static Color DarkGoldenrod => new(184, 134, 11);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
    /// </summary>
    public static Color DarkGray => new(169, 169, 169);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
    /// </summary>
    public static Color DarkGreen => new(0, 100, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
    /// </summary>
    public static Color DarkKhaki => new(189, 183, 107);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
    /// </summary>
    public static Color DarkMagenta => new(139, 0, 139);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
    /// </summary>
    public static Color DarkOliveGreen => new(85, 107, 47);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
    /// </summary>
    public static Color DarkOrange => new(255, 140, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
    /// </summary>
    public static Color DarkOrchid => new(153, 50, 204);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
    /// </summary>
    public static Color DarkRed => new(139, 0, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
    /// </summary>
    public static Color DarkSalmon => new(233, 150, 122);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (143, 188, 143, 255).
    ///     Previously (R, G, B, A) = (143, 188, 139, 255) before .NET 5.
    /// </summary>
    public static Color DarkSeaGreen => new(143, 188, 143);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
    /// </summary>
    public static Color DarkSlateBlue => new(72, 61, 139);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
    /// </summary>
    public static Color DarkSlateGray => new(47, 79, 79);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
    /// </summary>
    public static Color DarkTurquoise => new(0, 206, 209);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
    /// </summary>
    public static Color DarkViolet => new(148, 0, 211);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
    /// </summary>
    public static Color DeepPink => new(255, 20, 147);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
    /// </summary>
    public static Color DeepSkyBlue => new(0, 191, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
    /// </summary>
    public static Color DimGray => new(105, 105, 105);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
    /// </summary>
    public static Color DodgerBlue => new(30, 144, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
    /// </summary>
    public static Color Firebrick => new(178, 34, 34);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
    /// </summary>
    public static Color FloralWhite => new(255, 250, 240);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
    /// </summary>
    public static Color ForestGreen => new(34, 139, 34);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
    /// </summary>
    public static Color Fuchsia => new(255, 0, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
    /// </summary>
    public static Color Gainsboro => new(220, 220, 220);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (27, 27, 30, 255).
    /// </summary>
    public static Color GluonGrey => new(27, 27, 30);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
    /// </summary>
    public static Color GhostWhite => new(248, 248, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
    /// </summary>
    public static Color Gold => new(255, 215, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
    /// </summary>
    public static Color Goldenrod => new(218, 165, 32);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
    /// </summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
    /// </summary>
    public static Color Green => new(0, 128, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
    /// </summary>
    public static Color GreenYellow => new(173, 255, 47);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
    /// </summary>
    public static Color Honeydew => new(240, 255, 240);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
    /// </summary>
    public static Color HotPink => new(255, 105, 180);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
    /// </summary>
    public static Color IndianRed => new(205, 92, 92);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
    /// </summary>
    public static Color Indigo => new(75, 0, 130);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
    /// </summary>
    public static Color Ivory => new(255, 255, 240);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
    /// </summary>
    public static Color Khaki => new(240, 230, 140);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
    /// </summary>
    public static Color Lavender => new(230, 230, 250);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
    /// </summary>
    public static Color LavenderBlush => new(255, 240, 245);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
    /// </summary>
    public static Color LawnGreen => new(124, 252, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
    /// </summary>
    public static Color LemonChiffon => new(255, 250, 205);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
    /// </summary>
    public static Color LightBlue => new(173, 216, 230);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
    /// </summary>
    public static Color LightCoral => new(240, 128, 128);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
    /// </summary>
    public static Color LightCyan => new(224, 255, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
    /// </summary>
    public static Color LightGoldenrodYellow => new(250, 250, 210);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
    /// </summary>
    public static Color LightGreen => new(144, 238, 144);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
    /// </summary>
    public static Color LightGray => new(211, 211, 211);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
    /// </summary>
    public static Color LightPink => new(255, 182, 193);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
    /// </summary>
    public static Color LightSalmon => new(255, 160, 122);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
    /// </summary>
    public static Color LightSeaGreen => new(32, 178, 170);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
    /// </summary>
    public static Color LightSkyBlue => new(135, 206, 250);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
    /// </summary>
    public static Color LightSlateGray => new(119, 136, 153);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
    /// </summary>
    public static Color LightSteelBlue => new(176, 196, 222);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
    /// </summary>
    public static Color LightYellow => new(255, 255, 224);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
    /// </summary>
    public static Color Lime => new(0, 255, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
    /// </summary>
    public static Color LimeGreen => new(50, 205, 50);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
    /// </summary>
    public static Color Linen => new(250, 240, 230);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
    /// </summary>
    public static Color Magenta => new(255, 0, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
    /// </summary>
    public static Color Maroon => new(128, 0, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
    /// </summary>
    public static Color MediumAquamarine => new(102, 205, 170);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
    /// </summary>
    public static Color MediumBlue => new(0, 0, 205);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
    /// </summary>
    public static Color MediumOrchid => new(186, 85, 211);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
    /// </summary>
    public static Color MediumPurple => new(147, 112, 219);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
    /// </summary>
    public static Color MediumSeaGreen => new(60, 179, 113);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
    /// </summary>
    public static Color MediumSlateBlue => new(123, 104, 238);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
    /// </summary>
    public static Color MediumSpringGreen => new(0, 250, 154);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
    /// </summary>
    public static Color MediumTurquoise => new(72, 209, 204);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
    /// </summary>
    public static Color MediumVioletRed => new(199, 21, 133);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
    /// </summary>
    public static Color MidnightBlue => new(25, 25, 112);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
    /// </summary>
    public static Color MintCream => new(245, 255, 250);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
    /// </summary>
    public static Color MistyRose => new(255, 228, 225);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
    /// </summary>
    public static Color Moccasin => new(255, 228, 181);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
    /// </summary>
    public static Color NavajoWhite => new(255, 222, 173);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
    /// </summary>
    public static Color Navy => new(0, 0, 128);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
    /// </summary>
    public static Color NobleBlack => new(32, 32, 37);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
    /// </summary>
    public static Color OldLace => new(253, 245, 230);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
    /// </summary>
    public static Color Olive => new(128, 128, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
    /// </summary>
    public static Color OliveDrab => new(107, 142, 35);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
    /// </summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
    /// </summary>
    public static Color OrangeRed => new(255, 69, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
    /// </summary>
    public static Color Orchid => new(218, 112, 214);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
    /// </summary>
    public static Color PaleGoldenrod => new(238, 232, 170);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
    /// </summary>
    public static Color PaleGreen => new(152, 251, 152);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
    /// </summary>
    public static Color PaleTurquoise => new(175, 238, 238);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
    /// </summary>
    public static Color PaleVioletRed => new(219, 112, 147);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
    /// </summary>
    public static Color PapayaWhip => new(255, 239, 213);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
    /// </summary>
    public static Color PeachPuff => new(255, 218, 185);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
    /// </summary>
    public static Color Peru => new(205, 133, 63);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
    /// </summary>
    public static Color Pink => new(255, 192, 203);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
    /// </summary>
    public static Color Plum => new(221, 160, 221);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
    /// </summary>
    public static Color PowderBlue => new(176, 224, 230);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
    /// </summary>
    public static Color Purple => new(128, 0, 128);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
    /// </summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
    /// </summary>
    public static Color RosyBrown => new(188, 143, 143);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
    /// </summary>
    public static Color RoyalBlue => new(65, 105, 225);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (204, 71, 120, 255).
    /// </summary>
    public static Color Ruber => new(204, 71, 120);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
    /// </summary>
    public static Color SaddleBrown => new(139, 69, 19);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
    /// </summary>
    public static Color Salmon => new(250, 128, 114);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
    /// </summary>
    public static Color SandyBrown => new(244, 164, 96);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 66, 153, 255).
    /// </summary>
    public static Color SeaBlue => new(0, 66, 153);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
    /// </summary>
    public static Color SeaGreen => new(46, 139, 87);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
    /// </summary>
    public static Color SeaShell => new(255, 245, 238);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
    /// </summary>
    public static Color Sienna => new(160, 82, 45);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
    /// </summary>
    public static Color Silver => new(192, 192, 192);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
    /// </summary>
    public static Color SkyBlue => new(135, 206, 235);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
    /// </summary>
    public static Color SlateBlue => new(106, 90, 205);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
    /// </summary>
    public static Color SlateGray => new(112, 128, 144);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
    /// </summary>
    public static Color Snow => new(255, 250, 250);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
    /// </summary>
    public static Color SpringGreen => new(0, 255, 127);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
    /// </summary>
    public static Color SteelBlue => new(70, 130, 180);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
    /// </summary>
    public static Color Tan => new(210, 180, 140);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
    /// </summary>
    public static Color Teal => new(0, 128, 128);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
    /// </summary>
    public static Color Thistle => new(216, 191, 216);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
    /// </summary>
    public static Color Tomato => new(255, 99, 71);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
    /// </summary>
    public static Color Turquoise => new(64, 224, 208);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
    /// </summary>
    public static Color Violet => new(238, 130, 238);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (126, 3, 168, 255).
    /// </summary>
    public static Color BetterViolet => new(126, 3, 168);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 153, 0, 255).
    /// </summary>
    public static Color VividGamboge => new(255, 153, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
    /// </summary>
    public static Color Wheat => new(245, 222, 179);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
    /// </summary>
    public static Color White => new(255, 255, 255);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
    /// </summary>
    public static Color WhiteSmoke => new(245, 245, 245);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
    /// </summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>
    ///     Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
    /// </summary>
    public static Color YellowGreen => new(154, 205, 50);

    private static readonly FrozenDictionary<string, Color> DefaultColors = new Dictionary<string, Color>()
    {
        ["transparent"] = Transparent,
        ["aliceblue"] = AliceBlue,
        ["antiquewhite"] = AntiqueWhite,
        ["aqua"] = Aqua,
        ["aquamarine"] = Aquamarine,
        ["azure"] = Azure,
        ["beige"] = Beige,
        ["betterviolet"] = BetterViolet,
        ["bisque"] = Bisque,
        ["black"] = Black,
        ["blanchedalmond"] = BlanchedAlmond,
        ["blue"] = Blue,
        ["blueviolet"] = BlueViolet,
        ["brown"] = Brown,
        ["burlywood"] = BurlyWood,
        ["cadetblue"] = CadetBlue,
        ["chartreuse"] = Chartreuse,
        ["chocolate"] = Chocolate,
        ["coral"] = Coral,
        ["cornflowerblue"] = CornflowerBlue,
        ["cornsilk"] = Cornsilk,
        ["crimson"] = Crimson,
        ["cyan"] = Cyan,
        ["darkblue"] = DarkBlue,
        ["darkcyan"] = DarkCyan,
        ["darkgoldenrod"] = DarkGoldenrod,
        ["darkgray"] = DarkGray,
        ["darkgreen"] = DarkGreen,
        ["darkkhaki"] = DarkKhaki,
        ["darkmagenta"] = DarkMagenta,
        ["darkolivegreen"] = DarkOliveGreen,
        ["darkorange"] = DarkOrange,
        ["darkorchid"] = DarkOrchid,
        ["darkred"] = DarkRed,
        ["darksalmon"] = DarkSalmon,
        ["darkseagreen"] = DarkSeaGreen,
        ["darkslateblue"] = DarkSlateBlue,
        ["darkslategray"] = DarkSlateGray,
        ["darkturquoise"] = DarkTurquoise,
        ["darkviolet"] = DarkViolet,
        ["deeppink"] = DeepPink,
        ["deepskyblue"] = DeepSkyBlue,
        ["dimgray"] = DimGray,
        ["dodgerblue"] = DodgerBlue,
        ["firebrick"] = Firebrick,
        ["floralwhite"] = FloralWhite,
        ["forestgreen"] = ForestGreen,
        ["fuchsia"] = Fuchsia,
        ["gainsboro"] = Gainsboro,
        ["ghostwhite"] = GhostWhite,
        ["gluongrey"] = GluonGrey,
        ["gold"] = Gold,
        ["goldenrod"] = Goldenrod,
        ["gray"] = Gray,
        ["green"] = Green,
        ["greenyellow"] = GreenYellow,
        ["honeydew"] = Honeydew,
        ["hotpink"] = HotPink,
        ["indianred"] = IndianRed,
        ["indigo"] = Indigo,
        ["ivory"] = Ivory,
        ["khaki"] = Khaki,
        ["lavender"] = Lavender,
        ["lavenderblush"] = LavenderBlush,
        ["lawngreen"] = LawnGreen,
        ["lemonchiffon"] = LemonChiffon,
        ["lightblue"] = LightBlue,
        ["lightcoral"] = LightCoral,
        ["lightcyan"] = LightCyan,
        ["lightgoldenrodyellow"] = LightGoldenrodYellow,
        ["lightgreen"] = LightGreen,
        ["lightgray"] = LightGray,
        ["lightpink"] = LightPink,
        ["lightsalmon"] = LightSalmon,
        ["lightseagreen"] = LightSeaGreen,
        ["lightskyblue"] = LightSkyBlue,
        ["lightslategray"] = LightSlateGray,
        ["lightsteelblue"] = LightSteelBlue,
        ["lightyellow"] = LightYellow,
        ["lime"] = Lime,
        ["limegreen"] = LimeGreen,
        ["linen"] = Linen,
        ["magenta"] = Magenta,
        ["maroon"] = Maroon,
        ["mediumaquamarine"] = MediumAquamarine,
        ["mediumblue"] = MediumBlue,
        ["mediumorchid"] = MediumOrchid,
        ["mediumpurple"] = MediumPurple,
        ["mediumseagreen"] = MediumSeaGreen,
        ["mediumslateblue"] = MediumSlateBlue,
        ["mediumspringgreen"] = MediumSpringGreen,
        ["mediumturquoise"] = MediumTurquoise,
        ["mediumvioletred"] = MediumVioletRed,
        ["midnightblue"] = MidnightBlue,
        ["mintcream"] = MintCream,
        ["mistyrose"] = MistyRose,
        ["moccasin"] = Moccasin,
        ["navajowhite"] = NavajoWhite,
        ["navy"] = Navy,
        ["nobleblack"] = NobleBlack,
        ["oldlace"] = OldLace,
        ["olive"] = Olive,
        ["olivedrab"] = OliveDrab,
        ["orange"] = Orange,
        ["orangered"] = OrangeRed,
        ["orchid"] = Orchid,
        ["palegoldenrod"] = PaleGoldenrod,
        ["palegreen"] = PaleGreen,
        ["paleturquoise"] = PaleTurquoise,
        ["palevioletred"] = PaleVioletRed,
        ["papayawhip"] = PapayaWhip,
        ["peachpuff"] = PeachPuff,
        ["peru"] = Peru,
        ["pink"] = Pink,
        ["plum"] = Plum,
        ["powderblue"] = PowderBlue,
        ["purple"] = Purple,
        ["red"] = Red,
        ["rosybrown"] = RosyBrown,
        ["royalblue"] = RoyalBlue,
        ["ruber"] = Ruber,
        ["saddlebrown"] = SaddleBrown,
        ["salmon"] = Salmon,
        ["sandybrown"] = SandyBrown,
        ["seablue"] = SeaBlue,
        ["seagreen"] = SeaGreen,
        ["seashell"] = SeaShell,
        ["sienna"] = Sienna,
        ["silver"] = Silver,
        ["skyblue"] = SkyBlue,
        ["slateblue"] = SlateBlue,
        ["slategray"] = SlateGray,
        ["snow"] = Snow,
        ["springgreen"] = SpringGreen,
        ["steelblue"] = SteelBlue,
        ["tan"] = Tan,
        ["teal"] = Teal,
        ["thistle"] = Thistle,
        ["tomato"] = Tomato,
        ["turquoise"] = Turquoise,
        ["violet"] = Violet,
        ["vividgamboge"] = VividGamboge,
        ["wheat"] = Wheat,
        ["white"] = White,
        ["whitesmoke"] = WhiteSmoke,
        ["yellow"] = Yellow,
        ["yellowgreen"] = YellowGreen,
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<Color, string> DefaultColorsInverted =
        DefaultColors.ToLookup(pair => pair.Value).ToFrozenDictionary(i => i.Key, i => i.First().Key);

    /// <summary>
    ///     Gets the associated color name with this color instance by using an inverted version of <see cref="DefaultColors"/>.
    ///     Returns <see langword="null"/> if there is no possible name with this color instance.
    /// </summary>
    /// <returns>The associated color name if any, usable with <see cref="DefaultColors"/>.</returns>
    public readonly string? Name()
    {
        return DefaultColorsInverted.GetValueOrDefault(this);
    }
}
