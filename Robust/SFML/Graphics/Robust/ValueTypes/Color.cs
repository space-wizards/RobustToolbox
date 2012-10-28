using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Utility class for manipulating 32-bits RGBA colors
        /// </summary>
        ////////////////////////////////////////////////////////////
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        [TypeConverter(typeof(ColorConverter))]
        public struct Color : IEquatable<Color>
        {
            byte _r;
            byte _g;
            byte _b;
            byte _a;

            ////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the color from its red, green, blue and alpha components
            /// </summary>
            /// <param name="red">Red component</param>
            /// <param name="green">Green component</param>
            /// <param name="blue">Blue component</param>
            /// <param name="alpha">Alpha (transparency) component</param>
            ////////////////////////////////////////////////////////////
            public Color(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
            {
                _r = red;
                _g = green;
                _b = blue;
                _a = alpha;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the color from another
            /// </summary>
            /// <param name="color">Color to copy</param>
            ////////////////////////////////////////////////////////////
            public Color(Color color) : this(color.R, color.G, color.B, color.A)
            {
            }

            /// <summary>Initializes a new instance of Color.</summary>
            /// <param name="rgb">A Color specifying the red, green, and blue components of a color.</param>
            /// <param name="a">The alpha component of a color, between 0 and 255.</param>
            public Color(Color rgb, byte a) : this(rgb.R, rgb.G, rgb.B, a)
            {
            }

            /// <summary>Alpha (transparent) component of the color</summary>
            public byte A
            {
                get { return _a; }
                set { _a = value; }
            }

            /// <summary>Gets a system-defined color with the value R:240 G:248 B:255 A:255.</summary>
            public static Color AliceBlue
            {
                get { return new Color(240, 248, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:250 G:235 B:215 A:255.</summary>
            public static Color AntiqueWhite
            {
                get { return new Color(250, 235, 215); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:255 B:255 A:255.</summary>
            public static Color Aqua
            {
                get { return new Color(0, 255, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:127 G:255 B:212 A:255.</summary>
            public static Color Aquamarine
            {
                get { return new Color(127, 255, 212); }
            }

            /// <summary>Gets a system-defined color with the value R:240 G:255 B:255 A:255.</summary>
            public static Color Azure
            {
                get { return new Color(240, 255, 255); }
            }

            /// <summary>Blue component of the color</summary>
            public byte B
            {
                get { return _b; }
                set { _b = value; }
            }

            /// <summary>Gets a system-defined color with the value R:245 G:245 B:220 A:255.</summary>
            public static Color Beige
            {
                get { return new Color(245, 245, 220); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:228 B:196 A:255.</summary>
            public static Color Bisque
            {
                get { return new Color(255, 228, 196); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:0 B:0 A:255.</summary>
            public static Color Black
            {
                get { return new Color(0, 0, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:235 B:205 A:255.</summary>
            public static Color BlanchedAlmond
            {
                get { return new Color(255, 235, 205); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:0 B:255 A:255.</summary>
            public static Color Blue
            {
                get { return new Color(0, 0, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:138 G:43 B:226 A:255.</summary>
            public static Color BlueViolet
            {
                get { return new Color(138, 43, 226); }
            }

            /// <summary>Gets a system-defined color with the value R:165 G:42 B:42 A:255.</summary>
            public static Color Brown
            {
                get { return new Color(165, 42, 42); }
            }

            /// <summary>Gets a system-defined color with the value R:222 G:184 B:135 A:255.</summary>
            public static Color BurlyWood
            {
                get { return new Color(222, 184, 135); }
            }

            /// <summary>Gets a system-defined color with the value R:95 G:158 B:160 A:255.</summary>
            public static Color CadetBlue
            {
                get { return new Color(95, 158, 160); }
            }

            /// <summary>Gets a system-defined color with the value R:127 G:255 B:0 A:255.</summary>
            public static Color Chartreuse
            {
                get { return new Color(127, 255, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:210 G:105 B:30 A:255.</summary>
            public static Color Chocolate
            {
                get { return new Color(210, 105, 30); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:127 B:80 A:255.</summary>
            public static Color Coral
            {
                get { return new Color(255, 127, 80); }
            }

            /// <summary>Gets a system-defined color with the value R:100 G:149 B:237 A:255.</summary>
            public static Color CornflowerBlue
            {
                get { return new Color(100, 149, 237); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:248 B:220 A:255.</summary>
            public static Color Cornsilk
            {
                get { return new Color(255, 248, 220); }
            }

            /// <summary>Gets a system-defined color with the value R:220 G:20 B:60 A:255.</summary>
            public static Color Crimson
            {
                get { return new Color(220, 20, 60); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:255 B:255 A:255.</summary>
            public static Color Cyan
            {
                get { return new Color(0, 255, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:0 B:139 A:255.</summary>
            public static Color DarkBlue
            {
                get { return new Color(0, 0, 139); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:139 B:139 A:255.</summary>
            public static Color DarkCyan
            {
                get { return new Color(0, 139, 139); }
            }

            /// <summary>Gets a system-defined color with the value R:184 G:134 B:11 A:255.</summary>
            public static Color DarkGoldenrod
            {
                get { return new Color(184, 134, 11); }
            }

            /// <summary>Gets a system-defined color with the value R:169 G:169 B:169 A:255.</summary>
            public static Color DarkGray
            {
                get { return new Color(169, 169, 169); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:100 B:0 A:255.</summary>
            public static Color DarkGreen
            {
                get { return new Color(0, 100, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:189 G:183 B:107 A:255.</summary>
            public static Color DarkKhaki
            {
                get { return new Color(189, 183, 107); }
            }

            /// <summary>Gets a system-defined color with the value R:139 G:0 B:139 A:255.</summary>
            public static Color DarkMagenta
            {
                get { return new Color(139, 0, 139); }
            }

            /// <summary>Gets a system-defined color with the value R:85 G:107 B:47 A:255.</summary>
            public static Color DarkOliveGreen
            {
                get { return new Color(85, 107, 47); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:140 B:0 A:255.</summary>
            public static Color DarkOrange
            {
                get { return new Color(255, 140, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:153 G:50 B:204 A:255.</summary>
            public static Color DarkOrchid
            {
                get { return new Color(153, 50, 204); }
            }

            /// <summary>Gets a system-defined color with the value R:139 G:0 B:0 A:255.</summary>
            public static Color DarkRed
            {
                get { return new Color(139, 0, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:233 G:150 B:122 A:255.</summary>
            public static Color DarkSalmon
            {
                get { return new Color(233, 150, 122); }
            }

            /// <summary>Gets a system-defined color with the value R:143 G:188 B:139 A:255.</summary>
            public static Color DarkSeaGreen
            {
                get { return new Color(143, 188, 139); }
            }

            /// <summary>Gets a system-defined color with the value R:72 G:61 B:139 A:255.</summary>
            public static Color DarkSlateBlue
            {
                get { return new Color(72, 61, 139); }
            }

            /// <summary>Gets a system-defined color with the value R:47 G:79 B:79 A:255.</summary>
            public static Color DarkSlateGray
            {
                get { return new Color(47, 79, 79); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:206 B:209 A:255.</summary>
            public static Color DarkTurquoise
            {
                get { return new Color(0, 206, 209); }
            }

            /// <summary>Gets a system-defined color with the value R:148 G:0 B:211 A:255.</summary>
            public static Color DarkViolet
            {
                get { return new Color(148, 0, 211); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:20 B:147 A:255.</summary>
            public static Color DeepPink
            {
                get { return new Color(255, 20, 147); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:191 B:255 A:255.</summary>
            public static Color DeepSkyBlue
            {
                get { return new Color(0, 191, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:105 G:105 B:105 A:255.</summary>
            public static Color DimGray
            {
                get { return new Color(105, 105, 105); }
            }

            /// <summary>Gets a system-defined color with the value R:30 G:144 B:255 A:255.</summary>
            public static Color DodgerBlue
            {
                get { return new Color(30, 144, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:178 G:34 B:34 A:255.</summary>
            public static Color Firebrick
            {
                get { return new Color(178, 34, 34); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:250 B:240 A:255.</summary>
            public static Color FloralWhite
            {
                get { return new Color(255, 250, 240); }
            }

            /// <summary>Gets a system-defined color with the value R:34 G:139 B:34 A:255.</summary>
            public static Color ForestGreen
            {
                get { return new Color(34, 139, 34); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:0 B:255 A:255.</summary>
            public static Color Fuchsia
            {
                get { return new Color(255, 0, 255); }
            }

            /// <summary>Green component of the color</summary>
            public byte G
            {
                get { return _g; }
                set { _g = value; }
            }

            /// <summary>Gets a system-defined color with the value R:220 G:220 B:220 A:255.</summary>
            public static Color Gainsboro
            {
                get { return new Color(220, 220, 220); }
            }

            /// <summary>Gets a system-defined color with the value R:248 G:248 B:255 A:255.</summary>
            public static Color GhostWhite
            {
                get { return new Color(248, 248, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:215 B:0 A:255.</summary>
            public static Color Gold
            {
                get { return new Color(255, 215, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:218 G:165 B:32 A:255.</summary>
            public static Color Goldenrod
            {
                get { return new Color(218, 165, 32); }
            }

            /// <summary>Gets a system-defined color with the value R:128 G:128 B:128 A:255.</summary>
            public static Color Gray
            {
                get { return new Color(128, 128, 128); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:128 B:0 A:255.</summary>
            public static Color Green
            {
                get { return new Color(0, 128, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:173 G:255 B:47 A:255.</summary>
            public static Color GreenYellow
            {
                get { return new Color(173, 255, 47); }
            }

            /// <summary>Gets a system-defined color with the value R:240 G:255 B:240 A:255.</summary>
            public static Color Honeydew
            {
                get { return new Color(240, 255, 240); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:105 B:180 A:255.</summary>
            public static Color HotPink
            {
                get { return new Color(255, 105, 180); }
            }

            /// <summary>Gets a system-defined color with the value R:205 G:92 B:92 A:255.</summary>
            public static Color IndianRed
            {
                get { return new Color(205, 92, 92); }
            }

            /// <summary>Gets a system-defined color with the value R:75 G:0 B:130 A:255.</summary>
            public static Color Indigo
            {
                get { return new Color(75, 0, 130); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:255 B:240 A:255.</summary>
            public static Color Ivory
            {
                get { return new Color(255, 255, 240); }
            }

            /// <summary>Gets a system-defined color with the value R:240 G:230 B:140 A:255.</summary>
            public static Color Khaki
            {
                get { return new Color(240, 230, 140); }
            }

            /// <summary>Gets a system-defined color with the value R:230 G:230 B:250 A:255.</summary>
            public static Color Lavender
            {
                get { return new Color(230, 230, 250); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:240 B:245 A:255.</summary>
            public static Color LavenderBlush
            {
                get { return new Color(255, 240, 245); }
            }

            /// <summary>Gets a system-defined color with the value R:124 G:252 B:0 A:255.</summary>
            public static Color LawnGreen
            {
                get { return new Color(124, 252, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:250 B:205 A:255.</summary>
            public static Color LemonChiffon
            {
                get { return new Color(255, 250, 205); }
            }

            /// <summary>Gets a system-defined color with the value R:173 G:216 B:230 A:255.</summary>
            public static Color LightBlue
            {
                get { return new Color(173, 216, 230); }
            }

            /// <summary>Gets a system-defined color with the value R:240 G:128 B:128 A:255.</summary>
            public static Color LightCoral
            {
                get { return new Color(240, 128, 128); }
            }

            /// <summary>Gets a system-defined color with the value R:224 G:255 B:255 A:255.</summary>
            public static Color LightCyan
            {
                get { return new Color(224, 255, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:250 G:250 B:210 A:255.</summary>
            public static Color LightGoldenrodYellow
            {
                get { return new Color(250, 250, 210); }
            }

            /// <summary>Gets a system-defined color with the value R:211 G:211 B:211 A:255.</summary>
            public static Color LightGray
            {
                get { return new Color(211, 211, 211); }
            }

            /// <summary>Gets a system-defined color with the value R:144 G:238 B:144 A:255.</summary>
            public static Color LightGreen
            {
                get { return new Color(144, 238, 144); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:182 B:193 A:255.</summary>
            public static Color LightPink
            {
                get { return new Color(255, 182, 193); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:160 B:122 A:255.</summary>
            public static Color LightSalmon
            {
                get { return new Color(255, 160, 122); }
            }

            /// <summary>Gets a system-defined color with the value R:32 G:178 B:170 A:255.</summary>
            public static Color LightSeaGreen
            {
                get { return new Color(32, 178, 170); }
            }

            /// <summary>Gets a system-defined color with the value R:135 G:206 B:250 A:255.</summary>
            public static Color LightSkyBlue
            {
                get { return new Color(135, 206, 250); }
            }

            /// <summary>Gets a system-defined color with the value R:119 G:136 B:153 A:255.</summary>
            public static Color LightSlateGray
            {
                get { return new Color(119, 136, 153); }
            }

            /// <summary>Gets a system-defined color with the value R:176 G:196 B:222 A:255.</summary>
            public static Color LightSteelBlue
            {
                get { return new Color(176, 196, 222); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:255 B:224 A:255.</summary>
            public static Color LightYellow
            {
                get { return new Color(255, 255, 224); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:255 B:0 A:255.</summary>
            public static Color Lime
            {
                get { return new Color(0, 255, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:50 G:205 B:50 A:255.</summary>
            public static Color LimeGreen
            {
                get { return new Color(50, 205, 50); }
            }

            /// <summary>Gets a system-defined color with the value R:250 G:240 B:230 A:255.</summary>
            public static Color Linen
            {
                get { return new Color(250, 240, 230); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:0 B:255 A:255.</summary>
            public static Color Magenta
            {
                get { return new Color(255, 0, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:128 G:0 B:0 A:255.</summary>
            public static Color Maroon
            {
                get { return new Color(128, 0, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:102 G:205 B:170 A:255.</summary>
            public static Color MediumAquamarine
            {
                get { return new Color(102, 205, 170); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:0 B:205 A:255.</summary>
            public static Color MediumBlue
            {
                get { return new Color(0, 0, 205); }
            }

            /// <summary>Gets a system-defined color with the value R:186 G:85 B:211 A:255.</summary>
            public static Color MediumOrchid
            {
                get { return new Color(186, 85, 211); }
            }

            /// <summary>Gets a system-defined color with the value R:147 G:112 B:219 A:255.</summary>
            public static Color MediumPurple
            {
                get { return new Color(147, 112, 219); }
            }

            /// <summary>Gets a system-defined color with the value R:60 G:179 B:113 A:255.</summary>
            public static Color MediumSeaGreen
            {
                get { return new Color(60, 179, 113); }
            }

            /// <summary>Gets a system-defined color with the value R:123 G:104 B:238 A:255.</summary>
            public static Color MediumSlateBlue
            {
                get { return new Color(123, 104, 238); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:250 B:154 A:255.</summary>
            public static Color MediumSpringGreen
            {
                get { return new Color(0, 250, 154); }
            }

            /// <summary>Gets a system-defined color with the value R:72 G:209 B:204 A:255.</summary>
            public static Color MediumTurquoise
            {
                get { return new Color(72, 209, 204); }
            }

            /// <summary>Gets a system-defined color with the value R:199 G:21 B:133 A:255.</summary>
            public static Color MediumVioletRed
            {
                get { return new Color(199, 21, 133); }
            }

            /// <summary>Gets a system-defined color with the value R:25 G:25 B:112 A:255.</summary>
            public static Color MidnightBlue
            {
                get { return new Color(25, 25, 112); }
            }

            /// <summary>Gets a system-defined color with the value R:245 G:255 B:250 A:255.</summary>
            public static Color MintCream
            {
                get { return new Color(245, 255, 250); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:228 B:225 A:255.</summary>
            public static Color MistyRose
            {
                get { return new Color(255, 228, 225); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:228 B:181 A:255.</summary>
            public static Color Moccasin
            {
                get { return new Color(255, 228, 181); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:222 B:173 A:255.</summary>
            public static Color NavajoWhite
            {
                get { return new Color(255, 222, 173); }
            }

            /// <summary>Gets a system-defined color R:0 G:0 B:128 A:255.</summary>
            public static Color Navy
            {
                get { return new Color(0, 0, 128); }
            }

            /// <summary>Gets a system-defined color with the value R:253 G:245 B:230 A:255.</summary>
            public static Color OldLace
            {
                get { return new Color(253, 245, 230); }
            }

            /// <summary>Gets a system-defined color with the value R:128 G:128 B:0 A:255.</summary>
            public static Color Olive
            {
                get { return new Color(128, 128, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:107 G:142 B:35 A:255.</summary>
            public static Color OliveDrab
            {
                get { return new Color(107, 142, 35); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:165 B:0 A:255.</summary>
            public static Color Orange
            {
                get { return new Color(255, 165, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:69 B:0 A:255.</summary>
            public static Color OrangeRed
            {
                get { return new Color(255, 69, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:218 G:112 B:214 A:255.</summary>
            public static Color Orchid
            {
                get { return new Color(218, 112, 214); }
            }

            /// <summary>Gets a system-defined color with the value R:238 G:232 B:170 A:255.</summary>
            public static Color PaleGoldenrod
            {
                get { return new Color(238, 232, 170); }
            }

            /// <summary>Gets a system-defined color with the value R:152 G:251 B:152 A:255.</summary>
            public static Color PaleGreen
            {
                get { return new Color(152, 251, 152); }
            }

            /// <summary>Gets a system-defined color with the value R:175 G:238 B:238 A:255.</summary>
            public static Color PaleTurquoise
            {
                get { return new Color(175, 238, 238); }
            }

            /// <summary>Gets a system-defined color with the value R:219 G:112 B:147 A:255.</summary>
            public static Color PaleVioletRed
            {
                get { return new Color(219, 112, 147); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:239 B:213 A:255.</summary>
            public static Color PapayaWhip
            {
                get { return new Color(255, 239, 213); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:218 B:185 A:255.</summary>
            public static Color PeachPuff
            {
                get { return new Color(255, 218, 185); }
            }

            /// <summary>Gets a system-defined color with the value R:205 G:133 B:63 A:255.</summary>
            public static Color Peru
            {
                get { return new Color(205, 113, 63); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:192 B:203 A:255.</summary>
            public static Color Pink
            {
                get { return new Color(255, 192, 203); }
            }

            /// <summary>Gets a system-defined color with the value R:221 G:160 B:221 A:255.</summary>
            public static Color Plum
            {
                get { return new Color(221, 160, 221); }
            }

            /// <summary>Gets a system-defined color with the value R:176 G:224 B:230 A:255.</summary>
            public static Color PowderBlue
            {
                get { return new Color(176, 224, 230); }
            }

            /// <summary>Gets a system-defined color with the value R:128 G:0 B:128 A:255.</summary>
            public static Color Purple
            {
                get { return new Color(128, 0, 128); }
            }

            /// <summary>Red component of the color</summary>
            public byte R
            {
                get { return _r; }
                set { _r = value; }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:0 B:0 A:255.</summary>
            public static Color Red
            {
                get { return new Color(255, 0, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:188 G:143 B:143 A:255.</summary>
            public static Color RosyBrown
            {
                get { return new Color(188, 143, 143); }
            }

            /// <summary>Gets a system-defined color with the value R:65 G:105 B:225 A:255.</summary>
            public static Color RoyalBlue
            {
                get { return new Color(65, 105, 225); }
            }

            /// <summary>Gets a system-defined color with the value R:139 G:69 B:19 A:255.</summary>
            public static Color SaddleBrown
            {
                get { return new Color(139, 69, 19); }
            }

            /// <summary>Gets a system-defined color with the value R:250 G:128 B:114 A:255.</summary>
            public static Color Salmon
            {
                get { return new Color(250, 128, 114); }
            }

            /// <summary>Gets a system-defined color with the value R:244 G:164 B:96 A:255.</summary>
            public static Color SandyBrown
            {
                get { return new Color(244, 164, 96); }
            }

            /// <summary>Gets a system-defined color with the value R:46 G:139 B:87 A:255.</summary>
            public static Color SeaGreen
            {
                get { return new Color(46, 139, 87); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:245 B:238 A:255.</summary>
            public static Color SeaShell
            {
                get { return new Color(255, 245, 238); }
            }

            /// <summary>Gets a system-defined color with the value R:160 G:82 B:45 A:255.</summary>
            public static Color Sienna
            {
                get { return new Color(160, 82, 45); }
            }

            /// <summary>Gets a system-defined color with the value R:192 G:192 B:192 A:255.</summary>
            public static Color Silver
            {
                get { return new Color(192, 192, 192); }
            }

            /// <summary>Gets a system-defined color with the value R:135 G:206 B:235 A:255.</summary>
            public static Color SkyBlue
            {
                get { return new Color(135, 206, 235); }
            }

            /// <summary>Gets a system-defined color with the value R:106 G:90 B:205 A:255.</summary>
            public static Color SlateBlue
            {
                get { return new Color(106, 90, 205); }
            }

            /// <summary>Gets a system-defined color with the value R:112 G:128 B:144 A:255.</summary>
            public static Color SlateGray
            {
                get { return new Color(112, 128, 144); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:250 B:250 A:255.</summary>
            public static Color Snow
            {
                get { return new Color(255, 250, 250); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:255 B:127 A:255.</summary>
            public static Color SpringGreen
            {
                get { return new Color(0, 255, 127); }
            }

            /// <summary>Gets a system-defined color with the value R:70 G:130 B:180 A:255.</summary>
            public static Color SteelBlue
            {
                get { return new Color(70, 130, 180); }
            }

            /// <summary>Gets a system-defined color with the value R:210 G:180 B:140 A:255.</summary>
            public static Color Tan
            {
                get { return new Color(210, 180, 140); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:128 B:128 A:255.</summary>
            public static Color Teal
            {
                get { return new Color(0, 128, 128); }
            }

            /// <summary>Gets a system-defined color with the value R:216 G:191 B:216 A:255.</summary>
            public static Color Thistle
            {
                get { return new Color(216, 191, 216); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:99 B:71 A:255.</summary>
            public static Color Tomato
            {
                get { return new Color(255, 99, 71); }
            }

            /// <summary>Gets a system-defined color with the value R:0 G:0 B:0 A:0.</summary>
            public static Color TransparentBlack
            {
                get { return new Color(0, 0, 0, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:255 B:255 A:0.</summary>
            public static Color TransparentWhite
            {
                get { return new Color(255, 255, 255, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:64 G:224 B:208 A:255.</summary>
            public static Color Turquoise
            {
                get { return new Color(64, 224, 208); }
            }

            /// <summary>Gets a system-defined color with the value R:238 G:130 B:238 A:255.</summary>
            public static Color Violet
            {
                get { return new Color(238, 130, 238); }
            }

            /// <summary>Gets a system-defined color with the value R:245 G:222 B:179 A:255.</summary>
            public static Color Wheat
            {
                get { return new Color(245, 222, 179); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:255 B:255 A:255.</summary>
            public static Color White
            {
                get { return new Color(255, 255, 255); }
            }

            /// <summary>Gets a system-defined color with the value R:245 G:245 B:245 A:255.</summary>
            public static Color WhiteSmoke
            {
                get { return new Color(245, 245, 245); }
            }

            /// <summary>Gets a system-defined color with the value R:255 G:255 B:0 A:255.</summary>
            public static Color Yellow
            {
                get { return new Color(255, 255, 0); }
            }

            /// <summary>Gets a system-defined color with the value R:154 G:205 B:50 A:255.</summary>
            public static Color YellowGreen
            {
                get { return new Color(154, 205, 50); }
            }

            /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
            /// <param name="obj">The Object to compare with the current Color.</param>
            public override bool Equals(object obj)
            {
                return ((obj is Color) && Equals((Color)obj));
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return ((R << 24) | (G << 16) | (B << 8) | A).GetHashCode();
            }

            /// <summary>Linearly interpolates between two colors.</summary>
            /// <param name="value1">Source Color.</param>
            /// <param name="value2">Source Color.</param>
            /// <param name="amount">A value between 0 and 1.0 indicating the weight of value2.</param>
            public static Color Lerp(Color value1, Color value2, float amount)
            {
                var r = MathHelper.Lerp(value1.R, value2.R, amount);
                var g = MathHelper.Lerp(value1.G, value2.G, amount);
                var b = MathHelper.Lerp(value1.B, value2.B, amount);
                var a = MathHelper.Lerp(value1.A, value2.A, amount);

                r = Math.Min(byte.MaxValue, Math.Max(r, byte.MinValue));
                g = Math.Min(byte.MaxValue, Math.Max(g, byte.MinValue));
                b = Math.Min(byte.MaxValue, Math.Max(b, byte.MinValue));
                a = Math.Min(byte.MaxValue, Math.Max(a, byte.MinValue));

                return new Color((byte)r, (byte)g, (byte)b, (byte)a);
            }

            /// <summary>Gets a string representation of this object.</summary>
            public override string ToString()
            {
                return string.Format(CultureInfo.CurrentCulture, "{{R:{0} G:{1} B:{2} A:{3}}}", new object[] { R, G, B, A });
            }

            #region IEquatable<Color> Members

            /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
            /// <param name="other">The Color to compare with the current Color.</param>
            public bool Equals(Color other)
            {
                return (R == other.R) && (G == other.G) && (B == other.B) && (A == other.A);
            }

            #endregion

            /// <summary>Compares two objects to determine whether they are the same.</summary>
            /// <param name="a">The object to the left of the equality operator.</param>
            /// <param name="b">The object to the right of the equality operator.</param>
            public static bool operator ==(Color a, Color b)
            {
                return a.Equals(b);
            }

            /// <summary>Compares two objects to determine whether they are different.</summary>
            /// <param name="a">The object to the left of the equality operator.</param>
            /// <param name="b">The object to the right of the equality operator.</param>
            public static bool operator !=(Color a, Color b)
            {
                return !a.Equals(b);
            }
        }
    }
}