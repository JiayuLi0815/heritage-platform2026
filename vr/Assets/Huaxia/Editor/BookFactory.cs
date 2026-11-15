using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 《河山册》的预制体工厂。
    /// 墨迹是烘培出来的贴图（楷书 → RenderTexture → 带 alpha 的 PNG），
    /// 这样运行时只有三个 float 在动：湿度、显墨、页边注。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        public const string BookResDir = "Assets/Huaxia/Resources";
        public const string BookPrefabPath = BookResDir + "/HandBook.prefab";
        const string BookTexDir = "Assets/Huaxia/Textures";
        const string BookMeshDir = "Assets/Huaxia/Meshes";
        const string BookPageShaderName = "Huaxia/BookPage";

        // 页面尺寸（米）：单页 0.19 × 0.265，摊开 0.38，抬到眼前约 0.35 米可读
        const float PageW = 0.19f;
        const float PageH = 0.265f;
        const float BoardT = 0.0038f;
        const float LeafT = 0.0018f;
        const int LeafCount = 4;

        // 烘培分辨率：1024 宽对应 0.19 米，约 5400 px/m，楷书小字也够清晰
        // 朱印在页面上的位置（uv；v 从页底算起）
        const float SealU = 0.62f;
        const float SealV = 0.34f;
        const int BakeW = 1024;
        const int BakeH = 1440;

        struct BookPage
        {
            public string volume;
            public string place;
            public string line;
            public string hand;
            public string note1;
            public string note2;

            public BookPage(string volume, string place, string line, string hand, string n1, string n2)
            {
                this.volume = volume;
                this.place = place;
                this.line = line;
                this.hand = hand;
                note1 = n1;
                note2 = n2;
            }
        }

        static readonly BookPage[] BookPages =
        {
            new BookPage("卷一", "良渚", "文明始于共同生活", "水要分。",
                "一进水口，三田分水。", "同一个纹，隔着很远的墓。"),
            new BookPage("卷二", "秦陵", "文明由无数人的共同建设而成", "土要夯。",
                "一层土，一层杵。", "他们本来有颜色。"),
            new BookPage("卷三", "泉州", "文明在交流中不断延续", "风要等。",
                "同一片山岩，三种字。", "船总从同一个方向来。")
        };

        // ---------------------------------------------------------------- 入口

        [MenuItem("Huaxia/仅构建：河山册预制体")]
        public static void BuildBookMenu()
        {
            BuildBookPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>烘培墨迹 + 建材质 + 存预制体。BuildAll 最先调它。</summary>
        public static void BuildBookPrefab()
        {
            LoadFonts();
            if (bookPage == null)
            {
                Debug.LogError("[Huaxia] 楷书字体缺失，河山册的墨迹无法烘培");
                return;
            }

            BakeStainTexture();
            BakePageTextures();
            BuildBookMaterials();

            var root = new GameObject("HandBook");
            var book = root.AddComponent<HandBook>();
            var t = root.transform;

            // 封底：一整块板，摊开时左右两页共用
            Box("Board", t, new Vector3(0f, BoardT * 0.5f, 0f),
                new Vector3(PageW * 2f + 0.008f, BoardT, PageH + 0.010f), BookMat("Book_Cover"), false);

            // 书脊：一层薄布，压在两页中间
            Box("Spine", t, new Vector3(0f, BoardT + LeafT * 2.4f, 0f),
                new Vector3(0.014f, LeafT * 4.8f, PageH + 0.002f), BookMat("Book_Spine"), false);

            // 左页：《河山册》书名页，永远不动
            var titlePage = MakePage("TitlePage", t, new Vector3(-PageW, BoardT, 0f),
                BookMat("Book_Page_Title"));

            // 右页：四叶，卷一在最上面（先翻走的也是它）
            var leaves = new BookLeaf[LeafCount];
            for (int i = 0; i < LeafCount; i++)
            {
                float y = BoardT + LeafT * (LeafCount - i);
                var pivot = Empty("LeafPivot_" + i, t, new Vector3(0f, y, 0f));
                var face = MakePage("Sheet_" + i, pivot.transform,
                    new Vector3(0f, 0f, 0f), BookMat("Book_Page_" + i), true);

                var leaf = pivot.gameObject.AddComponent<BookLeaf>();
                leaf.face = face.GetComponent<MeshRenderer>();
                var back = pivot.transform.Find("Sheet_" + i + "/Sheet_" + i + "_Back");
                if (back != null) leaf.back = back.GetComponent<Renderer>();
                leaf.pivot = pivot.transform;
                leaf.chapterId = i < BookPages.Length ? GameState.Chapters[i] : null;
                leaf.wetness = 1f;
                leaf.ink = 1f;
                leaf.note = 0f;
                leaf.foot = 0f;
                leaf.unlocked = i == 0;
                leaves[i] = leaf;
            }

            book.leaves = leaves;
            book.cover = null;
            book.current = 0;
            book.coverOpen = true;

            if (!AssetDatabase.IsValidFolder(BookResDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
                AssetDatabase.CreateFolder("Assets/Huaxia", "Resources");
            }

            PersistBookMeshes(root);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, BookPrefabPath);
            Object.DestroyImmediate(root);
            if (saved != null) Debug.Log("[Huaxia] 河山册预制体: " + BookPrefabPath);
        }


        /// <summary>
        /// 预制体不能像场景那样把内存里的网格随文件一起存下去（会存成空引用），
        /// 所以先把每个 MeshFilter 的网格另存成资产。
        /// </summary>
        static void PersistBookMeshes(GameObject root)
        {
            EnsureFolder(BookMeshDir);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                string path = BookMeshDir + "/Book_" + mf.gameObject.name.Replace(" ", "") + ".asset";
                // 同名资产可能来自旧版本（几何改过），必须覆盖，否则会挂着旧网格
                var copy = Object.Instantiate(mesh);
                copy.name = "Book_" + mf.gameObject.name;
                AssetDatabase.CreateAsset(copy, path);
                mf.sharedMesh = copy;
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>一叶纸：薄的页块 + 正面一片带墨的纸面。局部原点在书脊一侧。</summary>
        static GameObject MakePage(string name, Transform parent, Vector3 localPos, Material faceMat, bool withBack = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            // MeshFactory 的 BoxUV 是单位立方体，尺寸靠 localScale，所以这里先用临时父级归一
            Box(name + "_Edge", root.transform, new Vector3(PageW * 0.5f, LeafT * 0.5f, 0f),
                new Vector3(PageW, LeafT, PageH), BookMat("Book_PageEdge"), false);

            var face = new GameObject(name + "_Face", typeof(MeshFilter), typeof(MeshRenderer));
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, LeafT + 0.0003f, 0f);
            face.GetComponent<MeshFilter>().sharedMesh = PageQuadMesh();
            var r = face.GetComponent<MeshRenderer>();
            r.sharedMaterial = faceMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;

            // 叶的背面：翻过去看见的是这一叶的背，不是发亮的空白板
            if (withBack)
            {
                var back = new GameObject(name + "_Back", typeof(MeshFilter), typeof(MeshRenderer));
                back.transform.SetParent(root.transform, false);
                back.transform.localPosition = new Vector3(0f, -0.0003f, 0f);
                back.GetComponent<MeshFilter>().sharedMesh = PageBackQuadMesh();
                var br = back.GetComponent<MeshRenderer>();
                br.sharedMaterial = BookMat("Book_Page_Verso");
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // 叶背一旦接收阴影，就会被自己 0.6mm 之上的那一面全糊住
                br.receiveShadows = false;
            }

            return face;
        }

        static Mesh pageQuad;
        static Mesh pageBackQuad;


        /// <summary>叶背：占同一块纸面（x 0..PageW），双面各一套顶点，翻过去不管朝向都读得正。</summary>
        static Mesh PageBackQuadMesh()
        {
            if (pageBackQuad != null) return pageBackQuad;
            float hh = PageH * 0.5f;
            var m = new Mesh { name = "HxBookPageBack" };
            var p = new[]
            {
                new Vector3(PageW, 0f, -hh), new Vector3(PageW, 0f, hh),
                new Vector3(0f, 0f, hh), new Vector3(0f, 0f, -hh)
            };
            var uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.vertices = new[] { p[0], p[1], p[2], p[3], p[0], p[1], p[2], p[3] };
            m.uv = new[] { uv[0], uv[1], uv[2], uv[3], uv[0], uv[1], uv[2], uv[3] };
            m.normals = new[]
            {
                // 叶背两面都以「朝下」为法线：翻过去之后 180° 旋转会把它转成朝上，
                // 这才是翻过去那一叶真正的受光面
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7 };
            m.RecalculateBounds();
            pageBackQuad = m;
            return m;
        }


        static Mesh PageQuadMesh()
        {
            if (pageQuad != null) return pageQuad;
            float hh = PageH * 0.5f;
            var m = new Mesh { name = "HxBookPage" };
            m.vertices = new[]
            {
                new Vector3(0f, 0f, -hh), new Vector3(0f, 0f, hh),
                new Vector3(PageW, 0f, hh), new Vector3(PageW, 0f, -hh)
            };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            pageQuad = m;
            return m;
        }

        // ---------------------------------------------------------------- 材质

        static Material BookMat(string name)
        {
            Material cached;
            if (matCache.TryGetValue(name, out cached) && cached != null) return cached;

            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);

            bool isCover = name == "Book_Cover" || name == "Book_Spine" ||
                           name == "Book_PageEdge";

            if (isCover)
            {
                if (m == null) { m = new Material(StylizedShader); AssetDatabase.CreateAsset(m, path); }
                m.shader = StylizedShader;
                string kind = (name == "Book_Cover" || name == "Book_Spine") ? "plaster" : "paper";
                m.SetTexture("_MainTex", StylizedTextureFactory.Get(kind));
                m.SetFloat("_DetailScale", 7f);
                m.SetFloat("_DetailStrength", 0.55f);
                m.SetTexture("_BumpMap", StylizedTextureFactory.GetNormal(kind));
                m.SetFloat("_BumpScale", 0.45f);
                m.SetFloat("_MacroScale", 2.4f);
                m.SetFloat("_MacroStrength", 0.4f);
                Color baseCol = name == "Book_Cover" ? HuaxiaPalette.InkTealDeep
                    : name == "Book_Spine" ? Color.Lerp(HuaxiaPalette.InkTealDeep, HuaxiaPalette.PaperWarm, 0.22f)
                    : Color.Lerp(HuaxiaPalette.PaperWarm, Cream, 0.55f);
                m.SetColor("_Color", Lin(baseCol));
                m.SetColor("_ShadowTint", Lin(Color.Lerp(HuaxiaPalette.DuskAmbient, HuaxiaPalette.PaperWarm, 0.34f)));
                m.SetFloat("_Wrap", 0.72f);
                m.SetFloat("_RampSoftness", 0.55f);
                m.SetFloat("_RimStrength", 0.06f);
                m.SetFloat("_Glossiness", 0.12f);
                m.SetFloat("_Metallic", 0f);
            }
            else
            {
                var shader = Shader.Find(BookPageShaderName);
                if (shader == null)
                {
                    Debug.LogError("[Huaxia] 找不到 Huaxia/BookPage，纸面退回风格化材质");
                    shader = StylizedShader;
                }
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;

                int idx = -1;
                for (int i = 0; i < LeafCount; i++)
                    if (name == "Book_Page_" + i) idx = i;

                string suffix = idx >= 0 ? idx.ToString() : "Title";
                m.SetTexture("_InkTex", LoadBookTex("Book_Ink_" + suffix));
                m.SetTexture("_NoteTex", LoadBookTex("Book_Note_" + suffix));
                m.SetTexture("_FootTex", LoadBookTex("Book_Foot_" + suffix));
                m.SetTexture("_MainTex", StylizedTextureFactory.Get("paper"));
                m.SetTexture("_StainTex", LoadBookTex("Book_Stain"));
                // 朱印只落在书名页：同一张遮罩，其余页面把强度设成 0
                m.SetTexture("_SealMask", LoadBookTex("Book_Seal"));
                m.SetFloat("_SealStrength", suffix == "Title" ? 1f : 0f);
                m.SetColor("_SealColor", Lin(new Color(0.74f, 0.24f, 0.19f)));
                m.SetColor("_Color", Lin(new Color(0.900f, 0.880f, 0.818f)));
                m.SetColor("_WetColor", Lin(new Color(0.740f, 0.725f, 0.668f)));
                m.SetColor("_InkColor", Lin(new Color(0.082f, 0.080f, 0.086f)));
                m.SetColor("_InkFade", Lin(new Color(0.305f, 0.290f, 0.275f)));
                m.SetColor("_StainTint", Lin(new Color(0.575f, 0.560f, 0.480f)));
                m.SetFloat("_FiberScale", 26f);
                m.SetFloat("_FiberStrength", 0.09f);
                m.SetFloat("_StainStrength", 0.9f);
                m.SetFloat("_GutterDark", 0.18f);
                m.SetFloat("_EdgeDark", 0.10f);
                // 书名页是常驻的实物：纸已经干透，只留很淡的水痕
                m.SetFloat("_Wetness", suffix == "Title" ? 0.18f : 1f);
                m.SetFloat("_Ink", 1f);
                m.SetFloat("_Note", 0f);
                m.SetFloat("_Foot", 0f);
                // 背面：同一种纸，但没有字，只有水痕
                if (name == "Book_Page_Verso")
                {
                    // 叶背：与正面同一张纸，只是略沉一档；墨层必须为 0（所有叶共用同一张墨贴图，会透出镜像书名）
                    m.SetFloat("_Ink", 0.0f);
                    m.SetFloat("_Note", 0.0f);
                    m.SetFloat("_Foot", 0f);
                    m.SetFloat("_SealStrength", 0f);
                    m.SetFloat("_Wetness", 0.26f);
                    m.SetFloat("_StainStrength", 0.50f);
                    m.SetColor("_Color", Lin(new Color(0.870f, 0.850f, 0.782f)));
                    m.SetFloat("_GutterDark", 0.12f);
                    m.SetFloat("_EdgeDark", 0.14f);
                }

                m.SetFloat("_Glossiness", 0.10f);
                m.SetFloat("_WetGloss", 2.2f);
                m.SetFloat("_RimStrength", 0.05f);
                m.SetFloat("_Wrap", 0.74f);
                m.SetFloat("_RampSoftness", 0.55f);
                m.SetFloat("_AmbientBoost", 1.8f);
            }

            EditorUtility.SetDirty(m);
            matCache[name] = m;
            return m;
        }

        static void BuildBookMaterials()
        {
            BookMat("Book_Cover");
            BookMat("Book_Spine");
            BookMat("Book_PageEdge");
            BookMat("Book_Page_Title");
            for (int i = 0; i < LeafCount; i++) BookMat("Book_Page_" + i);
        }

        static Texture2D LoadBookTex(string name)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(BookTexDir + "/" + name + ".png");
            if (t == null)
            {
                AssetDatabase.ImportAsset(BookTexDir + "/" + name + ".png", ImportAssetOptions.ForceSynchronousImport);
                t = AssetDatabase.LoadAssetAtPath<Texture2D>(BookTexDir + "/" + name + ".png");
            }
            return t;
        }

        // ---------------------------------------------------------------- 墨迹烘培

        static void BakePageTextures()
        {
            for (int i = 0; i < BookPages.Length; i++)
            {
                var p = BookPages[i];
                int idx = i;

                BakeLayer("Book_Ink_" + idx, c =>
                {
                    VCol(c, "Volume", p.volume, 46, 96, 120, new Color(1f, 1f, 1f, 0.72f));
                    VCol(c, "Place", p.place, 132, 96, 232, Color.white);
                });

                BakeLayer("Book_Note_" + idx, c =>
                {
                    VCol(c, "Line", p.line, 62, 300, 236, Color.white);
                    VCol(c, "Hand", p.hand, 58, 396, 300, new Color(1f, 1f, 1f, 0.94f));
                });

                BakeLayer("Book_Foot_" + idx, c =>
                {
                    // 页高 1440，行高约 1.18 倍字号：34pt × 12 字 ≈ 480px，850 起笔留出 110px 余量。
                    // 原来 40pt 从 978 起笔，写 12 字直接冲出纸底。
                    VCol(c, "Note1", p.note1, 34, 470, 850, new Color(1f, 1f, 1f, 0.82f));
                    VCol(c, "Note2", p.note2, 34, 570, 850, new Color(1f, 1f, 1f, 0.82f));
                });
            }

            // 末页：只有一句，靠下
            BakeLayer("Book_Ink_3", c => { VCol(c, "Empty", " ", 40, 100, 200, Color.white); });
            BakeLayer("Book_Note_3", c =>
            {
                VCol(c, "Last", "此頁留給後來者。", 76, 300, 300, Color.white);
            });
            BakeLayer("Book_Foot_3", c => { VCol(c, "Empty", " ", 40, 100, 200, Color.white); });

            // 书名页
            BakeLayer("Book_Ink_Title", c =>
            {
                VCol(c, "Title", "河山册", 148, 330, 250, Color.white);
                VCol(c, "Sub", "此冊隨行", 48, 178, 660, new Color(1f, 1f, 1f, 0.78f));
            });
            BakeLayer("Book_Note_Title", c => { VCol(c, "Empty", " ", 40, 100, 200, Color.white); });
            BakeLayer("Book_Foot_Title", c => { VCol(c, "Empty", " ", 40, 100, 200, Color.white); });

            BakeSeal();
        }

        /// <summary>朱印：朱地上留白字（白文），四字右起竖读「河山 / 之印」。</summary>
        static void BakeSeal()
        {
            // 印面不用贴花，直接烘成一张「朱地、白文」的遮罩塞进纸面的印章通道：
            // RGB 用朱砂色（后面按亮度取用），四字留黑，纸透过字口露出来
            BakeLayer("Book_Seal", BakeW, BakeH, c =>
            {
                var bg = new GameObject("Ground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bg.layer = 5;
                bg.transform.SetParent(c, false);
                var brt = bg.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(134f, 134f);
                brt.anchoredPosition = new Vector2(SealU * BakeW - BakeW * 0.5f, SealV * BakeH - BakeH * 0.5f);
                bg.GetComponent<Image>().color = HuaxiaPalette.Cinnabar;

                var cut = new Color(0f, 0f, 0f, 1f);
                Glyph(c, "河", 56, new Vector2(SealU * BakeW - BakeW * 0.5f + 28f, SealV * BakeH - BakeH * 0.5f + 28f), cut);
                Glyph(c, "山", 56, new Vector2(SealU * BakeW - BakeW * 0.5f + 28f, SealV * BakeH - BakeH * 0.5f - 30f), cut);
                Glyph(c, "之", 56, new Vector2(SealU * BakeW - BakeW * 0.5f - 28f, SealV * BakeH - BakeH * 0.5f + 28f), cut);
                Glyph(c, "印", 56, new Vector2(SealU * BakeW - BakeW * 0.5f - 28f, SealV * BakeH - BakeH * 0.5f - 30f), cut);
            }, false);   // 保留朱砂 RGB：纸面着色器靠「红多于绿」认印面，字口留黑自然透出纸
        }

        /// <summary>单个字，居中放在给定位置。</summary>
        static void Glyph(Transform parent, string ch, int size, Vector2 pos, Color color)
        {
            var go = new GameObject(ch, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size * 1.6f, size * 1.6f);
            rt.anchoredPosition = pos;

            var t = go.AddComponent<Text>();
            t.font = bookPage;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.text = ch;
        }

        static void BakeLayer(string name, System.Action<Transform> layout)
        {
            BakeLayer(name, BakeW, BakeH, layout);
        }

        static void BakeLayer(string name, int w, int h, System.Action<Transform> layout)
        {
            BakeLayer(name, w, h, layout, true);
        }

        static void BakeLayer(string name, int w, int h, System.Action<Transform> layout, bool whiteRgb)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;
            rt.Create();

            var camGO = new GameObject("BakeCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = h * 0.5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << 5;             // 只画 UI 层
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.targetTexture = rt;

            var canvasGO = new GameObject("BakeCanvas", typeof(RectTransform), typeof(Canvas));
            canvasGO.layer = 5;
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;

            var root = canvasGO.transform;
            layout(root);

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            int ink = 0;
            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i += 7) if (px[i].a > 40) ink++;
            if (whiteRgb)
                for (int i = 0; i < px.Length; i++) { px[i].r = 255; px[i].g = 255; px[i].b = 255; }
            tex.SetPixels32(px);
            tex.Apply();

            Directory.CreateDirectory(Directory.GetParent(Application.dataPath).FullName + "/" + BookTexDir);
            File.WriteAllBytes(Directory.GetParent(Application.dataPath).FullName + "/" + BookTexDir + "/" + name + ".png",
                tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGO);
            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(rt);

            AssetDatabase.ImportAsset(BookTexDir + "/" + name + ".png", ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(BookTexDir + "/" + name + ".png") as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }

            if (ink < 40) Debug.LogWarning("[Huaxia] 墨迹烘培可能为空: " + name);
        }

        /// <summary>竖排一列：一个字一行。</summary>
        static void VCol(Transform parent, string name, string text, int size, float right, float top, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(size * 1.6f, 1600f);
            rt.anchoredPosition = new Vector2(-right, -top);

            var t = go.AddComponent<Text>();
            t.font = bookPage;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.UpperCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.0f;
            t.supportRichText = false;
            t.raycastTarget = false;

            var sb = new System.Text.StringBuilder();
            foreach (char ch in text)
            {
                if (ch == ' ') continue;
                sb.Append(ch).Append('\n');
            }
            t.text = sb.ToString().TrimEnd('\n');
        }

        static void BakeStainTexture()
        {
            const int S = 512;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];
            float seed = 12.7f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;

                    // r：沿页边的水痕
                    float b = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.16f, 0.015f, b));
                    float n1 = Mathf.PerlinNoise(u * 7f + seed, v * 7f + seed) * 0.6f +
                               Mathf.PerlinNoise(u * 19f, v * 19f) * 0.4f;
                    float ring = edge * (0.55f + 0.45f * n1);
                    // 4 块大斑
                    float blot = 0f;
                    for (int k = 0; k < 4; k++)
                    {
                        float cx = Mathf.PerlinNoise(k * 3.1f, k * 1.7f);
                        float cy = Mathf.PerlinNoise(k * 2.3f + 5f, k * 4.9f);
                        float rad = 0.06f + 0.10f * Mathf.PerlinNoise(k * 7.7f, k * 2.2f);
                        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.12f + cx * 0.76f, 0.12f + cy * 0.76f));
                        blot = Mathf.Max(blot, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rad, rad * 0.15f, d)));
                    }
                    blot *= 0.5f + 0.5f * Mathf.PerlinNoise(u * 9f + 30f, v * 9f + 30f);

                    // g：细潮斑
                    float mottle = Mathf.PerlinNoise(u * 14f + 60f, v * 14f + 60f) * 0.6f +
                                   Mathf.PerlinNoise(u * 31f + 11f, v * 31f + 11f) * 0.4f;
                    mottle = Mathf.SmoothStep(0.35f, 0.85f, mottle);

                    // b：干透后留下的淡圈
                    float dry = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.20f, 0.03f, b)) * 0.55f + blot * 0.45f;

                    px[y * S + x] = new Color32(
                        (byte)(Mathf.Clamp01(Mathf.Max(ring, blot)) * 255f),
                        (byte)(Mathf.Clamp01(mottle) * 255f),
                        (byte)(Mathf.Clamp01(dry) * 255f), 255);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            Directory.CreateDirectory(Directory.GetParent(Application.dataPath).FullName + "/" + BookTexDir);
            File.WriteAllBytes(Directory.GetParent(Application.dataPath).FullName + "/" + BookTexDir + "/Book_Stain.png",
                tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(BookTexDir + "/Book_Stain.png", ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(BookTexDir + "/Book_Stain.png") as TextureImporter;
            if (imp != null)
            {
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        // ------------------------------------------------------- 验收场景与预览

        const string BookLabScenePath = SceneDir + "/BookLab.unity";
        const float LabBookY = 1.25f;
        const float LabSpacing = 0.72f;

        [MenuItem("Huaxia/仅构建：河山册验收场景")]
        public static void BuildBookLabMenu()
        {
            BuildBookPrefab();
            BuildBookLabScene();
        }

        /// <summary>
        /// 案上四本，四种状态并排，只为看图与验收：湿卷 / 卷一已干 / 卷一有页边注 / 末页。
        /// </summary>
        public static void BuildBookLabScene()
        {
            EnsureFolder(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = SkyMat("Sky_BookLab", HuaxiaPalette.DuskZenith, HuaxiaPalette.DuskHorizon, 1.45f, 1.10f, 0.80f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(HuaxiaPalette.Sand) * 0.95f;
            RenderSettings.ambientEquatorColor = Lin(HuaxiaPalette.Sand) * 0.80f;
            RenderSettings.ambientGroundColor = Lin(HuaxiaPalette.WoodDark) * 0.70f;
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = false;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 2.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sunGo.transform.rotation = Quaternion.Euler(38f, 202f, 0f);

            var table = MatAs("BookLabDesk", Color.Lerp(HuaxiaPalette.WoodBrown, HuaxiaPalette.PaperWarm, 0.30f), null, 0f, 0.16f);
            Box("BookLabDeskTop", null, new Vector3(0f, 1.06f, 0f),
                new Vector3(3.4f, 0.06f, 1.30f), table, false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i < 2 ? -1f : 1f) * 1.45f;
                float z = (i % 2 == 0 ? -1f : 1f) * 0.55f;
                Box("BookLabLeg_" + i, null, new Vector3(x, 0.53f, z),
                    new Vector3(0.09f, 1.06f, 0.09f), table, false);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BookPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Huaxia] 河山册预制体缺失: " + BookPrefabPath);
                return;
            }

            for (int v = 0; v < 4; v++)
                MakeLabBook(prefab, v, new Vector3((v - 1.5f) * LabSpacing, LabBookY, 0f));

            EditorSceneManager.SaveScene(scene, BookLabScenePath);
            Debug.Log("[Huaxia] 书案场景: " + BookLabScenePath);
        }

        static void MakeLabBook(GameObject prefab, int variant, Vector3 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "Book_" + variant;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(-58f, 0f, 0f);

            var book = go.GetComponent<HandBook>();
            if (book != null) book.enabled = false;

            var leaves = go.GetComponentsInChildren<BookLeaf>(true);
            foreach (var leaf in leaves)
            {
                leaf.enabled = false;
                if (leaf.face != null && leaf.face.sharedMaterial != null)
                    leaf.face.sharedMaterial = new Material(leaf.face.sharedMaterial);
                if (leaf.back != null && leaf.back.sharedMaterial != null)
                    leaf.back.sharedMaterial = new Material(leaf.back.sharedMaterial);
            }

            // 即便有人在编辑器里按播放，这一本也要停在画好的状态上
            System.Array.Sort(leaves, (a, b) => string.CompareOrdinal(a.name, b.name));

            switch (variant)
            {
                case 0: // 刚入册：纸还湿着，只认得出卷名
                    leaves[0].SetWetImmediate(1f);
                    leaves[0].SetInkImmediate(1f);
                    leaves[0].SetNoteImmediate(0f);
                    leaves[0].SetFootImmediate(0f);
                    break;

                case 1: // 卷一已干：页面字与手记显出来
                    leaves[0].SetWetImmediate(0f);
                    leaves[0].SetInkImmediate(1f);
                    leaves[0].SetNoteImmediate(1f);
                    leaves[0].SetFootImmediate(0f);
                    break;

                case 2: // 卷一已干 + 一条页边注
                    leaves[0].SetWetImmediate(0f);
                    leaves[0].SetInkImmediate(1f);
                    leaves[0].SetNoteImmediate(1f);
                    leaves[0].SetFootImmediate(1f);
                    break;

                case 3: // 末页：前三叶翻到左边
                    for (int i = 0; i < 3; i++)
                    {
                        var p = leaves[i].transform;
                        p.localRotation = Quaternion.Euler(0f, 0f, 180f);
                        // 翻过去的叶挂在枢轴下方，枢轴得抬到书名页之上，叶才不会扎进纸里
                        p.localPosition = new Vector3(0f, BoardT + LeafT * 2f + 0.0003f + i * (LeafT + 0.0002f), 0f);
                        leaves[i].SetWetImmediate(0f);
                        leaves[i].SetNoteImmediate(1f);
                    }
                    var last = leaves[3];
                    last.SetWetImmediate(0f);
                    last.SetInkImmediate(1f);
                    last.SetNoteImmediate(1f);
                    last.SetFootImmediate(0f);
                    break;
            }
        }

        // ------------------------------------------------ 手中视角（验收「书就是 UI」）

        /// <summary>抬手位姿与 BookHolder 的 raisedPos / raisedEuler 一致，预览图才代表实机看到的样子。</summary>
        static readonly Vector3 GripPos = new Vector3(0f, 1.58f - 0.175f, 0.40f);

        [MenuItem("Huaxia/仅渲染：手中河山册（进度三态）")]
        public static void RenderGripPreviews()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BookPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Huaxia] 河山册预制体缺失: " + BookPrefabPath);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(HuaxiaPalette.Sand) * 0.85f;
            RenderSettings.ambientEquatorColor = Lin(HuaxiaPalette.Sand) * 0.62f;
            RenderSettings.ambientGroundColor = Lin(HuaxiaPalette.WoodDark) * 0.55f;
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = false;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sunGo.transform.rotation = Quaternion.Euler(52f, 188f, 0f);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "HeshanBook_Grip";
            go.transform.position = GripPos;
            go.transform.rotation = Quaternion.Euler(-58f, 0f, 0f);

            var book = go.GetComponent<HandBook>();
            if (book != null) book.enabled = false;

            var leaves = go.GetComponentsInChildren<BookLeaf>(true);
            foreach (var leaf in leaves)
            {
                leaf.enabled = false;
                if (leaf.face != null && leaf.face.sharedMaterial != null)
                    leaf.face.sharedMaterial = new Material(leaf.face.sharedMaterial);
                if (leaf.back != null && leaf.back.sharedMaterial != null)
                    leaf.back.sharedMaterial = new Material(leaf.back.sharedMaterial);
            }
            System.Array.Sort(leaves, (a, b) => string.CompareOrdinal(a.name, b.name));

            // 相机按书摊开的实际包围盒取景：手离纸约 0.4 m，俯角约 20°
            Bounds bb = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                bb.Encapsulate(r.bounds);

            var camGo = new GameObject("GripCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.115f, 0.100f, 0.086f);

            float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float radius = bb.extents.magnitude;
            // 书页朝 -Z（BookLab 与 BookHolder 都是这个朝向）：从 -Z 侧俯看才是摊开的纸
            Vector3 dir = Quaternion.Euler(22f, 0f, 0f) * Vector3.forward;
            cam.transform.position = bb.center - dir * (radius / Mathf.Sin(halfFov) * 0.92f);
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // 三态：刚入册 / 卷一读完 / 卷一读完且页边落笔
            string[] names = { "book_grip_entry", "book_grip_done", "book_grip_note" };
            for (int v = 0; v < names.Length; v++)
            {
                bool done = v > 0;
                leaves[0].SetWetImmediate(done ? 0f : 1f);
                leaves[0].SetInkImmediate(1f);
                leaves[0].SetNoteImmediate(done ? 1f : 0f);
                leaves[0].SetFootImmediate(v > 1 ? 1f : 0f);
                ShootGrip(cam, "Previews/" + names[v] + ".png");
            }

            Debug.Log("[Huaxia] 手中书预览完成");
        }

        static void ShootGrip(Camera cam, string outPath)
        {
            int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Previews");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, Path.GetFileName(outPath)), tex.EncodeToPNG());
            Debug.Log("[Huaxia] 手中书: " + Path.Combine(dir, Path.GetFileName(outPath)));

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        [MenuItem("Huaxia/仅渲染：河山册预览图")]
        public static void RenderBookPreviews()
        {
            EditorSceneManager.OpenScene(BookLabScenePath);
            RenderPreview(BookLabScenePath, new Vector3(0f, 2.05f, -1.55f), new Vector3(0f, 1.15f, 0f),
                "Previews/book_lab.png", 55f);

            string[] names = { "book_wet", "book_dry", "book_note", "book_last" };
            for (int v = 0; v < names.Length; v++)
            {
                float x = (v - 1.5f) * LabSpacing;
                var at = new Vector3(x, LabBookY, 0f);
                RenderPreview(BookLabScenePath, at + new Vector3(0f, 0.34f, -0.53f), at,
                    "Previews/" + names[v] + ".png", 40f);
            }
        }
    }
}
