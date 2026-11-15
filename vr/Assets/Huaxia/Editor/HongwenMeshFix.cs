using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 弘文那份 Tripo 模型带着一小撮 UV 全为 (0,0) 的碎三角：贴图 (0,0) 是黑的，
    /// 于是她脸上会冒出一块硬边黑斑（用户报的“脸上黑点”）。
    /// 这些碎三角其实是盖在肤色脸上的补片，删掉会露出背后的空洞，所以只改 UV：
    /// 每个连通块整体换成一个“和周围肤色最接近”的皮肤 UV（整块同一个 UV，避免被采到高 mip），
    /// 补片就变成肤色，黑斑消失。模型需要 isReadable，故 Hongwen.fbx.meta 里开着。
    /// </summary>
    public class HongwenMeshFix : AssetPostprocessor
    {
        const string TargetAsset = "Assets/Huaxia/Resources/Heritage/DigitalHuman/Hongwen.fbx";
        const string FallbackTexturePath =
            "Assets/Huaxia/Resources/Heritage/DigitalHuman/Hongwen.fbm/" +
            "tripo_node_f505687b-b2b7-4d4b-9dc1-4a69123029b9_Clone_BaseColor.png";
        const float Cell = 0.01f;
        static readonly Vector2 FallbackUv = new Vector2(0.0623f, 0.5414f);   // 脸颊中段的一块肤色

        void OnPostprocessModel(GameObject root)
        {
            if (assetPath != TargetAsset) return;
            var tex = LoadTexture();
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Fix(smr.sharedMesh, tex);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                Fix(mf.sharedMesh, tex);
        }

        static Texture2D LoadTexture()
        {
            if (System.IO.File.Exists(FallbackTexturePath))
            {
                var bytes = System.IO.File.ReadAllBytes(FallbackTexturePath);
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(t, bytes)) return t;
            }
            Debug.LogWarning("[Huaxia] 弘文肤色贴图不存在 " + FallbackTexturePath);
            return null;
        }

        static bool IsSkin(Texture2D tex, Vector2 uv)
        {
            if (tex == null) return true;
            var c = tex.GetPixelBilinear(Mathf.Repeat(uv.x, 1f), Mathf.Repeat(uv.y, 1f));
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return c.r > 0.35f && c.r >= c.b && (max - min) < 0.35f;
        }

        static void Fix(Mesh mesh, Texture2D tex)
        {
            if (mesh == null) return;
            var uv = mesh.uv;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            if (uv == null || uv.Length != verts.Length) return;

            var grid = new Dictionary<Vector3Int, List<int>>();
            var broken = new List<int>();
            var isBroken = new bool[uv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                if (uv[i].sqrMagnitude < 1e-6f) { broken.Add(i); isBroken[i] = true; continue; }
                var key = CellOf(verts[i]);
                List<int> bucket;
                if (!grid.TryGetValue(key, out bucket)) { bucket = new List<int>(); grid[key] = bucket; }
                bucket.Add(i);
            }
            if (broken.Count == 0) return;

            // 补片按连通块处理：整块用同一个 UV，避免块内 UV 跨度太大被 GPU 采到高 mip 而混色。
            var parent = new int[uv.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                if (isBroken[a] && isBroken[b]) Union(parent, a, b);
                if (isBroken[b] && isBroken[c]) Union(parent, b, c);
                if (isBroken[a] && isBroken[c]) Union(parent, a, c);
            }

            var comps = new Dictionary<int, List<int>>();
            for (int k = 0; k < broken.Count; k++)
            {
                int r = Find(parent, broken[k]);
                List<int> list;
                if (!comps.TryGetValue(r, out list)) { list = new List<int>(); comps[r] = list; }
                list.Add(broken[k]);
            }

            int fixedCount = 0;
            foreach (var comp in comps.Values)
            {
                // 目标色：补片周围一圈正常顶点的贴图色，取平均，尽量贴近局部肤色。
                Vector3 center = Vector3.zero;
                for (int k = 0; k < comp.Count; k++) center += verts[comp[k]];
                center /= comp.Count;
                var key = CellOf(center);

                Color target = Color.black;
                int targetN = 0;
                var cand = new List<KeyValuePair<Vector2, Color>>();
                for (int dx = -4; dx <= 4; dx++)
                for (int dy = -4; dy <= 4; dy++)
                for (int dz = -4; dz <= 4; dz++)
                {
                    List<int> bucket;
                    if (!grid.TryGetValue(new Vector3Int(key.x + dx, key.y + dy, key.z + dz), out bucket)) continue;
                    for (int b = 0; b < bucket.Count; b++)
                    {
                        int vi = bucket[b];
                        float d = (verts[vi] - center).sqrMagnitude;
                        if (d > 0.0004f) continue;              // 只认 2 cm 内的邻居
                        var c = tex == null ? Color.white : tex.GetPixelBilinear(uv[vi].x, uv[vi].y);
                        if (tex != null && !IsSkin(tex, uv[vi])) continue;
                        target += c;
                        targetN++;
                        if (d > 0.0001f) cand.Add(new KeyValuePair<Vector2, Color>(uv[vi], c));
                    }
                }

                Vector2 chosen = FallbackUv;
                if (targetN > 0 && cand.Count > 0)
                {
                    target /= targetN;
                    float best = float.MaxValue;
                    for (int c = 0; c < cand.Count; c++)
                    {
                        var col = cand[c].Value;
                        float dd = (col.r - target.r) * (col.r - target.r) + (col.g - target.g) * (col.g - target.g)
                                   + (col.b - target.b) * (col.b - target.b);
                        if (dd < best) { best = dd; chosen = cand[c].Key; }
                    }
                }
                else
                {
                    // 周围没有可信邻居时，退回“最近的肤色顶点”。
                    float bestD = float.MaxValue;
                    for (int k = 0; k < comp.Count; k++)
                    {
                        int bi = comp[k];
                        var p = verts[bi];
                        var kk = CellOf(p);
                        for (int dx = -3; dx <= 3; dx++)
                        for (int dy = -3; dy <= 3; dy++)
                        for (int dz = -3; dz <= 3; dz++)
                        {
                            List<int> bucket;
                            if (!grid.TryGetValue(new Vector3Int(kk.x + dx, kk.y + dy, kk.z + dz), out bucket)) continue;
                            for (int b = 0; b < bucket.Count; b++)
                            {
                                int vi = bucket[b];
                                float d = (verts[vi] - p).sqrMagnitude;
                                if (d >= 0.0009f || d >= bestD) continue;
                                if (!IsSkin(tex, uv[vi])) continue;
                                bestD = d;
                                chosen = uv[vi];
                            }
                        }
                    }
                }

                for (int k = 0; k < comp.Count; k++) uv[comp[k]] = chosen;
                fixedCount += comp.Count;
            }

            mesh.uv = uv;
            Debug.Log("[Huaxia] 弘文模型：把 " + fixedCount + " 个 UV 为 (0,0) 的补片顶点（" + comps.Count +
                      " 块）改到肤色 UV（" + mesh.name + "）");
        }

        static int Find(int[] parent, int i)
        {
            while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
            return i;
        }

        static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a), rb = Find(parent, b);
            if (ra != rb) parent[ra] = rb;
        }

        static Vector3Int CellOf(Vector3 p)
        {
            return new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell));
        }
    }
}
