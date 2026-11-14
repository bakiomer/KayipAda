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

using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class MapLayout
    {
        // Ada (elips + kıyı dalgaları, IslandShape)
        public static readonly Vector2 IslandCenter = new Vector2(0f, 3f);
        public static readonly Vector2 IslandRadius = new Vector2(17f, 22f);
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
        public static readonly Rect OrchardRect = new Rect(2f, 6f, 9f, 9f);
        public static readonly Vector3 RockyPad = new Vector3(-6.5f, 0f, 4.4f);
        public static readonly Vector3 OrchardPad = new Vector3(6.5f, 0f, 4.4f);
        public static readonly Vector3[] Rocks = { new Vector3(-9f, 0f, 8.4f), new Vector3(-4.3f, 0f, 8.4f), new Vector3(-9.3f, 0f, 12.6f) };
        public static readonly Vector3 Workshop = new Vector3(-5f, 0f, 12.6f);
        public static readonly Vector3[] FruitTrees = { new Vector3(4.5f, 0f, 8.6f), new Vector3(9f, 0f, 8.6f), new Vector3(6.8f, 0f, 12.8f) };

        // Kampın yanındaki geliştirme tezgahı
        public static readonly Vector3 UpgradeStation = new Vector3(-8f, 0f, -2.2f);

        // Tersane (iskele başlangıcına göre): gemi denizde, teslim alanı ve toplar karada.
        public static readonly Vector3 ShipOffset = new Vector3(7f, 0f, 7f);
        public static readonly Vector3 DeliveryOffset = new Vector3(3.6f, 0f, -2f);
        public static readonly Vector3[] CannonOffsets = { new Vector3(-5f, 0f, -3.5f), new Vector3(0.3f, 0f, -6.5f), new Vector3(7.2f, 0f, -5.5f) };
        public static readonly Vector3 WreckOffset = new Vector3(-10f, 0f, 3f);
        // Düşmanların çıktığı kıyı noktaları (ada şeklinin açısı, derece; 90 = kuzey ucu)
        public static readonly float[] EnemySpawnAngles = { 28f, 45f, 135f, 152f };

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
            new[] { new Vector2(0f, 4.4f), new Vector2(6.5f, 4.4f), new Vector2(6.5f, 10.5f) },      // meyve bahçesi
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

        // Yol çizgisine en yakın uzaklık (metre).
        public static float DistanceToPaths(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (Vector2[] line in Paths)
            {
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
