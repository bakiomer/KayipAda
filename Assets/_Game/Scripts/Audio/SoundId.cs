// SoundId.cs
// Oyundaki ses türleri. Her türün sesleri SoundLibrary ayar dosyasında seçilir.
// Yeni bir ses türü eklenince buraya ve SoundLibrary'ye (sahne kurma komutu) eklenir.

namespace KayipAda
{
    public enum SoundId
    {
        Collect,   // kaynak sırta eklendi
        Chop,      // ağaca/kayaya vurma (toplama anı)
        Sell,      // pazarda satış
        Coin,      // açma karesine para akışı
        Unlock,    // alan/bina/işçi açıldı
        Button,    // arayüz tuşu
        Footstep,  // adım
        Drop,      // bina girişine bırakma
        Produce,   // bina ürün çıkardı
        Upgrade,   // geliştirme satın alındı
        Build,     // gemiye malzeme teslim edildi
        CannonFire,// top ateşlendi
        Explosion, // gülle patladı
        EnemyDie,  // düşman öldü
        ShipHit,   // gemi hasar aldı
        NightStart,// son gece başladı
        Victory,   // kaçış
        Mine       // kayaya vurma
    }
}
