// UpgradeStat.cs
// Geliştirilebilen özellikler. Her birinin etkisi UpgradeType ayar dosyasındadır.

namespace KayipAda
{
    public enum UpgradeStat
    {
        PlayerCapacity,   // oyuncunun taşıma kapasitesi (+adet)
        PlayerSpeed,      // oyuncunun hızı (+%)
        WorkerSpeed,      // işçilerin hızı (+%)
        WorkerCapacity,   // işçilerin taşıma kapasitesi (+adet)
        BuildingSpeed     // binaların işleme hızı (+%)
    }
}
