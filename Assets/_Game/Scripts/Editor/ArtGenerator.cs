// ArtGenerator.cs
// Hazır paketlerde olmayan görselleri kodla üretir (Assets/_Game/Art/Generated) ve paylaşılan malzemeleri kurar:
// - Ada zemini modeli: organik kıyı, düz üst yüzey, denize inen kumsal eğimi (IslandShape'ten).
// - Ada dokusu: ıslak kum → kum → çimen geçişi, bölge tonları (kayalık, bahçe), açık renkli yürüme yolları.
// - Su ağı (dalga için ızgara) ve kıyıya uzaklık dokusu (su shader'ı renk ve köpük için okur).
// - Arayüz/efekt dokuları: daire, halka, yumuşak daire (gölge), yuvarlak köşeli panel, panel gölgesi, para ikonu.
// - Malzemeler (Assets/_Game/Art/Materials): her biri tek dosya ve paylaşımlı (telefonda ucuz).

using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class ArtGenerator
    {
        public const string GeneratedFolder = BuilderUtil.Root + "/Art/Generated";
        public const string MaterialsFolder = BuilderUtil.Root + "/Art/Materials";

        public const string CirclePath = GeneratedFolder + "/Circle.png";
        public const string RingPath = GeneratedFolder + "/Ring.png";
        public const string SoftCirclePath = GeneratedFolder + "/SoftCircle.png";
        public const string RoundedRectPath = GeneratedFolder + "/RoundedRect.png";
        public const string PanelShadowPath = GeneratedFolder + "/PanelShadow.png";
        public const string CoinIconPath = GeneratedFolder + "/CoinIcon.png";
        public const string SquarePath = GeneratedFolder + "/Square.png";
        public const string ArrowPath = GeneratedFolder + "/Arrow.png";
        public const string IslandTexturePath = GeneratedFolder + "/IslandGround.png";
        public const string ShoreTexturePath = GeneratedFolder + "/ShoreDistance.png";
        public const string IslandMeshPath = GeneratedFolder + "/IslandMesh.asset";
        public const string WaterMeshPath = GeneratedFolder + "/WaterMesh.asset";

        public const float WaterLevel = -0.12f;
        public const float WaterSize = 150f;
        public const float ShoreMaxDistance = 16f;

        // ================================================================ Arayüz/efekt dokuları

        public static void GenerateSprites()
        {
            BuilderUtil.EnsureFolder(GeneratedFolder);
            MakeSprite(CirclePath, 256, (x, y, s) => Mathf.Clamp01((s * 0.5f - 1f - Dist(x, y, s)) / 1.5f), 0);
            MakeSprite(RingPath, 256, (x, y, s) =>
            {
                float d = Dist(x, y, s);
                return Mathf.Clamp01((s * 0.5f - 2f - d) / 1.5f) * Mathf.Clamp01((d - s * 0.37f) / 1.5f);
            }, 0);
            MakeSprite(SoftCirclePath, 128, (x, y, s) =>
            {
                float k = Mathf.Clamp01(Dist(x, y, s) / (s * 0.5f));
                return Mathf.Pow(1f - Mathf.SmoothStep(0f, 1f, k), 1.5f);
            }, 0);
            MakeSprite(RoundedRectPath, 64, (x, y, s) => Mathf.Clamp01(0.5f - RoundedBox(x, y, s, 28f)), 30);
            MakeSprite(PanelShadowPath, 128, (x, y, s) => (1f - Mathf.SmoothStep(-14f, 16f, RoundedBox(x, y, s, 44f) + 14f)) * 0.55f, 56);
            MakeSprite(SquarePath, 16, (x, y, s) => 1f, 0);
            MakeCoinIcon(CoinIconPath, 128);
            MakeSprite(ArrowPath, 128, ArrowAlpha, 0);
        }

        // Yukarıyı gösteren kalın ok (4 alt örnekle yumuşak kenar).
        private static float ArrowAlpha(int x, int y, int size)
        {
            float sum = 0f;
            for (int sy = 0; sy < 2; sy++)
            for (int sx = 0; sx < 2; sx++)
            {
                float u = (x + 0.25f + sx * 0.5f) / size, v = (y + 0.25f + sy * 0.5f) / size;
                bool head = v >= 0.42f && v <= 0.94f && Mathf.Abs(u - 0.5f) <= (0.94f - v) * 0.85f;
                bool shaft = v >= 0.06f && v < 0.46f && Mathf.Abs(u - 0.5f) <= 0.14f;
                if (head || shaft) sum += 0.25f;
            }
            return sum;
        }

        private static float Dist(int x, int y, int size)
        {
            float c = (size - 1) * 0.5f;
            return new Vector2(x - c, y - c).magnitude;
        }

        // Yuvarlak köşeli kutunun kenarına işaretli uzaklık (içeride eksi). Kutu, dokuyu (kenar payıyla) doldurur.
        private static float RoundedBox(int x, int y, int size, float radius)
        {
            float c = (size - 1) * 0.5f;
            Vector2 p = new Vector2(Mathf.Abs(x - c), Mathf.Abs(y - c));
            Vector2 half = new Vector2(c - 1f, c - 1f);
            Vector2 q = p - half + Vector2.one * radius;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static void MakeSprite(string path, int size, System.Func<int, int, int, float> alpha, int border)
        {
            if (!System.IO.File.Exists(path))
            {
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha(x, y, size));
                tex.SetPixels(pixels);
                BuilderUtil.WritePng(tex, path);
            }
            ConfigureSprite(path, border);
        }

        private static void MakeCoinIcon(string path, int size)
        {
            if (!System.IO.File.Exists(path))
            {
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color rim = new Color(0.82f, 0.52f, 0.08f);
                Color face = new Color(1f, 0.80f, 0.20f);
                Color shine = new Color(1f, 0.93f, 0.55f);
                float r = size * 0.5f - 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Dist(x, y, size);
                        float a = Mathf.Clamp01((r - d) / 1.5f);
                        Color c = d > r * 0.78f ? rim : face;
                        // Sol üstte parlama, ortada çukur halka.
                        float hx = x - size * 0.38f, hy = y - size * 0.62f;
                        if (d < r * 0.78f && hx * hx + hy * hy < (size * 0.16f) * (size * 0.16f)) c = shine;
                        if (Mathf.Abs(d - r * 0.55f) < 2.5f) c = rim;
                        c.a = a;
                        tex.SetPixel(x, y, c);
                    }
                }
                BuilderUtil.WritePng(tex, path);
            }
            ConfigureSprite(path, 0);
        }

        private static void ConfigureSprite(string path, int border)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }

        public static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // ================================================================ Ada

        // Dokunun kapladığı alan (XZ): ada + denize inen kumsal eğimi.
        public static Rect IslandTextureRect(IslandShape island)
        {
            Vector2 half = island.Radius * 1.15f + Vector2.one * 5f;
            return new Rect(island.Center - half, half * 2f);
        }

        public static Mesh BuildIslandMesh(IslandShape island)
        {
            const int segments = 160;
            float[] topRings = { 0.25f, 0.5f, 0.7f, 0.85f, 0.95f, 1f };      // kıyıya oran
            float[] skirtOut = { 0.7f, 1.8f, 3.4f };                         // kıyıdan dışarı (m)
            float[] skirtY = { -0.28f, -0.75f, -1.6f };
            Rect uvRect = IslandTextureRect(island);

            int ringCount = topRings.Length + skirtOut.Length;
            Vector3[] vertices = new Vector3[1 + ringCount * segments];
            Vector2[] uvs = new Vector2[vertices.Length];
            vertices[0] = new Vector3(island.Center.x, 0f, island.Center.y);

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 coast = island.CoastPoint(angle);
                Vector2 outward = (coast - island.Center).normalized;
                for (int r = 0; r < ringCount; r++)
                {
                    Vector3 v;
                    if (r < topRings.Length)
                    {
                        Vector2 p = Vector2.Lerp(island.Center, coast, topRings[r]);
                        v = new Vector3(p.x, 0f, p.y);
                    }
                    else
                    {
                        int k = r - topRings.Length;
                        Vector2 p = coast + outward * skirtOut[k];
                        v = new Vector3(p.x, skirtY[k], p.y);
                    }
                    vertices[1 + r * segments + i] = v;
                }
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2((vertices[i].x - uvRect.xMin) / uvRect.width, (vertices[i].z - uvRect.yMin) / uvRect.height);
            }

            System.Collections.Generic.List<int> tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(0); tris.Add(1 + next); tris.Add(1 + i); // merkez yelpazesi
                for (int r = 0; r < ringCount - 1; r++)
                {
                    int a = 1 + r * segments + i, b = 1 + r * segments + next;
                    int c = 1 + (r + 1) * segments + i, d = 1 + (r + 1) * segments + next;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            Mesh mesh = new Mesh { name = "IslandMesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return BuilderUtil.SaveAsset(mesh, IslandMeshPath);
        }

        public static Texture2D BuildIslandTexture(IslandShape island)
        {
            const int size = 1024;
            Rect rect = IslandTextureRect(island);
            Color wetSand = new Color(0.80f, 0.69f, 0.49f);
            Color sand = new Color(0.97f, 0.88f, 0.66f);
            Color grassA = new Color(0.45f, 0.73f, 0.31f);
            Color grassB = new Color(0.55f, 0.80f, 0.36f);
            Color dirt = new Color(0.88f, 0.76f, 0.54f);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(rect.xMin + (x + 0.5f) / size * rect.width, rect.yMin + (y + 0.5f) / size * rect.height);
                    float sd = island.SignedDistance(p);
                    float n1 = Fbm(p * 0.25f);
                    float n2 = Fbm(p * 1.3f + new Vector2(17f, 3f));

                    // Kum: kıyıda ıslak ve koyu, içeri doğru açık.
                    Color sandColor = Color.Lerp(wetSand, sand, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, -1.2f, sd)));

                    // Çimen: kum şeridinin içinde, dalgalı kenarla.
                    float edge = -(MapLayout.SandWidth + (n1 - 0.5f) * 2.4f);
                    float grass = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge + 0.5f, edge - 0.5f, sd));
                    Color grassColor = Color.Lerp(grassA, grassB, n2);
                    grassColor = Color.Lerp(grassColor, MapLayout.RockyTint * (0.9f + 0.2f * n2), RegionWeight(p, MapLayout.RockyRect) * 0.75f);
                    grassColor = Color.Lerp(grassColor, MapLayout.OrchardTint * (0.92f + 0.16f * n2), RegionWeight(p, MapLayout.OrchardRect) * 0.6f);

                    Color c = Color.Lerp(sandColor, grassColor, grass);

                    // Yürüme yolları: açık renkli toprak, hafif dalgalı kenar.
                    float dp = MapLayout.DistanceToPaths(p) + (n2 - 0.5f) * 0.35f;
                    float halfWidth = MapLayout.PathWidth * 0.5f;
                    float path = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfWidth + 0.2f, halfWidth - 0.2f, dp));
                    c = Color.Lerp(c, dirt * (0.96f + 0.08f * n2), path * Mathf.Lerp(0.35f, 1f, grass));

                    // Su altındaki kum koyulaşır.
                    if (sd > 0f) c = Color.Lerp(c, wetSand * 0.8f, Mathf.Clamp01(sd / 2.5f));

                    c *= 0.95f + 0.08f * Fbm(p * 4f);
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            BuilderUtil.WritePng(tex, IslandTexturePath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(IslandTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return BuilderUtil.Load<Texture2D>(IslandTexturePath);
        }

        // Dikdörtgenin içinde 1, dışına doğru yumuşakça 0.
        private static float RegionWeight(Vector2 p, Rect r)
        {
            float dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
            float dz = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
            return 1f - Mathf.Clamp01(new Vector2(dx, dz).magnitude / 1.5f);
        }

        // ================================================================ Su

        public static Rect WaterRect(IslandShape island)
        {
            return new Rect(island.Center - Vector2.one * (WaterSize * 0.5f), Vector2.one * WaterSize);
        }

        public static Mesh BuildWaterMesh(IslandShape island)
        {
            const int cells = 75;
            Rect rect = WaterRect(island);
            int n = cells + 1;
            Vector3[] vertices = new Vector3[n * n];
            int[] tris = new int[cells * cells * 6];
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                    vertices[z * n + x] = new Vector3(rect.xMin + rect.width * x / cells, 0f, rect.yMin + rect.height * z / cells);

            int t = 0;
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int a = z * n + x;
                    tris[t++] = a; tris[t++] = a + n; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = a + n; tris[t++] = a + n + 1;
                }
            }

            Mesh mesh = new Mesh { name = "WaterMesh" };
            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.up * 2f); // dalgalar için pay
            return BuilderUtil.SaveAsset(mesh, WaterMeshPath);
        }

        // Her pikselde: kıyıya uzaklık / ShoreMaxDistance (karada 0). Su shader'ı okur.
        public static Texture2D BuildShoreTexture(IslandShape island)
        {
            const int size = 512;
            Rect rect = WaterRect(island);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(rect.xMin + (x + 0.5f) / size * rect.width, rect.yMin + (y + 0.5f) / size * rect.height);
                    float v = Mathf.Clamp01(island.SignedDistance(p) / ShoreMaxDistance);
                    pixels[y * size + x] = new Color(v, v, v);
                }
            }
            tex.SetPixels(pixels);
            BuilderUtil.WritePng(tex, ShoreTexturePath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ShoreTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false; // renk değil, uzaklık verisi
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return BuilderUtil.Load<Texture2D>(ShoreTexturePath);
        }

        // ================================================================ Malzemeler

        // Var olan malzemeyi günceller, yoksa oluşturur (GUID korunur → prefab bağlantıları bozulmaz).
        public static Material GetMaterial(string name, string shaderName, System.Action<Material> setup)
        {
            BuilderUtil.EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("Kayıp Ada: Shader bulunamadı: " + shaderName + " (URP Unlit kullanılıyor)");
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material mat = BuilderUtil.Load<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.enableInstancing = true;
            setup?.Invoke(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Mat, ışık alan malzeme (URP Simple Lit: telefonda Lit'ten ucuz, parlama yok).
        public static Material GetLit(string name, Texture texture, Color color)
        {
            return GetMaterial(name, "Universal Render Pipeline/Simple Lit", m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", 0.1f);
            });
        }

        public static Material GetTransparent(string name, Texture texture, Color color)
        {
            return GetMaterial(name, "KayipAda/UnlitTransparent", m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", color);
                m.renderQueue = 3000;
            });
        }

        public static Material GetWater(IslandShape island, Texture shoreTexture)
        {
            Rect rect = WaterRect(island);
            return GetMaterial("M_Water", "KayipAda/Water", m =>
            {
                m.SetTexture("_ShoreTex", shoreTexture);
                m.SetVector("_ShoreRect", new Vector4(rect.xMin, rect.yMin, rect.width, rect.height));
                m.SetFloat("_ShoreMaxDistance", ShoreMaxDistance);
                m.renderQueue = 3000;
            });
        }

        // ================================================================ Gürültü

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        private static float ValueNoise(Vector2 p)
        {
            int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y);
            float fx = p.x - x0, fy = p.y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(x0, y0), Hash(x0 + 1, y0), fx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        // 0..1 arası doğal görünümlü gürültü.
        public static float Fbm(Vector2 p)
        {
            return (ValueNoise(p) * 0.5f + ValueNoise(p * 2.03f) * 0.25f + ValueNoise(p * 4.01f) * 0.125f) / 0.875f;
        }
    }
}
