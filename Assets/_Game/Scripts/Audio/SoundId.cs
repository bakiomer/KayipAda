// SoundId.cs
// Oyundaki ses türleri. Her türün sesleri SoundLibrary ayar dosyasında seçilir.
// Sayılar SABİTTİR (ses kütüphanesi dosyası bunları sayı olarak saklar): yeni tür eklerken yeni bir sayı ver,
// silinen bir türün sayısını tekrar kullanma.

namespace KayipAda
{
    public enum SoundId
    {
        Collect = 0,        // kaynak sırta eklendi
        Chop = 1,           // ağaca vurma
        Sell = 2,           // pazarda satış
        Coin = 3,           // açma karesine para akışı
        Unlock = 4,         // alan/bina/işçi açıldı
        Button = 5,         // arayüz tuşu
        Footstep = 6,       // adım
        Drop = 7,           // bina girişine bırakma
        Produce = 8,        // bina ürün çıkardı
        Upgrade = 9,        // geliştirme satın alındı
        Build = 10,         // gemiye malzeme teslim edildi
        // 11-15: eski savunma sesleri (kaldırıldı, sayıları tekrar kullanma)
        Victory = 16,       // kaçış
        Mine = 17,          // kayaya vurma
        StageComplete = 18, // gemi aşaması tamamlandı
        Splash = 19,        // gemi suya indi
        Firework = 20,      // havai fişek
        Pop = 21,           // meyve toplama
        Rustle = 22,        // lif toplama (hışırtı)
        UnlockJingle = 23,  // alan/bina açılınca kısa müzik
        GateOpen = 24,      // çit kapısı açılıyor (gıcırtı)
        Whoosh = 25,        // alan açma "vuuş"
        VictoryMusic = 26,  // final töreni müziği (sesler sırayla çalınır)
        PartPlace = 27,     // gemi parçası yerine oturdu
        Seagull = 28,       // uzaktan martı sesi
        ShipSlide = 29,     // gemi kızaktan kayıyor (ahşap gıcırtı + sürtünme)
        MachineCreak = 30,  // halat çarkı turu (ahşap gıcırtı)
        MachineFriction = 31, // halat çarkında lif sürtünmesi
        LoomTick = 32,      // dokuma tezgahında mekik "tık-tak"
        Discovery = 33,     // bulut dağıldı / yeni bölge keşfedildi
        ChestOpen = 34,     // sandık açıldı
        Anvil = 35,         // demirci örsü (çekiç)
        QuestDone = 36      // yan görev tamamlandı
    }
}
