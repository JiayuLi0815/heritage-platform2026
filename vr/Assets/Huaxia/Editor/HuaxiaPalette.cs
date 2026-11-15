using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 全局东方色板。所有脚本化场景只能从这里取色，保证跨章节色调统一。
    /// 数值为 sRGB 直觉值，写入材质时由 HuaxiaBuilder.Lin() 转为线性空间。
    /// </summary>
    public static class HuaxiaPalette
    {
        // —— 基础低饱和 ——
        public static readonly Color InkTeal = Hex("2E4A4E");      // 墨青
        public static readonly Color InkTealDeep = Hex("1E3236");  // 深墨青
        public static readonly Color PaperWhite = Hex("EDE4D2");   // 暖纸白
        public static readonly Color PaperWarm = Hex("DCCFB6");    // 旧纸
        public static readonly Color Sand = Hex("C4B295");         // 沙土
        public static readonly Color Rock = Hex("8A8781");         // 岩灰
        public static readonly Color RockDark = Hex("5E5C58");     // 深岩灰
        public static readonly Color BlueGrey = Hex("7C858A");     // 青灰
        public static readonly Color WoodBrown = Hex("6E4B32");    // 木棕
        public static readonly Color WoodDark = Hex("46301F");     // 深木
        public static readonly Color WoodGrey = Hex("7A6A56");     // 旧木灰
        public static readonly Color FoliageGreen = Hex("4E6B45"); // 植物绿
        public static readonly Color FoliageDeep = Hex("33482F");  // 深叶
        public static readonly Color PineGreen = Hex("3A4F42");    // 松绿

        // —— 强调色（克制使用）——
        public static readonly Color Cinnabar = Hex("A6392C");     // 朱砂
        public static readonly Color Azurite = Hex("4A6E7E");      // 石青
        public static readonly Color Malachite = Hex("5C7D63");    // 石绿
        public static readonly Color Amber = Hex("C8963C");        // 琥珀金
        public static readonly Color AmberPale = Hex("E0BE7C");    // 淡金
        public static readonly Color Terracotta = Hex("B4673F");   // 陶土橙
        public static readonly Color AutumnRed = Hex("9C4A38");    // 秋叶

        // —— 序章环境 ——
        public static readonly Color DuskZenith = Hex("33465A");   // 暮色天顶
        public static readonly Color DuskHorizon = Hex("C9A98A");  // 暮色地平线
        public static readonly Color DuskFog = Hex("9AA6AC");      // 暮色雾
        public static readonly Color DuskAmbient = Hex("5E6B78");  // 环境光
        public static readonly Color DuskSun = Hex("F0C089");      // 低角度暖光

        public static Color Hex(string hex)
        {
            byte r, g, b;
            if (!byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out r)) r = 200;
            if (!byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out g)) g = 200;
            if (!byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out b)) b = 200;
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        public static Color A(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }
    }
}
