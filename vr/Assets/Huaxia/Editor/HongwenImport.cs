using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>Inspects and configures the Tripo-authored 弘文 FBX (rigged + 11 takes).</summary>
    public static class HongwenImport
    {
        public const string FbxPath = "Assets/Huaxia/Resources/Heritage/DigitalHuman/Hongwen.fbx";

        [MenuItem("Huaxia/Digital Human/Probe Hongwen FBX")]
        public static void Probe()
        {
            var log = new StringBuilder();
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[Hongwen] FBX not imported: " + FbxPath);
                return;
            }

            log.AppendLine("[Hongwen] animationType=" + importer.animationType +
                           " importAnimation=" + importer.importAnimation +
                           " importBlendShapes=" + importer.importBlendShapes +
                           " useFileScale=" + importer.useFileScale +
                           " globalScale=" + importer.globalScale +
                           " materialImportMode=" + importer.materialImportMode);

            var takes = importer.importedTakeInfos;
            log.AppendLine("[Hongwen] takes=" + takes.Length);
            foreach (var t in takes) log.AppendLine("    take '" + t.name + "' default=" + t.defaultClipName + " bake=" + t.bakeStartTime.ToString("0.00") + "-" + t.bakeStopTime.ToString("0.00"));

            var clips = new List<AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) clips.Add(c);

            log.AppendLine("[Hongwen] clips=" + clips.Count);
            foreach (var c in clips)
            {
                log.AppendLine("    clip '" + c.name + "' len=" + c.length.ToString("0.00") +
                               "s rate=" + c.frameRate.ToString("0") +
                               " legacy=" + c.legacy +
                               " loop=" + c.wrapMode +
                               " curves=" + AnimationUtility.GetCurveBindings(c).Length);
                if (!c.legacy)
                {
                    log.AppendLine("        empty=" + c.empty +
                                   " hasRootCurves=" + c.hasRootCurves +
                                   " rootMotion=" + c.averageSpeed.magnitude.ToString("0.000"));
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab != null)
            {
                var go = (GameObject)Object.Instantiate(prefab);
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var renderers = go.GetComponentsInChildren<Renderer>();
                Bounds b = renderers.Length > 0 ? renderers[0].bounds : default;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                log.AppendLine("[Hongwen] renderers=" + renderers.Length +
                               " skinned=" + (smr != null) +
                               " boundsSize=" + b.size.ToString("0.00") +
                               " centerY=" + b.center.y.ToString("0.000") +
                               " minY=" + b.min.y.ToString("0.000") +
                               " maxY=" + b.max.y.ToString("0.000") +
                               " localScale=" + go.transform.localScale.ToString("0.0000") +
                               " rot=" + go.transform.eulerAngles.ToString("0"));
                if (smr != null)
                {
                    log.AppendLine("    bones=" + smr.bones.Length);
                    var shared = smr.sharedMesh;
                    if (shared != null)
                    {
                        log.AppendLine("    verts=" + shared.vertexCount + " tris=" + shared.triangles.Length / 3 +
                                       " blendShapes=" + shared.blendShapeCount +
                                       " subMeshes=" + shared.subMeshCount);
                        for (int i = 0; i < Mathf.Min(shared.blendShapeCount, 12); i++)
                            log.AppendLine("        shape '" + shared.GetBlendShapeName(i) + "'");
                    }
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        log.AppendLine("    mat[" + i + "]=" + (mats[i] == null ? "<none>" : mats[i].name +
                            " shader=" + mats[i].shader.name +
                            " tex=" + (mats[i].HasProperty("_MainTex") && mats[i].mainTexture != null ? mats[i].mainTexture.name : "<none>") +
                            " color=" + (mats[i].HasProperty("_Color") ? mats[i].color.ToString() : "?")));
                    }
                }

                var sb = new StringBuilder();
                DumpHierarchy(go.transform, 0, sb);
                log.AppendLine("[Hongwen] hierarchy:\n" + sb);

                var anim = go.GetComponentInChildren<Animation>();
                var animator = go.GetComponentInChildren<Animator>();
                log.AppendLine("[Hongwen] hasAnimation=" + (anim != null) + " hasAnimator=" + (animator != null));

                Object.DestroyImmediate(go);
            }

            Debug.Log(log.ToString());
        }


        const string ControlsPath = "Assets/Huaxia/Resources/Heritage/DigitalHuman";

        [MenuItem("Huaxia/Digital Human/Configure Hongwen FBX")]
        public static void Configure()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null) { Debug.LogError("[Hongwen] missing " + FbxPath); return; }

            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            Debug.Log("[Hongwen] configured as Legacy (clips playable through Animation component)");
        }

        [MenuItem("Huaxia/Digital Human/Measure Hongwen")]
        public static void Measure()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab == null) { Debug.LogError("[Hongwen] prefab missing"); return; }
            var clips = new List<AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) clips.Add(c);

            var go = (GameObject)Object.Instantiate(prefab);
            try
            {
                foreach (var c in clips)
                {
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int i = 0; i <= 8; i++)
                    {
                        c.SampleAnimation(go, c.length * i / 8f);
                        foreach (var t in go.GetComponentsInChildren<Transform>())
                        {
                            float y = t.position.y;
                            if (y < lo) lo = y;
                            if (y > hi) hi = y;
                        }
                    }
                    Debug.Log("[Hongwen] clip '" + c.name + "' boneY " + lo.ToString("0.000") + " .. " + hi.ToString("0.000") +
                              " height=" + (hi - lo).ToString("0.000") + " legacy=" + c.legacy);

                    var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null && (c.name == "wait" || c.name == "walk" || c.name == "look_around"))
                    {
                        var baked = new Mesh();
                        for (int i = 0; i <= 4; i++)
                        {
                            c.SampleAnimation(go, c.length * i / 4f);
                            smr.BakeMesh(baked);
                            var b = baked.bounds;
                            Debug.Log("[Hongwen]   posed '" + c.name + "' t=" + (c.length * i / 4f).ToString("0.00") +
                                      " minY=" + b.min.y.ToString("0.000") + " maxY=" + b.max.y.ToString("0.000") +
                                      " height=" + b.size.y.ToString("0.000") +
                                      " width=" + b.size.x.ToString("0.000") + " depth=" + b.size.z.ToString("0.000"));
                        }
                        Object.DestroyImmediate(baked);
                    }
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        static void DumpHierarchy(Transform t, int depth, StringBuilder sb)
        {
            sb.Append(' ', depth * 2).Append(t.name)
              .Append("  pos=").Append(t.localPosition.ToString("0.000"))
              .Append(" rot=").Append(t.localEulerAngles.ToString("0"))
              .Append(" scale=").Append(t.localScale.ToString("0.000"))
              .AppendLine();
            if (depth > 5) return;
            foreach (Transform c in t) DumpHierarchy(c, depth + 1, sb);
        }
    }
}
