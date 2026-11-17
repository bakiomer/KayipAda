// GroundCheck.cs
// Sahne kurulduktan sonra havada duran ya da zemine fazla gömülmüş nesneleri bulur ve Console'a yazar.
// Her prefab örneğinin en alt noktası, bulunduğu yerdeki zemin yüksekliğiyle (ada içi y = 0, kıyı dışında
// eğimli etek) karşılaştırılır. Kapalı bölgelerdeki gizli nesneler de kontrol edilir (köşe noktaları
// meshten okunur, nesnenin açık olması gerekmez). Bilerek gömülen nesneler (iskele, batık gemi) atlanır.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KayipAda.EditorTools
{
    public static class GroundCheck
    {
        private const float FloatTolerance = 0.08f;  // en alt nokta zeminden bu kadar yüksekse "havada"
        private const float BuriedFraction = 0.5f;   // boyunun bu kadarı zeminin altındaysa "gömülü"

        // Bilerek suya/zemine gömülen ya da havada duran nesneler (ad başlangıcı).
        private static readonly string[] Skip = { "Collectibles", "Cloud", "Thicket", "Quarry", "OrchardProps", "CoveProps", "Arch", "Pier", "UpgradeSign", "Repair", "Cradle", "Bridge", "Dock", "ShipWreck", "Water", "Island", "Ship", "Slipway", "Scaffolding",
                                                  "Player", "Worker", "Enemy", "Coin", "FloatingText", "Confetti", "Dust",
                                                  "Sparkle", "Splash", "Firework", "Item_" };

        [MenuItem("Tools/Kayıp Ada/Zemin Kontrolü", false, 40)]
        public static void RunMenu()
        {
            int problems = Run();
            Debug.Log("KAYIPADA: Zemin kontrolü bitti, sorunlu nesne: " + problems);
        }

        // Sorunlu nesne sayısını döndürür; her sorunu ayrı satırda yazar.
        public static int Run()
        {
            IslandShape island = Object.FindFirstObjectByType<IslandShape>();
            HashSet<GameObject> roots = new HashSet<GameObject>();
            foreach (GameObject top in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer r in top.GetComponentsInChildren<Renderer>(true))
                {
                    GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                    if (root != null) roots.Add(root);
                }
            }

            int problems = 0;
            foreach (GameObject root in roots)
            {
                if (IsSkipped(root.transform)) continue;
                if (!MeasureY(root, out float minY, out float maxY)) continue;

                Vector3 p = root.transform.position;
                float ground = GroundHeight(island, new Vector2(p.x, p.z));
                float height = maxY - minY;
                string where = Path(root.transform);
                if (minY - ground > FloatTolerance)
                {
                    Debug.LogWarning("KAYIPADA: ZEMIN havada " + (minY - ground).ToString("0.00") + " m → " + where);
                    problems++;
                }
                else if (height > 0.05f && ground - minY > height * BuriedFraction)
                {
                    Debug.LogWarning("KAYIPADA: ZEMIN gömülü " + (ground - minY).ToString("0.00") + " m → " + where);
                    problems++;
                }
            }
            return problems;
        }

        private static bool IsSkipped(Transform t)
        {
            for (Transform x = t; x != null; x = x.parent)
            {
                foreach (string s in Skip) if (x.name.StartsWith(s)) return true;
                if (x.GetComponent<Canvas>() != null) return true;
            }
            return false;
        }

        // Meshlerin gerçek köşe noktalarından en alt ve en üst yükseklik (iskeletli meshler ve gizli nesneler dahil).
        // Yer işaretleri (halka, gölge, kare) ve yazılar ölçülmez; sadece modeller.
        private static bool MeasureY(GameObject root, out float minY, out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            List<Vector3> points = new List<Vector3>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Model") continue;
                if (t.GetComponentInParent<Canvas>(true) != null) continue;
                points.AddRange(PrefabFactory.WorldVertices(t.gameObject));
            }
            foreach (Vector3 v in points)
            {
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }
            return points.Count > 0;
        }

        // Ada içinde zemin y = 0; kıyıdan dışarı doğru ada eteğiyle aynı eğimde iner.
        private static float GroundHeight(IslandShape island, Vector2 p)
        {
            if (island == null) return 0f;
            float sd = island.SignedDistance(p);
            if (sd <= 0f) return 0f;
            return -Mathf.Min(sd * 0.4f, 1.6f);
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            for (Transform x = t.parent; x != null; x = x.parent) s = x.name + "/" + s;
            return s;
        }
    }
}
