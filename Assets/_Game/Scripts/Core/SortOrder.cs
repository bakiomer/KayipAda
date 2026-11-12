// SortOrder.cs
// Sprite'ların çizim sırası (hangisi önde görünür) tek bir yerde.
// Büyük sayı = daha önde. Yeni bir nesne türü eklenince sırası buraya yazılır.

namespace KayipAda
{
    public static class SortOrder
    {
        public const int Ground = 0;        // ada
        public const int GroundPatch = 1;   // bölge zeminleri, iskele
        public const int Zone = 2;          // Pazar, açma kareleri
        public const int Fence = 3;         // kapalı bölge çitleri
        public const int NodeBase = 4;      // kaynak noktası alt parçası
        public const int NodeTop = 5;       // kaynak noktası üst parçası
        public const int WorldLabel = 8;    // dünyadaki yazılar
        public const int Character = 10;    // oyuncu (ileride işçiler)
        public const int CarriedItem = 20;  // + yığındaki sırası
        public const int Effect = 50;       // uçan para vb.
    }
}
