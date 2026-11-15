using System;
using UnityEngine;

namespace Huaxia.EditorTools
{
    public static class MeshFactory
    {
        static readonly System.Collections.Generic.Dictionary<string, Mesh> BoxCache =
            new System.Collections.Generic.Dictionary<string, Mesh>();

        /// <summary>
        /// 把共享顶点的网格拆成逐三角面的独立顶点，得到平整的硬边法线。
        /// 台体、阙顶这类只有 8 个顶点的构件如果不拆，RecalculateNormals 会把
        /// 前立面和侧立面平均成 45° 的斜面法线——整面墙就变成一条从亮到黑的渐变，
        /// 读起来像被磨圆的橡皮，而不是夯土。
        /// </summary>
        public static Mesh Faceted(Mesh src)
        {
            var v = src.vertices;
            var uv = src.uv;
            var t = src.triangles;
            var nv = new Vector3[t.Length];
            var nuv = new Vector2[t.Length];
            var nt = new int[t.Length];
            bool hasUv = uv != null && uv.Length == v.Length;
            for (int i = 0; i < t.Length; i++)
            {
                nv[i] = v[t[i]];
                if (hasUv) nuv[i] = uv[t[i]];
                nt[i] = i;
            }
            var m = new Mesh { name = src.name + "_F" };
            if (t.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = nv;
            if (hasUv) m.uv = nuv;
            m.triangles = nt;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// 单位方块，但每个面的 UV 按真实尺寸展开（每 tile 米一个贴图周期）。
        /// 直接用 PrimitiveType.Cube 时 UV 恒为 0..1，大墙会被一张贴图拉满成大色块，
        /// 小构件则整张图塞进 0.3 米，两者都显得像迷彩。
        /// </summary>
        public static Mesh BoxUV(Vector3 size, float tile)
        {
            string key = Mathf.RoundToInt(size.x * 100f) + "|" + Mathf.RoundToInt(size.y * 100f) + "|" +
                         Mathf.RoundToInt(size.z * 100f) + "|" + Mathf.RoundToInt(tile * 100f);
            Mesh cached;
            if (BoxCache.TryGetValue(key, out cached) && cached != null) return cached;
            if (BoxCache.Count > 6000) BoxCache.Clear();

            float w = Mathf.Max(0.02f, size.x), h = Mathf.Max(0.02f, size.y), d = Mathf.Max(0.02f, size.z);
            var verts = new Vector3[24];
            var norms = new Vector3[24];
            var uvs = new Vector2[24];
            var tris = new int[36];
            int vi = 0, ti = 0;

            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 1f, 0f), d / tile, h / tile);
            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f), d / tile, h / tile);
            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(0f, 1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f), w / tile, d / tile);
            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), w / tile, d / tile);
            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), w / tile, h / tile);
            AddFace(verts, norms, uvs, tris, ref vi, ref ti,
                new Vector3(0f, 0f, -1f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f), w / tile, h / tile);

            var m = new Mesh();
            m.name = "BoxUV";
            m.vertices = verts;
            m.normals = norms;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateBounds();
            BoxCache[key] = m;
            return m;
        }

        static void AddFace(Vector3[] verts, Vector3[] norms, Vector2[] uvs, int[] tris,
                            ref int vi, ref int ti, Vector3 n, Vector3 u, Vector3 v, float su, float sv)
        {
            verts[vi + 0] = (n - u - v) * 0.5f;
            verts[vi + 1] = (n + u - v) * 0.5f;
            verts[vi + 2] = (n + u + v) * 0.5f;
            verts[vi + 3] = (n - u + v) * 0.5f;
            for (int i = 0; i < 4; i++) norms[vi + i] = n;
            uvs[vi + 0] = new Vector2(0f, 0f);
            uvs[vi + 1] = new Vector2(su, 0f);
            uvs[vi + 2] = new Vector2(su, sv);
            uvs[vi + 3] = new Vector2(0f, sv);
            tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 0; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;
            vi += 4;
            ti += 6;
        }
        /// <summary>带顶点色的地形网格。height / color 均由世界坐标 x,z 决定。</summary>
        public static Mesh Terrain(float size, int res, Func<float, float, float> height, Func<float, float, Color> color)
        {
            int n = res + 1;
            var verts = new Vector3[n * n];
            var cols = new Color[n * n];
            var uvs = new Vector2[n * n];

            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    float fx = ((x / (float)res) - 0.5f) * size;
                    float fz = ((z / (float)res) - 0.5f) * size;
                    int i = z * n + x;
                    verts[i] = new Vector3(fx, height(fx, fz), fz);
                    cols[i] = color(fx, fz).linear;
                    uvs[i] = new Vector2(x / (float)res, z / (float)res);
                }
            }

            var tris = new int[res * res * 6];
            int ti = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * n + x;
                    tris[ti++] = i;
                    tris[ti++] = i + n;
                    tris[ti++] = i + 1;

                    tris[ti++] = i + 1;
                    tris[ti++] = i + n;
                    tris[ti++] = i + n + 1;
                }
            }

            var mesh = new Mesh();
            mesh.name = "Terrain";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>双坡屋顶：屋脊沿 Z 轴。</summary>
        public static Mesh GableRoof(float width, float depth, float height, float overhang = 0.35f)
        {
            float hw = width * 0.5f + overhang;
            float hd = depth * 0.5f + overhang;

            var v = new Vector3[]
            {
                new Vector3(-hw, 0f, -hd),
                new Vector3( hw, 0f, -hd),
                new Vector3( hw, 0f,  hd),
                new Vector3(-hw, 0f,  hd),
                new Vector3(0f, height, -hd),
                new Vector3(0f, height,  hd),
            };

            var tris = new int[]
            {
                // 东侧坡面
                5, 2, 1,  4, 5, 1,
                // 西侧坡面
                4, 0, 3,  5, 4, 3,
                // 北侧山墙
                4, 1, 0,
                // 南侧山墙
                5, 3, 2,
            };

            // 逐面拆点，得到平整的坡面法线（避免脊线处法线被平均成弧形高光）
            var flat = new Vector3[tris.Length];
            var flatTris = new int[tris.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                flat[i] = v[tris[i]];
                flatTris[i] = i;
            }

            var mesh = new Mesh();
            mesh.name = "GableRoof";
            mesh.vertices = flat;
            mesh.triangles = flatTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 东方凹曲屋面：沿进深方向下凹，四角起翘，带板瓦厚度。
        /// 屋脊沿 Z 轴，檐口在 X 两端。
        /// </summary>
        public static Mesh CurvedRoof(float halfWidth, float halfDepth, float height,
                                      float cornerLift, float thickness, int resX = 22, int resZ = 18)
        {
            int nx = resX + 1, nz = resZ + 1;
            int count = nx * nz;

            Vector3 Point(float fx, float fz)
            {
                float x = halfWidth * fx;
                float z = halfDepth * fz;
                float ax = Mathf.Abs(fx);

                // 凹曲：近脊陡、近檐缓
                float y = height * Mathf.Pow(1f - ax, 1.75f);

                // 翼角起翘：越靠两端、越靠檐口，抬得越高
                float endF = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, Mathf.Abs(fz)));
                y += cornerLift * endF * Mathf.Pow(ax, 2.1f);

                // 角部外挑
                x *= 1f + 0.055f * endF * ax;
                z *= 1f + 0.075f * endF * ax;

                return new Vector3(x, y, z);
            }

            var verts = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    float fx = ix / (float)resX * 2f - 1f;
                    float fz = iz / (float)resZ * 2f - 1f;
                    int i = iz * nx + ix;
                    var p = Point(fx, fz);
                    verts[i] = p;                       // 顶面
                    verts[count + i] = p - Vector3.up * thickness; // 底面
                    uvs[i] = new Vector2(ix / (float)resX, iz / (float)resZ);
                    uvs[count + i] = uvs[i];
                }
            }

            var tris = new System.Collections.Generic.List<int>(resX * resZ * 12 + 400);
            for (int iz = 0; iz < resZ; iz++)
            {
                for (int ix = 0; ix < resX; ix++)
                {
                    int i = iz * nx + ix;
                    tris.Add(i); tris.Add(i + nx); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + nx); tris.Add(i + nx + 1);

                    int b = count + i;
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + nx);
                    tris.Add(b + 1); tris.Add(b + nx + 1); tris.Add(b + nx);
                }
            }

            // 檐口侧边（封厚），按逆时针周长走一圈，向外翻
            var ring = new System.Collections.Generic.List<int>();
            for (int ix = 0; ix < nx; ix++) ring.Add(0 * nx + ix);          // z = -max，向 +x
            for (int iz = 1; iz < nz; iz++) ring.Add(iz * nx + nx - 1);      // x = +max，向 +z
            for (int ix = nx - 2; ix >= 0; ix--) ring.Add((nz - 1) * nx + ix); // z = +max，向 -x
            for (int iz = nz - 2; iz >= 1; iz--) ring.Add(iz * nx + 0);       // x = -max，向 -z

            for (int k = 0; k < ring.Count; k++)
            {
                int a = ring[k];
                int b = ring[(k + 1) % ring.Count];
                int at = a, ab = count + a, bt = b, bb = count + b;
                tris.Add(at); tris.Add(bt); tris.Add(bb);
                tris.Add(at); tris.Add(bb); tris.Add(ab);
            }

            var mesh = new Mesh();
            mesh.name = "CurvedRoof";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>圆环板（月洞窗的石头窗套）：XY 平面，内外径之间的一圈，双面可见。</summary>
        public static Mesh PlateRing(float inner, float outer, int segments)
        {
            int n = Mathf.Max(8, segments);
            var verts = new Vector3[(n + 1) * 2 * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[n * 12];
            int v = 0, t = 0;
            for (int side = 0; side < 2; side++)
            {
                float dir = side == 0 ? 1f : -1f;
                int b = v;
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    verts[v] = new Vector3(ca * inner, sa * inner, 0f);
                    uvs[v++] = new Vector2(i / (float)n * Mathf.Max(1f, inner * 3f), 0f);
                    verts[v] = new Vector3(ca * outer, sa * outer, 0f);
                    uvs[v++] = new Vector2(i / (float)n * Mathf.Max(1f, outer * 3f), 1f);
                }
                for (int i = 0; i < n; i++)
                {
                    int i0 = b + i * 2, i1 = i0 + 1, i2 = i0 + 2, i3 = i0 + 3;
                    if (dir > 0f)
                    {
                        tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                        tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
                    }
                    else
                    {
                        tris[t++] = i0; tris[t++] = i1; tris[t++] = i2;
                        tris[t++] = i1; tris[t++] = i3; tris[t++] = i2;
                    }
                }
            }
            var mesh = new Mesh { name = "PlateRing" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>多棱柱（塔身、石幢一类）：底心在原点，往 +Y 长，每个侧面各自独立法线。</summary>
        public static Mesh Prism(float radius, float height, int sides, float tile = 1f)
        {
            int n = Mathf.Max(3, sides);
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            float top = height;
            for (int i = 0; i < n; i++)
            {
                float a0 = (i + 0.5f) / n * Mathf.PI * 2f;
                float a1 = (i + 1.5f) / n * Mathf.PI * 2f;
                var p0b = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1b = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                var p0t = new Vector3(p0b.x, top, p0b.z);
                var p1t = new Vector3(p1b.x, top, p1b.z);

                float uw = Vector3.Distance(p0b, p1b) / Mathf.Max(0.05f, tile);
                int b = verts.Count;
                verts.Add(p0b); verts.Add(p1b); verts.Add(p0t); verts.Add(p1t);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(uw, 0f));
                uvs.Add(new Vector2(0f, height / Mathf.Max(0.05f, tile)));
                uvs.Add(new Vector2(uw, height / Mathf.Max(0.05f, tile)));
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
            }

            // 顶面与底面各一张圆盘，省得从上往下看是个空壳
            for (int cap = 0; cap < 2; cap++)
            {
                bool up = cap == 0;
                float y = up ? top : 0f;
                int c = verts.Count;
                verts.Add(new Vector3(0f, y, 0f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f + Mathf.PI / n;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    verts.Add(new Vector3(ca * radius, y, sa * radius));
                    uvs.Add(new Vector2(ca * 0.5f + 0.5f, sa * 0.5f + 0.5f));
                }
                for (int i = 0; i < n; i++)
                {
                    int i0 = c + 1 + i, i1 = i0 + 1;
                    if (up) { tris.Add(c); tris.Add(i0); tris.Add(i1); }
                    else { tris.Add(c); tris.Add(i1); tris.Add(i0); }
                }
            }

            var mesh = new Mesh { name = "Prism" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>远山剪影：一道起伏的山脊幕布，可带平面内弯曲。</summary>
        public static Mesh MountainRidge(float width, float height, int peaks, int seed, float bow = 0f)
        {
            int seg = 192;
            var rnd = new System.Random(seed);
            float[] ph = new float[peaks];
            float[] fr = new float[peaks];
            float[] am = new float[peaks];
            for (int i = 0; i < peaks; i++)
            {
                ph[i] = (float)rnd.NextDouble() * 6.28f;
                fr[i] = 0.7f + (float)rnd.NextDouble() * 1.3f;
                am[i] = 0.35f + (float)rnd.NextDouble() * 0.65f;
            }

            // 先采样脊线高度，再多次平滑，避免正弦叠加出现锯齿状"低多边形"山峰
            float[] line = new float[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                float h = 0.30f;
                for (int p = 0; p < peaks; p++)
                    h += am[p] / peaks * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * fr[p] * 2.1f + ph[p]));

                // 只在两端收口，中段保持平缓起伏，避免整条山脊塌成一个圆丘
                float env = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.16f, t))
                            * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.84f, t));
                line[i] = h * env * height;
            }
            for (int pass = 0; pass < 1; pass++)
            {
                var tmp = (float[])line.Clone();
                for (int i = 1; i < seg; i++)
                    line[i] = (tmp[i - 1] + 2f * tmp[i] + tmp[i + 1]) * 0.25f;
            }

            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[(seg + 1) * 2];
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                float x = (t - 0.5f) * width;
                float bowZ = Mathf.Sin(t * Mathf.PI) * bow;

                verts[i * 2 + 0] = new Vector3(x, line[i], bowZ);
                verts[i * 2 + 1] = new Vector3(x, -6f, bowZ * 0.5f);
                uvs[i * 2 + 0] = new Vector2(t, 1f);
                uvs[i * 2 + 1] = new Vector2(t, 0f);
            }

            var tris = new int[seg * 6];
            int ti = 0;
            for (int i = 0; i < seg; i++)
            {
                int b0 = i * 2, t0 = i * 2 + 1, b1 = (i + 1) * 2, t1 = (i + 1) * 2 + 1;
                tris[ti++] = t0; tris[ti++] = b0; tris[ti++] = b1;
                tris[ti++] = t0; tris[ti++] = b1; tris[ti++] = t1;
            }

            var mesh = new Mesh();
            mesh.name = "MountainRidge";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>低模树冠：不规则球体。</summary>
        public static Mesh Canopy(float radius, int seed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(go);

            var rnd = new System.Random(seed);
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                float k = 0.82f + (float)rnd.NextDouble() * 0.36f;
                verts[i] *= k;
                verts[i].y *= 1.15f;
                verts[i] *= radius;
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            return mesh;
        }
        /// <summary>
        /// 带圆孔的矩形墙面（孔在原点，半径 r）。用扇形四边形精确覆盖「圆到矩形边界」的区域，
        /// 既没有缝隙也不重叠——用一排条块拼圆孔会同时出现漏光和错缝。
        /// </summary>
        public static Mesh PlateWithHole(float halfW, float halfH, float r, int segments)
        {
            segments = Mathf.Clamp(segments, 12, 128);
            var verts = new System.Collections.Generic.List<Vector3>();
            var norms = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                Vector2 p0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r;
                Vector2 p1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
                Vector2 o0 = Push(p0, halfW, halfH), o1 = Push(p1, halfW, halfH);

                int b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(o1); verts.Add(o0);
                for (int k = 0; k < 4; k++)
                {
                    norms.Add(Vector3.back);
                    Vector2 v = verts[b + k];
                    uvs.Add(new Vector2(v.x / 3f, v.y / 3f));
                }
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }

            var m = new Mesh { name = "PlateHole" };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            return m;
        }

        static Vector2 Push(Vector2 p, float halfW, float halfH)
        {
            float tx = Mathf.Abs(p.x) > 1e-4f ? halfW / Mathf.Abs(p.x) : float.MaxValue;
            float ty = Mathf.Abs(p.y) > 1e-4f ? halfH / Mathf.Abs(p.y) : float.MaxValue;
            return p * Mathf.Min(tx, ty);
        }
    }
}
