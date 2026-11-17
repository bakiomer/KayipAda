// TextUtil.cs
// Türkçe büyük harf: "Batık Koyu" → "BATIK KOYU", "Çit" → "ÇİT" (ToUpperInvariant ı/i harflerini yanlış çevirir).

namespace KayipAda
{
    public static class TextUtil
    {
        public static string UpperTr(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace('i', 'İ').Replace('ı', 'I').ToUpperInvariant();
        }
    }
}
