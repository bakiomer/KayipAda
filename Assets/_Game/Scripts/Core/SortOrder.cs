// SortOrder.cs
// Sprite'ların çizim sırası (hangisi önde görünür) tek bir yerde.
// Büyük sayı = daha önde. Yeni bir nesne türü eklenince sırası buraya yazılır.

namespace KayipAda
{
    public static class SortOrder
    {
        public const int Ground = 0;
        public const int Zone = 1;
        public const int NodeBase = 2;
        public const int NodeTop = 3;
        public const int WorldLabel = 5;
        public const int Character = 10;
        public const int CarriedItem = 20; // + yığındaki sırası
    }
}
