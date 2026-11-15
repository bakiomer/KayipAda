// MapLayout.cs
// Haritanın yerleşimi tek yerde: ada şekli, bölgeler, nesne konumları, yürüme yolları (XZ düzlemi, metre).
// Bunlar oynanış ayarı değil, harita tasarımıdır. Kamera güneyden (-Z) kuzeye (+Z) bakar.
//
//                    ~~~~~~~~ deniz ~~~~~~~~
//                         [iskele]   (batık gemi)      z ≈ 25+
//        ┌──── KAYALIK ────┐   yol   ┌─ MEYVE BAHÇESİ ─┐
//        │ kaya  TAŞ ATÖLY.│    |    │ ağaç     ağaç   │   z 6..15 (çitli, başta kapalı)
//        │ kaya       kaya │    |    │      ağaç       │
//        └──────[kare]─────┘    |    └─────[kare]──────┘   z 4.5
//               palmiye    palmiye    palmiye             z -1..2  BAŞLANGIÇ SAHİLİ
//  KAMP  [oduncu kare]      oyuncu      [taşıyıcı kare]    z -6
//            PAZAR                    BIÇKIHANE           z -12

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class MapLayout
    {
        // Ada (elips + kıyı dalgaları, IslandShape)
        public static readonly Vector2 IslandCenter = new Vector2(0f, 3f);
        public static readonly Vector2 IslandRadius = new Vector2(20f, 24f);
        public const float SandWidth = 3.4f;     // kıyıdan içeri kum şeridi
        public const float PathWidth = 1.7f;

        public static readonly Vector3 PlayerStart = new Vector3(0f, 0f, -6f);

        // Başlangıç sahili
        public static readonly Vector3[] Palms = { new Vector3(-4.5f, 0f, -1f), new Vector3(0f, 0f, 1.8f), new Vector3(4.5f, 0f, -1f) };
        public static readonly Vector3 Market = new Vector3(-5f, 0f, -11.5f);
        public static readonly Vector3 Sawmill = new Vector3(5f, 0f, -12f);
        public static readonly Vector3 Camp = new Vector3(-11f, 0f, -5f);
        public static readonly Vector3 CampSpawn = new Vector3(-9f, 0f, -6.8f);
        public static readonly Vector3 LumberjackPad = new Vector3(-6.5f, 0f, -6.2f);
        public static readonly Vector3 CarrierPad = new Vector3(6.5f, 0f, -6.2f);

        // Kayalık (kuzeybatı) ve Meyve Bahçesi (kuzeydoğu): XZ dikdörtgenleri
        public static readonly Rect RockyRect = new Rect(-11f, 6f, 9f, 9f);
        public static readonly Rect OrchardRect = new Rect(2f, 6f, 7.5f, 9f);
        public static readonly Vector3 RockyPad = new Vector3(-6.5f, 0f, 4.4f);
        public static readonly Vector3 OrchardPad = new Vector3(5.75f, 0f, 4.4f);
        public static readonly Vector3[] Rocks = { new Vector3(-9f, 0f, 8.4f), new Vector3(-4.3f, 0f, 8.4f), new Vector3(-9.3f, 0f, 12.6f) };
        public static readonly Vector3 Workshop = new Vector3(-5f, 0f, 12.6f);
        public static readonly Vector3[] FruitTrees = { new Vector3(4f, 0f, 8.6f), new Vector3(8f, 0f, 8.6f), new Vector3(6f, 0f, 12.8f) };

        // Palmiye Koyu (doğu): lif veren palmiyeler. Kuzeyinde Halat Atölyesi ve Dokuma Tezgahı.
        public static readonly Rect CoveRect = new Rect(10.5f, -6f, 5f, 8f);
        public static readonly Vector3 CovePad = new Vector3(9.2f, 0f, -2f);
        public static readonly Vector3[] FiberPalms = { new Vector3(12f, 0f, -4.3f), new Vector3(14.3f, 0f, -1.6f), new Vector3(12f, 0f, 0.8f) };
        public static readonly Vector3 RopeWorkshop = new Vector3(13.2f, 0f, 6f);
        public static readonly Vector3 Loom = new Vector3(12.6f, 0f, 12f);
        public static readonly Vector3 FiberWorkerPad = new Vector3(8.6f, 0f, 2.4f);

        // Kampın yanındaki geliştirme tezgahı
        public static readonly Vector3 UpgradeStation = new Vector3(-8f, 0f, -2.2f);

        // Tersane: gemi iskelenin doğusunda kumsalda, kızağın üstünde; pruvası denize bakar.
        public const float ShipX = 6.5f;
        public const float ShipInland = 3.2f;   // geminin ortası kıyıdan bu kadar içeride
        public const float ShipBowTilt = 20f;   // gemi kıyıya paralel; burnu bu kadar (derece) denize dönük
        public const float ShipCradleHeight = 0.35f; // kızak altlığının yüksekliği (gemi bunun üstünde)
        public const int ShipBlockCount = 5;    // gövdeyi kapatan engel karesi sayısı
        public const float DeliveryGap = 2.2f;  // teslim alanının gövde kenarından uzaklığı
        public const int ShipRailCount = 4;     // denize uzanan kayma rayı sayısı
        public const float ShipRailLength = 12f;
        public const float ShipDeckFraction = 0.36f; // güverte yüksekliği (gemi boyunun oranı): altı gövde
        public const float ShipMastGap = 0.9f;  // farklı direk sayılması için boy eksenindeki en az uzaklık
        public const float ShipMastReach = 1.6f; // bir parçanın direğe ait sayıldığı en fazla uzaklık
        public const int ShipRibCount = 7;      // omurga aşamasındaki kaburga sayısı
        public static readonly Color ShipWoodColor = new Color(0.55f, 0.34f, 0.17f);
        public static readonly Color ShipPlankColor = new Color(0.78f, 0.58f, 0.36f);
        public static readonly Vector3 DeliveryFromShip = new Vector3(-3.8f, 0f, -1.2f);
        public static readonly Vector3 WreckOffset = new Vector3(-10f, 0f, 3f);

        // Sırttaki kaynakların renkleri (kolay ayırt edilsin).
        public static readonly Color ItemStoneColor = new Color(0.62f, 0.63f, 0.67f);
        public static readonly Color ItemBrickColor = new Color(0.86f, 0.4f, 0.22f);
        public static readonly Color ItemFiberColor = new Color(0.97f, 0.83f, 0.3f);
        public static readonly Color ItemClothColor = new Color(0.97f, 0.94f, 0.84f);

        // Bölge çiti: kapı aralığı (metre), çit engelinin kalınlığı, kapı kanadının açılma açısı, tabela yazısının yüksekliği (oran).
        public const float GateWidth = 2.8f;
        public const float FenceThickness = 0.35f;
        public const float GateOpenAngle = 100f;
        public const float SignTextHeight = 0.72f;
        public static readonly Color SignWoodColor = new Color(0.58f, 0.38f, 0.2f);

        // Ağaçların doğal çeşitliliği: en fazla eğim (derece), boy farkı (0.1 = ±%10), rastgele tohum.
        public const float TreeTiltMax = 8f;
        public const float TreeSizeJitter = 0.1f;
        public const int TreeSeed = 4242;

        // Bina karesi binanın neresinde durur (binanın köküne göre)
        public static readonly Vector3 BuildingPadOffset = new Vector3(0f, 0f, -0.6f);

        // Yürüme yolları (çoklu çizgiler, XZ). Zemin dokusuna açık renkli toprak yol olarak çizilir;
        // süsler bu yolların üstüne konmaz.
        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(0f, -15f), new Vector2(0f, 24f) },                                   // ana yol: sahil → iskele
            new[] { new Vector2(0f, -9f), new Vector2(-5f, -10f) },                                  // pazar
            new[] { new Vector2(0f, -9f), new Vector2(5f, -10.5f) },                                 // bıçkıhane
            new[] { new Vector2(0f, 4.4f), new Vector2(-6.5f, 4.4f), new Vector2(-6.5f, 10.5f) },    // kayalık
            new[] { new Vector2(0f, 4.4f), new Vector2(5.75f, 4.4f), new Vector2(5.75f, 10.5f) },    // meyve bahçesi
            new[] { new Vector2(6.5f, -6.2f), new Vector2(9.2f, -2f) },                              // palmiye koyu
            new[] { new Vector2(5.75f, 4.4f), new Vector2(9.9f, 3.3f), new Vector2(9.9f, 11.5f) },   // halat ve dokuma
            new[] { new Vector2(0f, -6f), new Vector2(-6.5f, -6.2f), new Vector2(-10f, -5.6f) },     // kamp
            new[] { new Vector2(0f, -6f), new Vector2(6.5f, -6.2f) }
        };

        // Bölge zemin tonları (doku üretiminde)
        public static readonly Color RockyTint = new Color(0.62f, 0.60f, 0.50f);
        public static readonly Color OrchardTint = new Color(0.36f, 0.70f, 0.28f);

        // İskele: kıyının kuzey ucundan denize uzanır (ada şeklinden hesaplanır).
        public static Vector3 DockStart(IslandShape island)
        {
            Vector2 coast = island.CoastPoint(Mathf.PI / 2f);
            return new Vector3(coast.x, 0f, coast.y - 2.2f);
        }

        // Yolların kıvrımı: en fazla sapma (metre) ve dalga sıklığı (radyan/metre).
        public const float PathWiggle = 0.45f;
        public const float PathWiggleFrequency = 0.35f;

        private static Vector2[][] curvedPaths;
        private static Rect[] curvedBounds;

        // Yolların yumuşatılmış, hafif kıvrımlı hali (cetvel gibi düz değil). Köşeler yuvarlanır, uzun düz
        // parçalar hafifçe kıvrılır; parçaların uçları yerinde kalır (yollar birbirine bağlı kalır).
        public static Vector2[][] CurvedPaths
        {
            get
            {
                if (curvedPaths == null)
                {
                    curvedPaths = BuildCurvedPaths();
                    curvedBounds = new Rect[curvedPaths.Length];
                    for (int k = 0; k < curvedPaths.Length; k++)
                    {
                        Vector2 min = curvedPaths[k][0], max = min;
                        foreach (Vector2 v in curvedPaths[k]) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
                        curvedBounds[k] = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    }
                }
                return curvedPaths;
            }
        }

        private static Vector2[][] BuildCurvedPaths()
        {
            Vector2[][] result = new Vector2[Paths.Length][];
            for (int k = 0; k < Paths.Length; k++)
            {
                Vector2[] line = Paths[k];
                List<Vector2> points = new List<Vector2>();
                float travelled = 0f;
                for (int i = 0; i < line.Length - 1; i++)
                {
                    Vector2 p0 = i > 0 ? line[i - 1] : line[i];
                    Vector2 a = line[i], b = line[i + 1];
                    Vector2 p3 = i + 2 < line.Length ? line[i + 2] : b;
                    float length = Vector2.Distance(a, b);
                    int steps = Mathf.Max(4, Mathf.CeilToInt(length / 0.5f));
                    Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized;
                    for (int s = 0; s < steps; s++)
                    {
                        float t = s / (float)steps;
                        float wiggle = Mathf.Sin(t * Mathf.PI) * Mathf.Sin((travelled + t * length) * PathWiggleFrequency + k * 1.7f);
                        points.Add(CatmullRom(p0, a, b, p3, t) + normal * wiggle * PathWiggle);
                    }
                    travelled += length;
                }
                points.Add(line[line.Length - 1]);
                result[k] = points.ToArray();
            }
            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        // Yol çizgisine (kıvrımlı hali) en yakın uzaklık (metre).
        public static float DistanceToPaths(Vector2 p)
        {
            float best = float.MaxValue;
            Vector2[][] lines = CurvedPaths;
            for (int k = 0; k < lines.Length; k++)
            {
                Vector2[] line = lines[k];
                Rect box = curvedBounds[k];
                float bx = Mathf.Max(box.xMin - p.x, 0f, p.x - box.xMax), bz = Mathf.Max(box.yMin - p.y, 0f, p.y - box.yMax);
                if (bx * bx + bz * bz >= best * best) continue; // bu yol zaten daha uzak (hızlı eleme)
                for (int i = 0; i < line.Length - 1; i++)
                {
                    best = Mathf.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
                }
            }
            return best;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
