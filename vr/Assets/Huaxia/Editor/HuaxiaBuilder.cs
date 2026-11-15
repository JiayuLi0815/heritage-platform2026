using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 用程序化几何快速搭建可玩场景，所有物件都留有明确的层级命名，
    /// 后续可直接替换为正式模型、贴图与音效。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        public const string SceneDir = "Assets/Huaxia/Scenes";
        public const string MatDir = "Assets/Huaxia/Materials";

        // 字体系统只有两家，都是 SIL OFL 开源可商用：
        //   文楷（霞鹜文楷）= 手记 / 正文 / 碑文 / 幡名，楷书的温润
        //   宋体（思源宋体）= 匾额 / 标题 / 路引 / 一切要"刻"在石头或木头上的字
        static Font uiText;    // 文楷 Regular —— UI 正文、模态正文、提示
        static Font uiTitle;   // 宋体 Bold —— UI 标题、强调
        static Font plate;     // 文楷 Medium —— 世界空间刻字（幡、碑、站名）
        static Font display;   // 宋体 Bold —— 匾额、书题、路引牌标题
        static Font bookPage;  // 文楷 Medium —— 书页手记
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        public static readonly Color Cream = new Color(0.89f, 0.86f, 0.79f);
        public static readonly Color MutedText = new Color(0.62f, 0.60f, 0.55f);
        public static readonly Color Gold = new Color(0.85f, 0.70f, 0.42f);

        /// <summary>在 Linear 色彩空间下，脚本里按 sRGB 直觉写的颜色需要转换。</summary>
        /// <summary>水面专用层：从点光源的 cullingMask 中排除，避免 ForwardAdd 叠加出屏幕对齐色块。</summary>
        public const int WaterLayer = 8;

        public static Color Lin(Color c) { return c.linear; }

        // ---------------------------------------------------------------- 入口

        [MenuItem("Huaxia/构建全部场景 (序章古道 + 良渚 + 秦陵 + 泉州)")]
        public static void BuildAll()
        {
            BuildBookPrefab();
            BuildArchiveScene();
            BuildLiangzhuScene();
            BuildQinlingScene();
            BuildQuanzhouScene();
            BuildEpilogueScene();
            ApplyProjectSettings();
            ConfigureAudioClips();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Huaxia] 全部场景构建完成");
        }

        [MenuItem("Huaxia/仅构建：序章古道")]
        public static void BuildArchiveMenu()
        {
            BuildArchiveScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Huaxia/仅构建：良渚古城")]
        public static void BuildLiangzhuMenu()
        {
            BuildLiangzhuScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Huaxia/仅构建：秦陵")]
        public static void BuildQinlingMenu()
        {
            BuildQinlingScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Huaxia/仅构建：泉州港口")]
        public static void BuildQuanzhouMenu()
        {
            BuildQuanzhouScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Huaxia/仅构建：终章藏卷阁")]
        public static void BuildEpilogueMenu()
        {
            BuildEpilogueScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Huaxia/清除探索进度")]
        public static void ClearProgress()
        {
            GameState.ResetAll();
            Debug.Log("[Huaxia] 探索进度已清除");
        }

        /// <summary>给当前场景放一个音乐锚点。音乐本体由常驻的 AmbientDirector 播放。</summary>
        public static void AddSceneAmbient(string clip, float volume)
        {
            var go = new GameObject("SceneAmbient");
            var amb = go.AddComponent<Huaxia.SceneAmbient>();
            amb.clipName = clip;
            amb.volume = volume;
        }

        // ---------------------------------------------------------------- 资源

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void LoadFonts()
        {
            if (uiText != null) return;
            uiText = LoadFontAsset(KaiFontDir + "/LXGWWenKai-Regular.ttf");
            bookPage = LoadFontAsset(KaiFontDir + "/LXGWWenKai-Medium.ttf");
            plate = bookPage;
            uiTitle = LoadFontAsset(KaiFontDir + "/SourceHanSerifCN-Bold.ttf");
            display = uiTitle;
            if (uiText == null) uiText = bookPage;
            if (bookPage == null) bookPage = uiText;
            if (plate == null) plate = uiText;
            if (uiTitle == null) uiTitle = bookPage;
            if (display == null) display = plate;
            if (uiText == null)
                Debug.LogError("[Huaxia] 楷体导入失败，UI 将无法显示中文");
            if (uiTitle == null)
                Debug.LogWarning("[Huaxia] 宋体缺失，匾额回退为楷体");
        }

        const string KaiFontDir = "Assets/Huaxia/Fonts";

        /// <summary>字体资源可能在 batchmode 首次构建时尚未导入，缺失时强制导入后重试。</summary>
        static Font LoadFontAsset(string path)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (f != null) return f;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        static Shader StylizedShader
        {
            get
            {
                var s = Shader.Find("Huaxia/StylizedLit");
                return s != null ? s : Shader.Find("Standard");
            }
        }

        /// <summary>
        /// 按材质名推断细节纹理类型，保证同类材质在全工程外观一致。
        /// 判断有先后：越具体的工艺越先命中（茅草、树皮、玉、瓦…），最后才落到 stone。
        /// </summary>
        static string DetailKindOf(string name)
        {
            string n = name.ToLowerInvariant();

            // 茅草/稻草/苇席：拉长的草茎，和树皮、叶簇要分开
            if (Has(n, "thatch", "straw", "reed", "茅", "草束", "苇")) return "thatch";
            if (Has(n, "bark", "trunk", "树皮")) return "bark";
            if (Has(n, "culm", "竹材")) return "wood";
        if (Has(n, "postfill", "filldirt", "骨泥")) return "earth";
            if (Has(n, "leaf", "canopy", "foliage", "bamboo", "pine", "cypress", "shrub",
                       "grass", "moss", "rice", "叶", "簇", "竹", "松", "柏", "草", "稻", "苔")) return "foliage";

            if (Has(n, "bronze", "patina", "copper", "铜", "青")) return "bronze";
            if (Has(n, "pottery", "sherd", "ceramic", "terracotta", "陶", "俑", "瓦当")) return "pottery";
            if (Has(n, "jade", "玉")) return "jade";

            // 屋面必须在「脊」之前判断，否则 RoofRidge 会落到远山
            if (Has(n, "tile", "roof", "eave", "瓦", "脊", "檐")) return "tile";
            if (Has(n, "ridge", "mountain", "qnfar", "远山")) return "ridge";

            if (Has(n, "rammed", "loess", "pise", "dimple", "夯", "黄土")) return "rammed";
            if (Has(n, "brick", "砖")) return "brick";

            if (Has(n, "wood", "post", "beam", "column", "rafter", "bracket", "plank", "board",
                       "frame", "lattice", "timber", "hull", "door", "木", "柱", "梁", "榫", "板", "门")) return "wood";
            if (Has(n, "metal", "iron", "gold", "brass", "金", "铁")) return "metal";

            // 草拌泥（木骨泥墙）：史前建筑墙体的主要做法，比素土多了草筋痕迹
            if (Has(n, "mud", "daub", "wattle", "泥墙", "草拌")) return "daub";

            if (Has(n, "earth", "soil", "dirt", "sand", "clay", "ash", "charcoal", "土", "泥", "沙", "灰")) return "earth";

            if (Has(n, "paper", "scroll", "纸", "卷")) return "paper";
            if (Has(n, "cloth", "fabric", "silk", "petal", "布", "绢", "瓣")) return "fabric";
            if (Has(n, "plaster", "wall", "screen", "lantern", "flag", "cinnabar",
                       "墙", "屏", "灯", "幡", "朱")) return "plaster";

            if (Has(n, "stone", "rock", "step", "bridge", "pave", "plinth", "base", "stela", "tablet",
                       "gravel", "passage", "石", "岩", "阶", "台", "碑", "基")) return "stone";

            return "stone";
        }

        static bool Has(string s, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
                if (s.Contains(keys[i])) return true;
            return false;
        }

        /// <summary>1 / 该值 = 一张贴图覆盖的米数。</summary>
        static float DetailScaleOf(string kind)
        {
            switch (kind)
            {
                case "wood": return 0.55f;      // 1.8 m 一张，木纹尺度
                case "stone": return 0.28f;     // 3.6 m，石面颗粒
                case "earth": return 0.30f;     // 3.3 m，土面斑驳
                case "daub": return 0.40f;      // 2.5 m，草筋与抹泥痕
                case "foliage": return 0.32f;
                case "plaster": return 0.50f;   // 2 m，抹面刮痕与砂粒
                case "paper": return 0.34f;
                case "fabric": return 0.55f;
                case "metal": return 0.70f;
                case "tile": return 0.82f;      // 1.2 m 一张，9 条瓦垄刚好是一间面阔
                case "rammed": return 0.30f;    // 11 层/张 → 夯层约 30 cm，接近实测
                case "brick": return 0.55f;     // 13 皮/张 → 砖厚约 14 cm
                case "thatch": return 0.90f;
                case "bark": return 0.50f;
                case "bronze": return 0.60f;
                case "pottery": return 0.90f;
                case "jade": return 0.35f;
                case "ridge": return 0.045f;    // 远山只留极粗起伏
                default: return 0.30f;
            }
        }

        static float DetailStrengthOf(string kind)
        {
            switch (kind)
            {
                case "wood": return 0.50f;
                case "stone": return 0.26f;
                case "earth": return 0.34f;
                case "daub": return 0.40f;
                case "foliage": return 0.55f;
                case "plaster": return 0.26f;
                case "paper": return 0.14f;
                case "fabric": return 0.22f;
                case "metal": return 0.30f;
                case "tile": return 0.60f;
                case "rammed": return 0.42f;
                case "brick": return 0.34f;
                case "thatch": return 0.55f;
                case "bark": return 0.50f;
                case "bronze": return 0.35f;
                case "pottery": return 0.30f;
                case "jade": return 0.12f;
                case "ridge": return 0.16f;
                default: return 0.30f;
            }
        }

        static float BumpScaleOf(string kind)
        {
            switch (kind)
            {
                case "wood": return 0.42f;
                case "stone": return 0.42f;
                case "earth": return 0.72f;
                case "daub": return 0.62f;
                case "foliage": return 0.40f;
                case "plaster": return 0.40f;
                case "paper": return 0.10f;
                case "fabric": return 0.16f;
                case "metal": return 0.35f;
                case "tile": return 1.00f;
                case "rammed": return 0.48f;
                case "brick": return 0.75f;
                case "thatch": return 0.55f;
                case "bark": return 0.95f;
                case "bronze": return 0.30f;
                case "pottery": return 0.45f;
                case "jade": return 0.10f;
                case "ridge": return 0.05f;
                default: return 0.40f;
            }
        }

        /// <summary>宏观斑驳的频率（1 / 米）。小构件要更密，大地面才用很疏的一层。</summary>
        static float MacroScaleOf(string kind)
        {
            switch (kind)
            {
                case "earth": return 0.11f;
                case "daub": return 0.13f;
                case "plaster": return 0.16f;
                case "stone": return 0.12f;
                case "rammed": return 0.10f;
                case "brick": return 0.12f;
                case "tile": return 0.10f;
                case "wood": return 0.16f;
                default: return 0.06f;
            }
        }

        static float MacroStrengthOf(string kind)
        {
            switch (kind)
            {
                case "stone": return 0.13f;
                case "rammed": return 0.18f;
                case "earth": return 0.22f;
                case "daub": return 0.22f;
                case "foliage": return 0.40f;
                case "tile": return 0.26f;
                case "wood": return 0.22f;
                case "plaster": return 0.22f;
                case "paper": return 0.10f;
                case "jade": return 0.08f;
                case "ridge": return 0.10f;
                default: return 0.24f;
            }
        }

        public static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.2f)
        {
            return MatAs(name, color, null, metallic, smoothness);
        }

        /// <summary>创建/更新统一风格材质。detail 为 null 时按名称自动推断。</summary>
        public static Material MatAs(string name, Color color, string detail, float metallic = 0f, float smoothness = 0.2f)
        {
            Material cached;
            if (matCache.TryGetValue(name, out cached) && cached != null) return cached;

            // 名字能推断出具体工艺就用工艺纹理；只有名字落回笼统的 stone/earth 时才采用调用点的提示
            string kind = DetailKindOf(name);
            if (!string.IsNullOrEmpty(detail) && (kind == "stone" || kind == "earth")) kind = detail;

            if (!AssetDatabase.IsValidFolder(MatDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
                AssetDatabase.CreateFolder("Assets/Huaxia", "Materials");
            }

            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(StylizedShader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = StylizedShader;

            var tex = StylizedTextureFactory.Get(kind);
            m.SetTexture("_MainTex", tex);
            m.SetFloat("_DetailScale", DetailScaleOf(kind));
            m.SetFloat("_DetailStrength", DetailStrengthOf(kind));
            m.SetTexture("_BumpMap", StylizedTextureFactory.GetNormal(kind));
            m.SetFloat("_BumpScale", BumpScaleOf(kind));
            m.SetFloat("_MacroScale", MacroScaleOf(kind));
            m.SetFloat("_MacroStrength", MacroStrengthOf(kind));
            m.SetColor("_Color", Lin(color));
            m.SetFloat("_Metallic", Mathf.Clamp(metallic, 0f, 0.1f));
            m.SetFloat("_Glossiness", Mathf.Clamp(smoothness, 0f, 0.45f));
            // _ShadowTint 是乘在固有色上的暗部系数：保持冷调偏色，但亮度归一到 0.68。
            // 之前 0.74 + _Wrap 0.70 两层一起把明暗交界线抹平了，画面因此读不出体积。
            var tint = Color.Lerp(HuaxiaPalette.DuskAmbient, color, 0.14f);
            float lum = 0.2126f * tint.r + 0.7152f * tint.g + 0.0722f * tint.b;
            float k = 0.68f / Mathf.Max(lum, 0.001f);
            m.SetColor("_ShadowTint", Lin(new Color(
                Mathf.Clamp01(tint.r * k), Mathf.Clamp01(tint.g * k), Mathf.Clamp01(tint.b * k), 1f)));
            m.SetFloat("_Wrap", 0.46f);
            m.SetFloat("_RampSoftness", 0.42f);
            m.SetFloat("_RimStrength", kind == "foliage" ? 0.34f : 0.16f);
            m.SetFloat("_EmissionStrength", 1f);
            EditorUtility.SetDirty(m);

            matCache[name] = m;
            return m;
        }

        public static Material EmissiveMat(string name, Color color, Color emission, float metallic = 0f, float smoothness = 0.4f,
                                           float strength = 1f)
        {
            var m = Mat(name, color, metallic, smoothness);
            m.SetColor("_EmissionColor", Lin(emission));
            m.SetFloat("_EmissionStrength", strength);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// 地上引导圈用的加法柔光材质：透明度由 _Alpha 控制，压得低才是「一圈光」，
        /// 用不透明的自发光材质会变成扣在地上的塑料环。
        /// </summary>
        public static Material GuideGlowMat(string name, Color tint, float alpha, float intensity = 1.25f,
                                            float edgeSoft = 0.3f, float hotEdge = 0.45f)
        {
            Material cached;
            if (matCache.TryGetValue(name, out cached) && cached != null) return cached;

            if (!AssetDatabase.IsValidFolder(MatDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
                AssetDatabase.CreateFolder("Assets/Huaxia", "Materials");
            }

            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Huaxia/GuideGlow");
            if (m == null)
            {
                m = new Material(shader != null ? shader : StylizedShader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null)
            {
                m.shader = shader;
            }

            m.SetColor("_Tint", Lin(tint));
            m.SetFloat("_Alpha", Mathf.Clamp01(alpha));
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_EdgeSoft", edgeSoft);
            m.SetFloat("_HotEdge", hotEdge);
            EditorUtility.SetDirty(m);

            matCache[name] = m;
            return m;
        }

        public static Material WaterMat(string name, Color color, float smoothness = 0.85f)
        {
            Material cached;
            if (matCache.TryGetValue(name, out cached) && cached != null) return cached;

            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Huaxia/StylizedWater");
                m = new Material(sh != null ? sh : Shader.Find("Standard"));
                AssetDatabase.CreateAsset(m, path);
            }

            var shader = Shader.Find("Huaxia/StylizedWater");
            if (shader != null)
            {
                m.shader = shader;
                                m.SetColor("_DeepColor", Lin(Color.Lerp(HuaxiaPalette.InkTealDeep, color, 0.55f)));
                m.SetColor("_ShallowColor", Lin(Color.Lerp(HuaxiaPalette.BlueGrey, color, 0.62f)));
                m.SetColor("_SkyTint", Lin(Color.Lerp(HuaxiaPalette.DuskAmbient, HuaxiaPalette.PaperWarm, 0.45f)) * 0.55f);
                m.SetFloat("_FresnelPower", 3.2f);
                m.SetFloat("_WaveStrength", 0.3f);
                m.SetFloat("_Glossiness", smoothness);
                m.SetFloat("_AlphaBase", 0.88f);
            }
            EditorUtility.SetDirty(m);

            matCache[name] = m;
            return m;
        }

        public static Material TerrainMat(string assetName = "TerrainVertexColor")
        {
            string path = MatDir + "/" + assetName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(StylizedShader);
                AssetDatabase.CreateAsset(m, path);
            }

            var shader = Shader.Find("Huaxia/StylizedLit");
            if (shader != null) m.shader = shader;

            m.SetTexture("_MainTex", StylizedTextureFactory.Get("earth"));
            m.SetFloat("_DetailScale", 0.1f);
            m.SetFloat("_DetailStrength", 0.6f);
            m.SetTexture("_BumpMap", StylizedTextureFactory.GetNormal("earth"));
            m.SetFloat("_BumpScale", 0.5f);
            m.SetFloat("_MacroScale", 0.02f);
            m.SetFloat("_MacroStrength", 0.26f);
            m.SetColor("_Color", Lin(Color.white));
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", 0.06f);
            m.SetFloat("_VertexColorInfluence", 1f);
            m.SetColor("_ShadowTint", Lin(new Color(0.66f, 0.70f, 0.78f)));
            m.SetFloat("_Wrap", 0.72f);
            m.SetFloat("_RimStrength", 0.06f);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material SkyMat(string name, Color skyTint, Color ground, float atmosphere, float exposure, float warmth = 1f)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Skybox/Panoramic");

            // 天顶压深、中段保持冷蓝、地平线才转暖：三段分明，天上才有「高度」。
            // 原来 mid 掺了 24% 暖纸色，整片天空被拉成一块灰米色，远山也就没有衬托。
            Color zenith = Color.Lerp(skyTint, HuaxiaPalette.InkTeal, 0.32f) * 0.94f;
            Color mid = Color.Lerp(skyTint, Color.Lerp(skyTint, Color.white, 0.20f), 0.55f);
            Color horizon = Color.Lerp(Color.Lerp(skyTint, HuaxiaPalette.PaperWarm, 0.55f),
                HuaxiaPalette.DuskHorizon, 0.55f * warmth);

            var tex = StylizedTextureFactory.SkyGradient(name, zenith, mid, horizon, ground, atmosphere);

            m.SetTexture("_MainTex", tex);
            m.SetFloat("_Mapping", 1f);        // 1 = Latitude Longitude
            m.SetFloat("_ImageType", 0f);      // 0 = 360 degrees
            // 天空必须比地面暗一档，否则地平线永远是过曝的白，画面没有中调
            m.SetFloat("_Exposure", Mathf.Max(0.2f, exposure));
            m.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- 几何

        public static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        // 方块贴图的世界尺寸：一张贴图铺 1.5 米，UV 按构件实际大小展开
        const float BoxTileMeters = 1.5f;

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, BoxTileMeters);
            var r = go.AddComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        public static GameObject Cyl(string name, Transform parent, Vector3 pos, float radius, float height, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            var r = go.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Ball(string name, Transform parent, Vector3 pos, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * radius * 2f;
            var r = go.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject MeshObject(string name, Transform parent, Vector3 pos, Mesh mesh, Material mat, bool collider = false)
        {
            return MeshObject(name, parent, pos, mesh, mat, collider, true);
        }

        /// <summary>
        /// castShadows=false 用于整块地形：地形的包围盒有 800 m，
        /// 相机附近的阴影贴图照样会把几百米外的山全画进去，
        /// 结果整座陵园被四周山体罩在阴影里，太阳等于没出来。
        /// 关掉地形投射后，坡面的明暗还靠 N·L 和雾，只是不再互相投影。
        /// </summary>
        public static GameObject MeshObject(string name, Transform parent, Vector3 pos, Mesh mesh, Material mat,
                                            bool collider, bool castShadows)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!castShadows)
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        // ---------------------------------------------------------------- UI

        public static Text TextAt(Transform parent, string name, string content, int size, Color color,
                                  TextAnchor align, Vector2 pivotAnchor, Vector2 pos, Vector2 sizeDelta, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = pivotAnchor;
            rt.anchorMax = pivotAnchor;
            rt.pivot = pivotAnchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            var t = go.AddComponent<Text>();
            t.font = bold ? uiTitle : uiText;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = content;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.25f;
            t.raycastTarget = false;
            return t;
        }

        public static Text TextStretch(Transform parent, string name, string content, int size, Color color,
                                       TextAnchor align, Vector2 offsetMin, Vector2 offsetMax, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var t = go.AddComponent<Text>();
            t.font = bold ? uiTitle : uiText;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = content;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.3f;
            t.raycastTarget = false;
            return t;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                          Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        public static UIManager CreateHUD(string hint)
        {
            LoadFonts();

            var canvasGO = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var ui = canvasGO.AddComponent<UIManager>();

            // 左上：章节与任务
            var chapterPanel = Panel(canvasGO.transform, "ChapterPanel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(26f, -26f), new Vector2(600f, 366f), new Color(0.02f, 0.025f, 0.035f, 0.62f));
            ui.chapterTitle = TextAt(chapterPanel, "Title", "文明档案馆", 28, Cream, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(560f, 40f), true);
            ui.chapterSubtitle = TextAt(chapterPanel, "Subtitle", "", 17, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(21f, -56f), new Vector2(560f, 30f));
            ui.objectivesText = TextAt(chapterPanel, "Objectives", "", 17, Cream, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(20f, -92f), new Vector2(560f, 200f));

            // 目标栏底部：离最近一处线索还有多远、在哪个方向（由 ObjectiveGuide 每半秒写）
            Panel(chapterPanel, "GuideLine", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -300f), new Vector2(560f, 1f), new Color(0.85f, 0.70f, 0.42f, 0.35f));
            ui.guideText = TextAt(chapterPanel, "Guide", "", 18, new Color(0.95f, 0.80f, 0.50f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(20f, -310f), new Vector2(560f, 44f));

            // 右上：操作提示
            var hintText = TextAt(canvasGO.transform, "Hint", hint, 16, new Color(0.75f, 0.73f, 0.68f, 0.85f),
                TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-28f, -30f), new Vector2(900f, 30f));
            ui.hintText = hintText;

            // 准星
            var cross = TextAt(canvasGO.transform, "Crosshair", "+", 26, new Color(0.9f, 0.88f, 0.8f, 0.65f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            ui.crosshair = cross.gameObject;

            // 底部交互提示
            var promptPanel = Panel(canvasGO.transform, "PromptPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 64f), new Vector2(1100f, 50f), new Color(0.02f, 0.025f, 0.035f, 0.55f));
            ui.promptText = TextStretch(promptPanel, "PromptText", "", 21, Cream, TextAnchor.MiddleCenter,
                new Vector2(16f, 6f), new Vector2(-16f, -6f));

            // 顶部浮层提示
            var toastPanel = Panel(canvasGO.transform, "ToastPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(760f, 46f), new Color(0.02f, 0.025f, 0.035f, 0.6f));
            ui.toastRoot = toastPanel.gameObject;
            ui.toastText = TextStretch(toastPanel, "ToastText", "", 19, Gold, TextAnchor.MiddleCenter,
                new Vector2(12f, 4f), new Vector2(-12f, -4f));

            // 模态窗
            var modalRoot = new GameObject("ModalRoot", typeof(RectTransform));
            modalRoot.transform.SetParent(canvasGO.transform, false);
            var mrt = modalRoot.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;
            var dim = modalRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = false;
            ui.modalRoot = modalRoot;

            var panel = Panel(modalRoot.transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1120f, 760f), new Color(0.045f, 0.05f, 0.06f, 0.97f));

            ui.modalTitle = TextAt(panel, "ModalTitle", "", 36, Cream, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(44f, -34f), new Vector2(1030f, 50f), true);
            ui.modalSubtitle = TextAt(panel, "ModalSubtitle", "", 19, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(45f, -88f), new Vector2(1030f, 32f));

            var divider = Panel(panel, "Divider", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(44f, -132f), new Vector2(1032f, 2f), new Color(0.85f, 0.70f, 0.42f, 0.5f));

            ui.modalBody = TextAt(panel, "ModalBody", "", 23, new Color(0.86f, 0.84f, 0.78f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(44f, -158f), new Vector2(1030f, 500f));

            ui.modalFooter = TextAt(panel, "ModalFooter", "", 20, Gold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(900f, 36f));

            // 遗产立牌翻开的那本册页（层序在 ModalRoot 之上、黑场之下）
            CreateHeritagePanel(canvasGO.transform);

            // 淡入淡出
            var fadeGO = new GameObject("Fade", typeof(RectTransform));
            fadeGO.transform.SetParent(canvasGO.transform, false);
            var frt = fadeGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fade = fadeGO.AddComponent<Image>();
            fade.color = new Color(0f, 0f, 0f, 0f);
            fade.raycastTarget = false;
            ui.fadeImage = fade;

            // 黑场上留一句话（终章末语）。平时空着，EndExperience 才写。
            var fadeLabel = TextAt(fadeGO.transform, "FadeText", "", 40, Cream, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 120f));
            ui.fadeText = fadeLabel;

            canvasGO.AddComponent<ObjectiveGuide>();
            return ui;
        }

        // ---------------------------------------------------------------- 玩家

        public static GameObject CreatePlayer(Vector3 pos, float yaw)
        {
            var player = new GameObject("Player", typeof(CharacterController));
            player.transform.position = pos;
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var cc = player.GetComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.34f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;

            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.66f, 0f);

            var cam = camGO.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            cam.fieldOfView = 66f;

            // 《河山册》挂在相机下的一只挂点上：抬手就能看，放下就走路
            var bookSocket = new GameObject("BookSocket");
            bookSocket.transform.SetParent(camGO.transform, false);
            var holder = bookSocket.AddComponent<BookHolder>();
            // 开场不自动把书抬到眼前：按 Tab 才取。书一直挂在腰侧，低头看得见一角。
            holder.greetOnStart = false;

            var controller = player.AddComponent<DesktopPlayerController>();
            controller.playerCamera = cam;

            // 头显接上时自己接管（XRSettings.isDeviceActive）；桌面端这个组件会禁用自己
            player.AddComponent<XRInputBridge>();

            return player;
        }

        /// <summary>
        /// 给主相机挂全屏调色，并按章节赋预设。五个章节各有一条色调：
        /// 序章暮色偏冷、良渚晨雾偏青、秦陵秋土偏橙、泉州海雾偏青白、终章室内偏暖。
        /// </summary>
        public static CinematicGrade ApplyGrade(GameObject player, string preset)
        {
            if (player == null) return null;
            var cam = player.GetComponentInChildren<Camera>();
            if (cam == null) return null;

            var g = cam.gameObject.GetComponent<CinematicGrade>();
            if (g == null) g = cam.gameObject.AddComponent<CinematicGrade>();

            switch (preset)
            {
                case "archive":
                    g.exposure = 1.06f; g.contrast = 1.08f; g.saturation = 0.95f; g.vignette = 0.34f;
                    g.gainColor = new Color(1.02f, 1.00f, 0.98f, 1f);
                    g.liftColor = new Color(0.004f, 0.004f, 0.014f, 1f);
                    g.gammaColor = new Color(1.0f, 1.0f, 1.02f, 1f);
                    g.shadowsTint = new Color(0.90f, 0.96f, 1.08f, 0.45f);
                    g.highlightsTint = new Color(1.06f, 1.01f, 0.95f, 0.45f);
                    break;

                case "qinling":
                    g.exposure = 1.00f; g.contrast = 1.12f; g.saturation = 0.94f; g.vignette = 0.32f;
                    g.gainColor = new Color(1.03f, 1.00f, 0.95f, 1f);
                    g.liftColor = new Color(0.008f, 0.006f, 0.004f, 1f);
                    g.gammaColor = new Color(1.0f, 1.0f, 1.01f, 1f);
                    g.shadowsTint = new Color(0.88f, 0.96f, 1.10f, 0.50f);
                    g.highlightsTint = new Color(1.06f, 1.01f, 0.94f, 0.40f);
                    break;

                case "quanzhou":
                    g.exposure = 1.02f; g.contrast = 1.10f; g.saturation = 0.96f; g.vignette = 0.30f;
                    g.gainColor = new Color(0.99f, 1.00f, 1.03f, 1f);
                    g.liftColor = new Color(0.004f, 0.008f, 0.012f, 1f);
                    g.gammaColor = new Color(1.0f, 1.0f, 1.01f, 1f);
                    g.shadowsTint = new Color(0.90f, 0.98f, 1.06f, 0.40f);
                    g.highlightsTint = new Color(1.04f, 1.02f, 0.98f, 0.35f);
                    break;

                case "epilogue":
                    g.exposure = 1.06f; g.contrast = 1.06f; g.saturation = 1.00f; g.vignette = 0.36f;
                    g.gainColor = new Color(1.05f, 0.99f, 0.92f, 1f);
                    g.liftColor = new Color(0.010f, 0.006f, 0.004f, 1f);
                    g.gammaColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                    g.shadowsTint = new Color(0.92f, 0.96f, 1.05f, 0.40f);
                    g.highlightsTint = new Color(1.08f, 1.00f, 0.90f, 0.50f);
                    break;

                default: // liangzhu
                    g.exposure = 1.00f; g.contrast = 1.10f; g.saturation = 0.94f; g.vignette = 0.30f;
                    g.gainColor = new Color(1.00f, 1.02f, 1.00f, 1f);
                    g.liftColor = new Color(0.006f, 0.008f, 0.008f, 1f);
                    g.gammaColor = new Color(1.0f, 1.0f, 1.0f, 1f);
                    g.shadowsTint = new Color(0.88f, 0.98f, 1.10f, 0.45f);
                    g.highlightsTint = new Color(1.05f, 1.02f, 0.96f, 0.35f);
                    break;
            }
            return g;
        }

        public static Camera CreateAerialCamera(Vector3 pos, Vector3 euler)
        {
            var go = new GameObject("Aerial Camera", typeof(Camera));
            go.transform.position = pos;
            go.transform.eulerAngles = euler;
            var cam = go.GetComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 60f;
            cam.depth = 5f;
            go.SetActive(false);
            return cam;
        }

        // ---------------------------------------------------------------- 档案节点

        public static Interactable CreateMapNode(Transform parent, string name, Vector3 mapPos,
                                                 string label, bool unlocked, string sceneToLoad,
                                                 string title, string subtitle, string body,
                                                 Vector3 labelOffset)
        {
            var root = Empty("Node_" + name, parent, mapPos);

            // 东方木牌：石基座 + 木框 + 纸面 + 朱砂印章。不用发光柱、不用光束。
            var baseMat = Mat("TabletBase", HuaxiaPalette.RockDark, 0f, 0.22f);
            var frameMat = Mat("TabletFrame", HuaxiaPalette.WoodDark, 0f, 0.16f);
            var panelMat = Mat(unlocked ? "TabletPanelOpen" : "TabletPanelLocked",
                unlocked ? HuaxiaPalette.PaperWarm : HuaxiaPalette.Rock, 0f, 0.1f);
            var sealMat = EmissiveMat("TabletSeal",
                unlocked ? HuaxiaPalette.Cinnabar : HuaxiaPalette.RockDark,
                (unlocked ? HuaxiaPalette.Cinnabar : HuaxiaPalette.RockDark) * 0.9f, 0f, 0.35f);

            Box("Step", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(1.02f, 0.10f, 0.80f), baseMat);
            Box("Plinth", root.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.84f, 0.28f, 0.60f), baseMat);
            Box("Frame", root.transform, new Vector3(0f, 1.06f, 0f), new Vector3(0.66f, 1.38f, 0.08f), frameMat);
            Box("Panel", root.transform, new Vector3(0f, 1.06f, -0.045f), new Vector3(0.55f, 1.24f, 0.03f), panelMat);

            // 牌顶小瓦檐
            var cap = MeshObject("Cap", root.transform, new Vector3(0f, 1.75f, 0f),
                MeshFactory.CurvedRoof(0.46f, 0.18f, 0.15f, 0.05f, 0.035f, 8, 6),
                Mat("RoofTile", HuaxiaPalette.InkTeal, 0f, 0.2f));

            var seal = Box("Seal", root.transform, new Vector3(-0.17f, 0.60f, -0.065f),
                new Vector3(0.10f, 0.10f, 0.012f), sealMat, false);
            if (unlocked) seal.AddComponent<Pulse>().amplitude = 0.06f;

            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 1.15f, -0.55f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Lin(unlocked ? HuaxiaPalette.AmberPale : HuaxiaPalette.BlueGrey);
            light.range = 3.2f;
            light.intensity = unlocked ? 1.15f : 0.30f;
            light.shadows = LightShadows.None;

            // 竖排文字
            string vertical = string.Empty;
            for (int i = 0; i < label.Length; i++)
                vertical += (i == 0 ? "" : "\n") + label[i];
            CreateTextPlate(root.transform, vertical, new Vector3(0f, 1.06f, -0.068f), Vector3.zero,
                new Vector2(0.48f, 1.16f), 62,
                unlocked ? HuaxiaPalette.InkTealDeep : HuaxiaPalette.RockDark, title: true);

            var color = unlocked ? HuaxiaPalette.AmberPale : HuaxiaPalette.BlueGrey;
            _ = labelOffset;

            var interactable = root.AddComponent<Interactable>();
            interactable.kind = InteractKind.Chapter;
            interactable.prompt = unlocked ? "[E] 查看章节" : "[E] 查看";
            interactable.title = title;
            interactable.subtitle = subtitle;
            interactable.body = body;
            interactable.sceneToLoad = unlocked ? sceneToLoad : null;
            interactable.markerRenderer = seal.GetComponent<Renderer>();
            interactable.markerColor = color;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.85f;
            trigger.center = new Vector3(0f, 1.0f, 0f);

            return interactable;
        }

        /// <summary>世界空间固定文字板（不朝向摄像机），用于匾额、木牌、石碑。</summary>
        private static readonly Dictionary<Font, Material> singleSidedTextMats = new Dictionary<Font, Material>();

        /// <summary>文字牌用单面材质：默认 UI 材质是 Cull Off，从背面看到的就是反字。</summary>
        private static Material SingleSidedTextMaterial(Font font)
        {
            if (font == null) return null;
            if (singleSidedTextMats.TryGetValue(font, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Huaxia/TextSingleSided");
            if (shader == null) return null;

            var mat = new Material(shader) { name = "TextSingleSided_" + font.name };
            mat.SetTexture("_MainTex", font.material != null ? font.material.mainTexture : null);
            singleSidedTextMats[font] = mat;
            return mat;
        }

        /// <param name="title">匾额、书题、路引这类"刻在石木上"的大字用宋体；幡、碑、站名用楷体。</param>
        public static GameObject CreateTextPlate(Transform parent, string text, Vector3 localPos, Vector3 eulerAngles,
                                                 Vector2 sizeMeters, int fontSize, Color color, bool title = false)
        {
            // 画布内部从 500 像素放大到 1000 像素，字号同步 ×2：
            // 牌子的世界尺寸不变，但字形按两倍分辨率栅格化，走近了不再发虚。
            const float Units = 1000f;
            const int FontScale = 2;

            var go = new GameObject("Plate", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = eulerAngles;
            go.transform.localScale = Vector3.one * (sizeMeters.x / Units);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 3;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Units, Units * sizeMeters.y / Mathf.Max(0.01f, sizeMeters.x));

            var t = TextStretch(go.transform, "Text", text, fontSize * FontScale, color, TextAnchor.MiddleCenter,
                new Vector2(8f, 8f), new Vector2(-8f, -8f));
            t.font = (title && display != null) ? display : (plate != null ? plate : uiText);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.lineSpacing = 1.0f;

            // 背面补一片同文，避免从背后看到镜像文字。
            // 注意：Plate 的 localScale 很小（sizeMeters.x/1000），偏移量必须按米换算，
            // 且方向取 +Z（朝牌面后方），否则两片文字几乎共面 -> 背面读到的是反字。
            float unitsPerMeter = Units / Mathf.Max(0.01f, sizeMeters.x);
            var back = TextStretch(go.transform, "TextBack", text, fontSize * FontScale, color, TextAnchor.MiddleCenter,
                new Vector2(8f, 8f), new Vector2(-8f, -8f));
            back.font = (title && display != null) ? display : (plate != null ? plate : uiText);
            back.horizontalOverflow = HorizontalWrapMode.Wrap;
            back.lineSpacing = 1.0f;
            back.rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            back.rectTransform.localPosition = new Vector3(0f, 0f, 0.006f * unitsPerMeter);

            var singleSided = SingleSidedTextMaterial(t.font);
            if (singleSided != null)
            {
                t.material = singleSided;
                back.material = singleSided;
            }
            else
            {
                Debug.LogWarning("[TextPlate] 单面文字材质缺失，使用默认 UI 材质：" + text.Replace("\n", "/"));
            }
            return go;
        }

        public static GameObject CreateInfoPoint(Transform parent, string name, Vector3 pos,
                                                 string title, string subtitle, string body,
                                                 string objectiveId, Color accent,
                                                 SourceTier tier = SourceTier.Confirmed,
                                                 string tierNote = null,
                                                 string heritageId = null,
                                                 Transform leadTo = null,
                                                 string endScene = null, string endSpawnId = null,
                                                 string endLine = null,
                                                 string number = null)
        {
            var root = Empty("InfoPoint_" + name, parent, pos);

            var haloMat = EmissiveMat("Halo_" + name, accent * 0.5f, accent * 0.34f, 0f, 0.5f);
            var coreMat = EmissiveMat("Core_" + name, accent, accent * 1.5f, 0f, 0.7f);

            var ring = Cyl("Halo", root.transform, new Vector3(0f, 1.25f, 0f), 0.34f, 0.02f, haloMat);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var groundRing = Cyl("GroundRing", root.transform, new Vector3(0f, 0.03f, 0f), 0.72f, 0.015f, haloMat);
            groundRing.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            // 光柱高 3 米、顶上挂一颗珠子，站远了也能认出「那边有一个点」。
            // 但柱径必须很细：0.05 半径 + 0.9 强度在 ACES 下会烧成一根白塑料杆，
            // 画面正中立一根白杆，比没有指引更糟。0.016 半径 + 0.34 强度，只留一道光丝。
            var beam = Cyl("Beam", root.transform, new Vector3(0f, 1.55f, 0f), 0.016f, 3.1f, haloMat);
            beam.GetComponent<Renderer>().sharedMaterial = haloMat;
            var core = Ball("Core", root.transform, new Vector3(0f, 1.28f, 0f), 0.11f, coreMat);
            var pulse = core.AddComponent<Pulse>();
            pulse.amplitude = 0.1f;

            var top = Ball("BeaconTop", root.transform, new Vector3(0f, 3.10f, 0f), 0.115f, coreMat);
            var topPulse = top.AddComponent<Pulse>();
            topPulse.amplitude = 0.18f;

            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Lin(accent);
            light.range = 7f;
            light.intensity = 1.9f;

            CreateWorldLabel(root.transform, title, new Vector3(0f, 2.30f, 0f), Cream, 2.2f);

            // 点位序号：①②③ 立在标题上方，站远了也能看出「这是第几处、还差几处」
            if (!string.IsNullOrEmpty(number))
                CreateWorldLabel(root.transform, number, new Vector3(0f, 2.92f, 0f), accent, 2.6f);

            // 史实分层（GAME_DESIGN §9.1）：推断加「○」、艺术处理加「△」，已证实不加记号。
            // 一行小字挂在标题下面，站着就能看到这块牌子说的是史实还是推测。
            // 字号给到 1.10 宽、色值贴着纸白：给 0.92 宽 + 土黄色时，五米外就糊进背景看不见了。
            if (tier != SourceTier.Confirmed)
            {
                string mark = tier == SourceTier.Inferred ? "○ 学界推断" : "△ 艺术处理";
                Color markColor = tier == SourceTier.Inferred
                    ? new Color(0.92f, 0.90f, 0.84f)
                    : new Color(0.96f, 0.88f, 0.78f);
                CreateWorldLabel(root.transform, mark, new Vector3(0f, 1.92f, 0f), markColor, 1.40f);
            }

            var interactable = root.AddComponent<Interactable>();
            interactable.kind = InteractKind.Info;
            interactable.prompt = "[E] 查看线索";
            interactable.title = title;
            interactable.subtitle = subtitle;
            interactable.body = body;
            interactable.objectiveId = objectiveId;
            interactable.tier = tier;
            interactable.tierNote = tierNote;
            interactable.markerRenderer = core.GetComponent<Renderer>();
            interactable.markerColor = accent;
            interactable.heritageId = heritageId;
            interactable.leadTo = leadTo;
            interactable.endScene = endScene;
            interactable.endSpawnId = endSpawnId;
            interactable.endLine = endLine;
            if (!string.IsNullOrEmpty(heritageId)) interactable.prompt = "[E] 看这一处";
            if (!string.IsNullOrEmpty(endScene)) interactable.prompt = "[E] 合上这一卷";

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            trigger.center = new Vector3(0f, 1.1f, 0f);

            return root;
        }

        /// <summary>
        /// 遗址入口的界石：一块立石、一块地名牌，走近浮一行字，按 E 就回序章那条岔路的尽头。
        /// 有它，才能「进去看一眼就出来」——不用把这一卷走完才能走人。
        /// 石头立在来路边上，不挡进场视线；感应圈半径小，站在遗址里不会被它烦到。
        /// </summary>
        public static GameObject CreateReturnStone(Transform parent, string name, Vector3 pos, float yaw,
                                                   string placeName, string chapterId,
                                                   Material stone, Material wood)
        {
            var root = Empty("ReturnStone_" + name, parent, pos);
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Base", root.transform, new Vector3(0f, 0.12f, 0f),
                new Vector3(1.80f, 0.24f, 1.25f), stone, true);
            Box("Slab", root.transform, new Vector3(0f, 0.94f, 0f),
                new Vector3(1.10f, 1.50f, 0.34f), stone, true);
            Box("Cap", root.transform, new Vector3(0f, 1.74f, 0f),
                new Vector3(0.90f, 0.12f, 0.38f), wood, false);

            // 石面朝来路：字是给刚进来的人看的，回程不必绕着找。
            // 浅色字压在深石上——深字在阴天里会糊成一块，预览也读不出来。
            CreateTextPlate(root.transform,
                "界 石\n\n" + placeName + "\n\n顺来路，回山谷",
                new Vector3(0f, 0.96f, 0.19f), Vector3.zero,
                new Vector2(0.92f, 1.16f), 30, new Color(0.93f, 0.89f, 0.78f), title: true);

            var portal = root.AddComponent<ScenePortal>();
            portal.sceneToLoad = "Archive";
            portal.spawnId = "back_" + chapterId;
            portal.line = "[E] 回山谷　—— 顺着来路走回去";
            portal.lockedLine = portal.line;
            portal.destinationLabel = "序章 · 暮色古道";
            portal.radius = 2.8f;

            return root;
        }

        public static GameObject CreateWorldLabel(Transform parent, string text, Vector3 localPos, Color color, float width)
        {
            const float Units = 1000f;   // 同 CreateTextPlate：内部像素翻倍，字更实
            var go = new GameObject("Label_" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * (width / Units);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 3;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Units, 220f);

            var t = TextStretch(go.transform, "Text", text, 80, color, TextAnchor.MiddleCenter,
                new Vector2(12f, 8f), new Vector2(-12f, -8f));
            t.horizontalOverflow = HorizontalWrapMode.Wrap;

            go.AddComponent<Billboard>();
            return go;
        }

        // ---------------------------------------------------------------- 环境

        public static void SetupArchiveEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(Color.Lerp(HuaxiaPalette.DuskZenith, HuaxiaPalette.BlueGrey, 0.35f));
            RenderSettings.ambientEquatorColor = Lin(Color.Lerp(HuaxiaPalette.DuskAmbient, HuaxiaPalette.Sand, 0.30f));
            RenderSettings.ambientGroundColor = Lin(Color.Lerp(HuaxiaPalette.Rock, HuaxiaPalette.Sand, 0.70f));
            RenderSettings.ambientIntensity = 1.02f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // 远山要化进空气里，否则 100 m 外的山和脚边的石头一个饱和度，画面就被摊平
            RenderSettings.fogDensity = 0.0074f;
            RenderSettings.fogColor = Lin(Color.Lerp(HuaxiaPalette.DuskFog, HuaxiaPalette.DuskHorizon, 0.30f));
            RenderSettings.skybox = SkyMat("Sky_Archive",
                HuaxiaPalette.DuskZenith, HuaxiaPalette.DuskHorizon, 0.95f, 0.95f, 0.82f);
            DynamicGI.UpdateEnvironment();

            QualitySettings.shadowDistance = 90f;
        }

        public static Light SetupOutdoorEnvironment()
        {
            var sunGO = new GameObject("Sun", typeof(Light));
            sunGO.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Lin(new Color(1f, 0.96f, 0.90f));
            sun.intensity = 1.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.04f;

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(new Color(0.55f, 0.66f, 0.78f));
            RenderSettings.ambientEquatorColor = Lin(new Color(0.56f, 0.52f, 0.42f));
            RenderSettings.ambientGroundColor = Lin(new Color(0.30f, 0.28f, 0.22f));
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0045f;
            RenderSettings.fogColor = Lin(new Color(0.78f, 0.78f, 0.74f));
            RenderSettings.skybox = SkyMat("Sky_Liangzhu",
                new Color(0.58f, 0.66f, 0.78f), new Color(0.45f, 0.40f, 0.32f), 0.9f, 1.45f, 0.22f);
            DynamicGI.UpdateEnvironment();
            QualitySettings.shadowDistance = 110f;
            return sun;
        }

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDir + "/Archive.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Liangzhu.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Qinling.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Quanzhou.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Epilogue.unity", true),
            };
        }

        /// <summary>
        /// 环境音乐是长循环，走流式加载：内存只占一小段，且必须保持未压缩 PCM —— 
        /// Vorbis 编码会引入首尾填充，循环点会"咔"一下。
        /// </summary>
        [MenuItem("Huaxia/刷新音频导入设置")]
        public static void ReimportAudioClips()
        {
            ConfigureAudioClips();
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureAudioClips()
        {
            const string dir = "Assets/Huaxia/Resources/Audio";
            if (!AssetDatabase.IsValidFolder(dir)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;
                var st = imp.defaultSampleSettings;
                st.loadType = AudioClipLoadType.Streaming;
                st.compressionFormat = AudioCompressionFormat.PCM;
                st.quality = 1f;
                st.preloadAudioData = false;
                imp.defaultSampleSettings = st;
                imp.loadInBackground = true;
                imp.forceToMono = true;
                imp.SaveAndReimport();
            }
        }

        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Huaxia Civilization Lab";
            PlayerSettings.productName = "华夏文明探源";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        }

        // ---------------------------------------------------------------- 预览渲染

        [MenuItem("Huaxia/仅渲染：序章预览图")]
        public static void RenderArchivePreviews()
        {
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(0f, 1.70f, -28.6f),
                new Vector3(0f, 2.20f, 2f), "Previews/archive.png", 60f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(0.2f, 1.70f, -7.0f),
                new Vector3(0f, 2.10f, 0.6f), "Previews/archive_fork.png", 55f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(0.4f, 1.70f, -24.2f),
                new Vector3(-5.50f, 1.70f, -20.6f), "Previews/archive_hub.png", 46f);
            // 问道亭：广场西缘的布景，从广场中间看过去
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(1.2f, 1.70f, 1.6f),
                new Vector3(-10.7f, 1.60f, -1.0f), "Previews/archive_pavilion.png", 55f);
            // 进谷方向：亭子在左手边，三条路的望柱仍在正前方
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(0f, 1.70f, -14.0f),
                new Vector3(-3.0f, 1.90f, 8.0f), "Previews/archive_pavilion_flag.png", 60f);
            // 踏跺落在广场石墁上：还断不断，看这一张
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-4.0f, 1.70f, -1.2f),
                new Vector3(-10.7f, 1.30f, -1.0f), "Previews/archive_pavilion_front.png", 58f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(4.4f, 1.72f, -3.2f),
                new Vector3(13.5f, 0.60f, -8.5f), "Previews/archive_pond.png", 62f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(5.6f, 7.4f, 10.2f),
                new Vector3(0f, 6.3f, 46.5f), "Previews/archive_hall.png", 52f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-34f, 16.5f, -34f),
                new Vector3(0f, 4.0f, 12f), "Previews/archive_wide.png", 58f);
            // 三条路尽头的界门：走到跟前会浮出「按 E 过去」
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-10.07f, 1.70f, 9.73f),
                new Vector3(-15.47f, 2.20f, 14.94f), "Previews/archive_end_liangzhu.png", 55f);
            // 秦陵界门：从甬道西侧的支路口斜看过去，门朝南，牌面要读到
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-1.20f, 1.72f, 18.20f),
                new Vector3(-6.60f, 2.55f, 23.50f), "Previews/archive_end_qinling.png", 55f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(10.07f, 1.70f, 9.73f),
                new Vector3(15.47f, 2.20f, 14.94f), "Previews/archive_end_quanzhou.png", 55f);
            // 藏卷阁门前：三卷不齐，门不开
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(0f, 1.90f, 30.2f),
                new Vector3(0f, 4.30f, 43.0f), "Previews/archive_hallgate.png", 62f);
            // 西配殿（抄书处）：门是关的，屋里没有树
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-12.4f, 1.75f, 32.6f),
                new Vector3(-17.9f, 2.60f, 40.4f), "Previews/archive_sidehall.png", 54f);
            // 弘文：站在《河山册》牌前，相机从她正前方看过去
            RenderHongwenPreviews();
            // 立牌前的引导圈：抬到腰高往下看，圈要贴地、要在牌面正前方
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-5.95f, 1.50f, 1.36f),
                new Vector3(-7.60f, 0.35f, 3.80f), "Previews/archive_guide_ring.png", 55f);
            // 三块遗产立牌：各从正前方 2.9 m 看，验牌面读不读得出来
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-7.28f, 1.55f, 3.00f),
                new Vector3(-9.11f, 1.45f, 5.25f), "Previews/archive_board_liangzhu.png", 50f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-2.10f, 1.55f, 7.33f),
                new Vector3(-2.55f, 1.45f, 10.20f), "Previews/archive_board_qinling.png", 50f);
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(7.28f, 1.55f, 3.00f),
                new Vector3(9.11f, 1.45f, 5.25f), "Previews/archive_board_quanzhou.png", 50f);
        }

        // 弘文站在《河山册》总说明牌前那一格（见 ArchiveScene 里的 hongwenPos）。
        // 相机机位写死就得跟着她挪，所以单独拎出来：她换位置只改这两行。
        [MenuItem("Huaxia/仅渲染：弘文预览图")]
        public static void RenderHongwenPreviews()
        {
            // 全身：从她右前方看，人和牌、和地上的圈能一起进画面
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-0.90f, 1.80f, -26.60f),
                new Vector3(-3.30f, 1.15f, -22.30f), "Previews/archive_hongwen.png", 52f);
            // 特写：站在她正前方 1.7 m，验五官、贴图有没有脏东西
            RenderPreview(SceneDir + "/Archive.unity", new Vector3(-2.59f, 1.55f, -23.85f),
                new Vector3(-3.30f, 1.50f, -22.30f), "Previews/archive_hongwen_face.png", 30f);
        }

        [MenuItem("Huaxia/渲染场景预览图")]
        public static void RenderPreviews()
        {
            RenderArchivePreviews();
            RenderLiangzhuPreviews();

            RenderQinlingPreviews();

            // -------------------------------------------------------------- 第三章 · 泉州
            // 视点比原来抬 0.2 m、目光多抬 0.5 m：既不贴地让石埕吃掉下半张，
            // 也不至于把半个天空推上来。石埕本身已经压暗，可以稳稳托住画面下缘。
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(5.4f, 3.15f, -54f),
                new Vector3(-4.5f, 4.35f, -14f), "Previews/quanzhou.png", 62f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(5.4f, QzQuayY + 1.72f, -56f),
                new Vector3(9.2f, QzQuayY + 0.95f, -54.8f), "Previews/quanzhou_returnstone.png", 50f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(-4.0f, 3.50f, 18.8f),
                new Vector3(-24f, 1.70f, 16f), "Previews/quanzhou_pier.png", 58f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(12.0f, 6.40f, -48f),
                new Vector3(35f, 1.60f, -33f), "Previews/quanzhou_dock.png", 54f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(13.0f, 2.60f, -3.0f),
                new Vector3(13f, 1.90f, -20f), "Previews/quanzhou_cargo.png", 60f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(20.0f, 3.30f, 1.4f),
                new Vector3(20f, 2.70f, 8f), "Previews/quanzhou_wall.png", 58f);
            RenderPreview(SceneDir + "/Quanzhou.unity",
                new Vector3(52.4f, QuanzhouHeight(52.4f, 13.0f) + 3.10f, 13.0f),
                new Vector3(52f, QuanzhouHeight(52f, 34f) + 1.70f, 34f), "Previews/quanzhou_stele.png", 52f);
            // 洛阳桥桥头：站在立牌边往西看，桥墩与石梁要一路伸进海里
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(6.6f, QzQuayY + 1.74f, -38.6f),
                new Vector3(-30f, 1.20f, -44.8f), "Previews/quanzhou_bridge.png", 58f);
            // 开元寺双塔：从塔前的地面往上看五层收分
            RenderPreview(SceneDir + "/Quanzhou.unity",
                new Vector3(78f, QuanzhouHeight(78f, -9f) + 1.74f, -9f),
                new Vector3(78f, QuanzhouHeight(78f, 21f) + 8.60f, 21f), "Previews/quanzhou_pagoda.png", 58f);
            RenderPreview(SceneDir + "/Quanzhou.unity", new Vector3(76f, 46f, -104f),
                new Vector3(6f, 5f, 8f), "Previews/quanzhou_wide.png", 54f, 0.0026f);

            RenderEpiloguePreviews();
        }

        [MenuItem("Huaxia/仅渲染：秦陵预览图")]
        [MenuItem("Huaxia/渲染良渚预览图")]
        public static void RenderLiangzhuPreviews()
        {
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(-0.8f, 1.80f, -47.6f),
                new Vector3(3.0f, 1.25f, -14f), "Previews/liangzhu.png", 60f);
            // 入口的界石：从遗址里回头，能看见「顺来路，回山谷」
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(0.6f, 1.72f, -56.2f),
                new Vector3(-1.5f, LiangzhuHeight(-1.5f, -52.5f) + 0.95f, -52.5f),
                "Previews/liangzhu_returnstone.png", 50f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(-13.5f, 3.05f, -25.5f),
                new Vector3(-21.0f, 2.20f, -3.5f), "Previews/liangzhu_settlement.png", 54f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(15.5f, 5.20f, 15.0f),
                new Vector3(-2.0f, 5.60f, 36f), "Previews/liangzhu_mojiao.png", 56f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(2f, 13.5f, -84f),
                new Vector3(0f, 6.5f, 22f), "Previews/liangzhu_city.png", 50f, 0.0034f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(11.00f, 2.40f, -1.60f),
                new Vector3(12.90f, 2.30f, 6.10f), "Previews/liangzhu_wall.png", 54f);
            // 第一站：刚过陆城门，回身看——光柱在左手边，土垄城墙与西水门都在这一眼里
            RenderPreview(SceneDir + "/Liangzhu.unity",
                new Vector3(1.20f, LiangzhuHeight(1.2f, 13.0f) + 1.72f, 13.00f),
                new Vector3(-2.00f, LiangzhuHeight(-2.0f, 8.4f) + 1.10f, 8.40f),
                "Previews/liangzhu_gate.png", 56f);
            // 第三站：走完台地往西，第三点与反山应在同一眼里
            RenderPreview(SceneDir + "/Liangzhu.unity",
                new Vector3(2.00f, LiMojiaoTop + 1.80f, 24.00f),
                new Vector3(-14.00f, LiMojiaoTop + 0.95f, 25.50f), "Previews/liangzhu_jade.png", 56f);
            // 台地俯瞰：看三点位之间的树冠有没有挡住光柱
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(11.5f, 13.5f, 14.0f),
                new Vector3(-12.0f, LiMojiaoTop, 27.0f), "Previews/liangzhu_platform.png", 54f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(36.0f, 6.00f, 54.0f),
                new Vector3(57.7f, 2.30f, 39.1f), "Previews/liangzhu_altar.png", 56f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(-7.10f, 2.45f, 15.6f),
                new Vector3(-18.6f, 1.55f, 12.6f), "Previews/liangzhu_dock.png", 53f);
            RenderPreview(SceneDir + "/Liangzhu.unity", new Vector3(-14.0f, 6.20f, -33.0f),
                new Vector3(-46f, 2.60f, -13.0f), "Previews/liangzhu_hamlet.png", 50f);
        }

        public static void RenderQinlingPreviews()
        {
            // -------------------------------------------------------------- 第二章 · 秦陵
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(0.6f, QinlingHeight(0.6f, -96f) + 2.35f, -96f),
                new Vector3(0f, QinlingHeight(0f, -70f) + 6.40f, -70f), "Previews/qinling.png", 52f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(0f, QinlingHeight(0f, -78f) + 1.72f, -78f),
                new Vector3(4.0f, QinlingHeight(4.0f, -76.6f) + 0.95f, -76.6f),
                "Previews/qinling_returnstone.png", 50f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(-31.3f, QinlingHeight(-34.5f, -42f) + 2.60f, -54.3f),
                new Vector3(-35.7f, QinlingHeight(-34.5f, -42f) + 2.00f, -46.0f), "Previews/qinling_wall.png", 56f);
            // 一号坑：坑外俯看坑口与门道 + 从门道上口往下走 + 坑内走廊平视军阵
            float qpFloor = QinlingHeight(QnPitX, QnPitZ);
            float qpCz = QnPitZ + QpCorridorZ(3);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(QnPitX + 52f, QinlingHeight(QnPitX + 52f, QnPitZ - 14f) + 11.0f, QnPitZ - 14f),
                new Vector3(QnPitX - 8f, qpFloor + 2.4f, QnPitZ + 2f), "Previews/qinling_pit.png", 52f);
            // 站在东门道上口，看坡道怎么下到坑底——这是玩家进坑的入口
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(QnPitX + 41.0f, QinlingHeight(QnPitX + 41.0f, qpCz) + 1.68f, qpCz),
                new Vector3(QnPitX + 20.0f, qpFloor + 1.10f, qpCz), "Previews/qinling_pit_ramp.png", 60f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(QnPitX + 24f, qpFloor + 1.62f, qpCz + 1.06f),
                new Vector3(QnPitX + 16f, qpFloor + 0.90f, qpCz + 1.06f), "Previews/qinling_fig.png", 34f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(QnPitX + 21f, qpFloor + 1.74f, qpCz),
                new Vector3(QnPitX - 30f, qpFloor + 1.52f, qpCz), "Previews/qinling_pit_inside.png", 62f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(60.0f, QinlingHeight(60.0f, -13f) + 4.30f, -13f),
                new Vector3(60.0f, QinlingHeight(60.0f, 6f) + 0.85f, 6f), "Previews/qinling_bronze.png", 54f);
            // 封土俯视三分之四：站在陵园外沿抬高 13 m 看过去，
            // 整个覆斗的收分轮廓、坡面草和周围平地能进同一张图。
            // 原来贴到 34 m 处平视，封土直接顶满画幅，看不出它是"从地里长出来的"。
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(0.5f, QinlingHeight(0.5f, -62f) + 13.0f, -62f),
                new Vector3(0f, 19.0f, QnMoundZ), "Previews/qinling_mound.png", 48f);
            RenderPreview(SceneDir + "/Qinling.unity",
                new Vector3(-65.5f, QinlingHeight(-65.5f, -42.5f) + 2.15f, -42.5f),
                new Vector3(-57.2f, QinlingHeight(-58f, -31.5f) + 1.05f, -31.5f), "Previews/qinling_workshop.png", 58f);
            // 从北侧高处看：封土压在山前，南面的骊山才是背景。原来的机位在山南，
            // 镜头正对北边平原，山全在背后，难怪看不出是秦岭。
            RenderPreview(SceneDir + "/Qinling.unity", new Vector3(150f, 58f, 235f),
                new Vector3(0f, 10f, 26f), "Previews/qinling_wide.png", 52f, 0.0018f);
            // 封土顶往南看：一道路堑、一道山墙，秦岭的存在感就靠这一张
            RenderPreview(SceneDir + "/Qinling.unity", new Vector3(0f, 27f, 34f),
                new Vector3(0f, 96f, -270f), "Previews/qinling_lishan.png", 58f, 0.0018f);
        }

        [MenuItem("Huaxia/仅渲染：终章预览图")]
        public static void RenderEpiloguePreviews()
        {
            RenderPreview(SceneDir + "/Epilogue.unity", new Vector3(0f, 1.62f, -3.70f),
                new Vector3(0f, 1.92f, 4.40f), "Previews/epilogue_hall.png", 62f);
            RenderPreview(SceneDir + "/Epilogue.unity", new Vector3(0f, 1.44f, -0.10f),
                new Vector3(0f, 1.58f, 4.40f), "Previews/epilogue.png", 60f);
            RenderPreview(SceneDir + "/Epilogue.unity", new Vector3(0.30f, 1.14f, 1.34f),
                new Vector3(-0.01f, 0.96f, 2.06f), "Previews/epilogue_book.png", 38f);
            RenderPreview(SceneDir + "/Epilogue.unity", new Vector3(0f, 1.60f, 0.70f),
                new Vector3(0f, 1.48f, -4.40f), "Previews/epilogue_door.png", 60f);
        }


        // 预览图默认按 2 倍超采样渲染：1600×900 的框里塞 3200×1800 个像素，
        // 再用方框滤波缩回去。等价于 4×SSAA，边缘和织物/石面的高频不会糊成一片。
        const int PreviewW = 1600, PreviewH = 900;
        const int PreviewSSAA = 2;

        static void RenderPreview(string scenePath, Vector3 pos, Vector3 lookAt, string outPath, float fov,
                                  float fogDensity = -1f, System.Action afterOpen = null)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (fogDensity >= 0f) RenderSettings.fogDensity = fogDensity;
            // 有些东西只有运行时才动（比如藏卷阁的门轴），预览时先手动摆好再拍
            if (afterOpen != null) afterOpen();
            var go = new GameObject("PreviewCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt);

            // 编辑态下 Billboard 的 LateUpdate 不执行，手动让世界标签正对预览相机，避免镜像
            foreach (var bb in Object.FindObjectsOfType<Billboard>())
            {
                Vector3 toCam = bb.transform.position - pos;
                if (bb.lockVertical) toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0004f)
                    bb.transform.rotation = Quaternion.LookRotation(toCam.normalized);
            }

            var src = Camera.main;
            if (src != null)
            {
                cam.clearFlags = src.clearFlags;
                cam.backgroundColor = src.backgroundColor;
            }
            else
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }

            int w = PreviewW * PreviewSSAA, h = PreviewH * PreviewSSAA;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;          // 超采样之外再叠 4×MSAA，几何边缘才干净
            cam.allowMSAA = true;
            cam.targetTexture = rt;
            cam.Render();

            // 场景里的调色参数也复制到离屏相机：预览图与实机不能是两套颜色
            RenderTexture graded = null;
            var grade = Object.FindObjectOfType<CinematicGrade>();
            if (grade != null)
            {
                var gs = Shader.Find("Hidden/Huaxia/Grade");
                if (gs != null && gs.isSupported)
                {
                    var gm = new Material(gs);
                    grade.ApplyTo(gm);
                    graded = new RenderTexture(w, h, 0, rt.format);
                    Graphics.Blit(rt, graded, gm);
                    Object.DestroyImmediate(gm);
                }
            }

            RenderTexture.active = graded != null ? graded : rt;
            var big = new Texture2D(w, h, TextureFormat.RGB24, false);
            big.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            big.Apply();
            RenderTexture.active = null;

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Previews");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string name = Path.GetFileName(outPath);

            // 3200×1800 原图单独放 hires（不进仓库），供人按 1:1 看细节
            string hiresDir = Path.Combine(dir, "hires");
            if (!Directory.Exists(hiresDir)) Directory.CreateDirectory(hiresDir);
            File.WriteAllBytes(Path.Combine(hiresDir, name), big.EncodeToPNG());

            var tex = Downsample(big, PreviewSSAA);
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
            Debug.Log("[Huaxia] 预览图: " + Path.Combine(dir, name) + "（超采样 " + PreviewSSAA + "×）");

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            if (graded != null) Object.DestroyImmediate(graded);
            Object.DestroyImmediate(big);
            if (tex != big) Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        /// <summary>方框滤波降采样。预览图缩小后仍保留高频，不靠 GPU 双线性那 2×2 采样。</summary>
        static Texture2D Downsample(Texture2D src, int factor)
        {
            if (factor <= 1) return src;

            int w = src.width / factor, h = src.height / factor;
            var sp = src.GetPixels32();
            var dp = new Color32[w * h];
            int n = factor * factor;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int r = 0, g = 0, b = 0;
                    for (int j = 0; j < factor; j++)
                    {
                        int row = (y * factor + j) * src.width + x * factor;
                        for (int i = 0; i < factor; i++)
                        {
                            var c = sp[row + i];
                            r += c.r; g += c.g; b += c.b;
                        }
                    }
                    dp[y * w + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                }
            }

            var t = new Texture2D(w, h, TextureFormat.RGB24, false);
            t.SetPixels32(dp);
            t.Apply();
            return t;
        }
    }
}
