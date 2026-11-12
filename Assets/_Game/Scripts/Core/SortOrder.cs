// SortOrder.cs
// Sprite'ların çizim sırası (hangisi önde görünür) tek bir yerde.
// Büyük sayı = daha önde. Yeni bir nesne türü eklenince sırası buraya yazılır.

namespace KayipAda
{
    public static class SortOrder
    {
        public const int Ground = 0;        // ada
        public const int GroundPatch = 1;   // bölge zeminleri, iskele
        public const int Footprint = 2;     // kapalı binanın silik zemini
        public const int Zone = 3;          // Pazar, açma kareleri, bina giriş/çıkış alanları
        public const int Fence = 4;         // kapalı bölge çitleri
        public const int NodeBase = 5;      // kaynak noktası alt parçası
        public const int NodeTop = 6;       // kaynak noktası üst parçası
        public const int Building = 7;      // bina gövdesi
        public const int BuildingTop = 8;   // bina çatısı / detay
        public const int WorldLabel = 9;    // dünyadaki yazılar
        public const int PileItem = 10;     // + yerdeki yığındaki sırası (en fazla ~20 parça)
        public const int BarBack = 35;      // ilerleme çubuğu arka planı
        public const int BarFill = 36;      // ilerleme çubuğu dolan kısım
        public const int Character = 40;    // oyuncu (ileride işçiler)
        public const int CarriedItem = 50;  // + sırttaki yığındaki sırası
        public const int Effect = 100;      // uçan para vb.
    }
}
