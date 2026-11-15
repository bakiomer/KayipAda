// ShipParts.cs
// Gemi modelini (Quaternius Ship_Large, tek parça mesh) aşamalarda görünecek parçalara ayırır:
// - Gövde: güverte seviyesinin altındaki her şey (+ güvertedeki alçak yapılar).
// - Direkler: güvertenin üstündeki ahşap/koyu renkli kısımlar, direk direk ayrı.
// - Yelkenler: güvertenin üstündeki açık/kırmızı renkli (bez) kısımlar, direk direk ayrı.
// Renk, modelin doku atlasından üçgenin UV'sine bakılarak bulunur. Omurga ve kaburgalar modelde olmadığı için
// kodla (kutulardan) üretilir. Parçaların meshleri Art/Generated/Ship_*.asset olarak kaydedilir.
// Her parça bir "menteşe" (pivot) altına konur: direkler tabanından yükselir, yelkenler tepeden açılır.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class ShipParts
    {
        public class Result
        {
            public Transform Keel;      // omurga + kaburgalar (sırayla belirir)
            public Transform Hull;      // gövde (aşağıdan yukarı belirir)
            public Transform Masts;     // direkler (tabandan yükselir)
            public Transform Sails;     // yelkenler (tepeden açılır)
            public List<Renderer> HullRenderers = new List<Renderer>();
            public float DeckY;         // güverte yüksekliği (shipRoot uzayında)
            public Vector3 TopOfTallestMast;
        }

        private const string MeshFolder = ArtGenerator.GeneratedFolder;

        // model: shipRoot altında, yönü ayarlanmış gemi modeli. shipRoot dönüşü henüz kimlik olmalı (uzay = shipRoot uzayı).
        public static Result Split(GameObject model, Transform shipRoot, Material hullMaterial, Material partMaterial, Material ribMaterial,
                                   string atlasPath)
        {
            Result result = new Result();
            MeshFilter source = model.GetComponentInChildren<MeshFilter>();
            if (source == null || source.sharedMesh == null) return result;

            Mesh mesh = source.sharedMesh;
            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] tris = mesh.triangles;
            Matrix4x4 toRoot = shipRoot.worldToLocalMatrix * source.transform.localToWorldMatrix;
            Vector3[] local = new Vector3[verts.Length];
            Bounds b = new Bounds(toRoot.MultiplyPoint3x4(verts[0]), Vector3.zero);
            for (int i = 0; i < verts.Length; i++)
            {
                local[i] = toRoot.MultiplyPoint3x4(verts[i]);
                b.Encapsulate(local[i]);
            }
            float deckY = b.min.y + b.size.y * MapLayout.ShipDeckFraction;
            float upper = deckY + (b.max.y - deckY) * 0.45f;
            result.DeckY = deckY;

            Texture2D atlas = LoadReadable(atlasPath);

            // 1) Üçgen sınıfı: 0 gövde, 1 direk/ahşap üst, 2 yelken/bez.
            int triCount = tris.Length / 3;
            int[] kind = new int[triCount];
            float[] along = new float[triCount];
            float[] centerY = new float[triCount];
            for (int t = 0; t < triCount; t++)
            {
                int a = tris[t * 3], c = tris[t * 3 + 1], d = tris[t * 3 + 2];
                Vector3 center = (local[a] + local[c] + local[d]) / 3f;
                float minY = Mathf.Min(local[a].y, Mathf.Min(local[c].y, local[d].y));
                along[t] = center.z;
                centerY[t] = center.y;
                if (minY <= deckY) { kind[t] = 0; continue; }
                Color color = atlas != null && uvs.Length == verts.Length
                    ? atlas.GetPixelBilinear((uvs[a].x + uvs[c].x + uvs[d].x) / 3f, (uvs[a].y + uvs[c].y + uvs[d].y) / 3f)
                    : Color.gray;
                kind[t] = IsCloth(color) ? 2 : 1;
            }

            // 2) Direk eksenleri: üst kısımdaki direk üçgenlerinin boy eksenindeki kümeleri.
            List<float> tops = new List<float>();
            for (int t = 0; t < triCount; t++) if (kind[t] == 1 && centerY[t] > upper) tops.Add(along[t]);
            tops.Sort();
            List<float> masts = new List<float>();
            List<int> counts = new List<int>();
            foreach (float z in tops)
            {
                if (masts.Count > 0 && z - masts[masts.Count - 1] / counts[counts.Count - 1] < MapLayout.ShipMastGap)
                {
                    masts[masts.Count - 1] += z;
                    counts[counts.Count - 1]++;
                }
                else
                {
                    masts.Add(z);
                    counts.Add(1);
                }
            }
            for (int i = 0; i < masts.Count; i++) masts[i] /= counts[i];

            // 3) Her üst üçgeni en yakın direğe ata; direği olmayan alçak ahşap kısımlar gövdeye kalır.
            int[] mastOf = new int[triCount];
            float[] mastTop = new float[Mathf.Max(1, masts.Count)];
            for (int t = 0; t < triCount; t++)
            {
                mastOf[t] = -1;
                if (kind[t] == 0 || masts.Count == 0) continue;
                int best = 0;
                for (int m = 1; m < masts.Count; m++) if (Mathf.Abs(along[t] - masts[m]) < Mathf.Abs(along[t] - masts[best])) best = m;
                if (Mathf.Abs(along[t] - masts[best]) > MapLayout.ShipMastReach && centerY[t] < upper) { kind[t] = 0; continue; }
                mastOf[t] = best;
                mastTop[best] = Mathf.Max(mastTop[best], centerY[t]);
            }

            // 4) Parça meshleri.
            Transform root = shipRoot;
            result.Hull = Group("Hull", root);
            result.Masts = Group("Masts", root);
            result.Sails = Group("Sails", root);
            Renderer hull = MakePart("Ship_Hull", result.Hull, source.transform, mesh, tris, t => kind[t] == 0, hullMaterial);
            if (hull != null) result.HullRenderers.Add(hull);

            int tallest = 0;
            for (int m = 0; m < masts.Count; m++)
            {
                if (mastTop[m] > mastTop[tallest]) tallest = m;
                int index = m;
                // Direk: tabanından yükselir (menteşe güvertede).
                Transform mastPivot = Pivot("Mast " + m, result.Masts, shipRoot, new Vector3(0f, deckY, masts[m]));
                MakePart("Ship_Mast" + m, mastPivot, source.transform, mesh, tris, t => kind[t] == 1 && mastOf[t] == index, partMaterial);
                // Yelken: tepeden aşağı açılır (menteşe direğin tepesinde).
                Transform sailPivot = Pivot("Sail " + m, result.Sails, shipRoot, new Vector3(0f, mastTop[m], masts[m]));
                MakePart("Ship_Sail" + m, sailPivot, source.transform, mesh, tris, t => kind[t] == 2 && mastOf[t] == index, partMaterial);
            }
            result.TopOfTallestMast = masts.Count > 0 ? new Vector3(0f, mastTop[tallest], masts[tallest]) : new Vector3(0f, b.max.y, 0f);

            // Orijinal tek parça model artık gerekmez.
            Object.DestroyImmediate(model);

            // 5) Omurga + kaburgalar (kodla): sırayla belirecek parçalar.
            result.Keel = Group("Keel", root);
            BuildKeel(result.Keel, local, b, deckY, ribMaterial);
            return result;
        }

        private static bool IsCloth(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            bool white = max > 0.72f && max - min < 0.2f;
            bool red = c.r > 0.5f && c.g < 0.35f && c.b < 0.35f;
            return white || red;
        }

        private static Transform Group(string name, Transform parent)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static Transform Pivot(string name, Transform parent, Transform shipRoot, Vector3 rootLocal)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = shipRoot.TransformPoint(rootLocal);
            return t;
        }

        // Kaynak meshin seçilen üçgenlerinden yeni (sıkıştırılmış) mesh ve nesne üretir.
        private static Renderer MakePart(string assetName, Transform parent, Transform sourceTransform, Mesh source, int[] tris,
                                         System.Func<int, bool> include, Material material)
        {
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            Vector2[] uv = source.uv;
            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<Vector3> nv = new List<Vector3>();
            List<Vector3> nn = new List<Vector3>();
            List<Vector2> nuv = new List<Vector2>();
            List<int> nt = new List<int>();
            for (int t = 0; t < tris.Length / 3; t++)
            {
                if (!include(t)) continue;
                for (int k = 0; k < 3; k++)
                {
                    int old = tris[t * 3 + k];
                    if (!remap.TryGetValue(old, out int idx))
                    {
                        idx = nv.Count;
                        remap[old] = idx;
                        nv.Add(v[old]);
                        if (n.Length == v.Length) nn.Add(n[old]);
                        if (uv.Length == v.Length) nuv.Add(uv[old]);
                    }
                    nt.Add(idx);
                }
            }
            if (nt.Count == 0) return null;

            Mesh mesh = new Mesh { name = assetName };
            if (nv.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(nv);
            if (nn.Count == nv.Count) mesh.SetNormals(nn);
            if (nuv.Count == nv.Count) mesh.SetUVs(0, nuv);
            mesh.SetTriangles(nt, 0);
            mesh.RecalculateBounds();
            if (nn.Count != nv.Count) mesh.RecalculateNormals();
            mesh = BuilderUtil.SaveAsset(mesh, MeshFolder + "/" + assetName + ".asset");

            GameObject go = new GameObject(assetName);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
            Vector3 parentScale = parent.lossyScale;
            Vector3 s = sourceTransform.lossyScale;
            go.transform.localScale = new Vector3(s.x / parentScale.x, s.y / parentScale.y, s.z / parentScale.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            return r;
        }

        // Omurga (geminin altında boydan boya kalın kiriş) + kaburgalar (gövde kesitine uyan U şekilli çerçeveler).
        private static void BuildKeel(Transform parent, Vector3[] local, Bounds b, float deckY, Material material)
        {
            float length = b.size.z * 0.86f;
            Piece(parent, "Keel", new Vector3(b.center.x, b.min.y + 0.12f, b.center.z), new Vector3(0.28f, 0.28f, length), Quaternion.identity, material);

            int ribs = MapLayout.ShipRibCount;
            for (int i = 0; i < ribs; i++)
            {
                float z = Mathf.Lerp(b.min.z + length * 0.12f, b.max.z - length * 0.12f, (i + 0.5f) / ribs);
                // Bu kesitte gövdenin genişliği (güverte altında).
                float half = 0.3f;
                foreach (Vector3 p in local) if (Mathf.Abs(p.z - z) < 0.25f && p.y < deckY) half = Mathf.Max(half, Mathf.Abs(p.x - b.center.x));
                half *= 0.9f;
                float height = deckY - b.min.y;
                Transform rib = new GameObject("Rib " + i).transform;
                rib.SetParent(parent, false);
                rib.localPosition = new Vector3(b.center.x, b.min.y, z);
                // İki yan (hafif dışa açık) + alt.
                Piece(rib, "Side L", new Vector3(-half * 0.8f, height * 0.55f, 0f), new Vector3(0.14f, height * 0.95f, 0.16f), Quaternion.Euler(0f, 0f, -12f), material);
                Piece(rib, "Side R", new Vector3(half * 0.8f, height * 0.55f, 0f), new Vector3(0.14f, height * 0.95f, 0.16f), Quaternion.Euler(0f, 0f, 12f), material);
                Piece(rib, "Bottom", new Vector3(0f, 0.16f, 0f), new Vector3(half * 1.5f, 0.14f, 0.16f), Quaternion.identity, material);
            }
        }

        private static void Piece(Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion rot, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Texture2D LoadReadable(string path)
        {
            if (!File.Exists(path)) return null;
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }
    }
}
