# Gece Raporu

> Bu dosya gece boyunca güncellendi; en sondaki hali sabah özetidir.

## Gece boyunca aldığım kararlar (notlar)

1. **Unity Editor açıktı.** İstemde kapalı dendiği halde proje Unity'de açıktı. Editörü kapatmama izin verilmedi; açık editöre dokunmadım.
   Onun yerine projenin bir kopyasını `C:\Oyunlar\KayipAda\_BatchCopy` klasörüne aldım (Git'e eklenmez, `.gitignore`'da).
   Bütün batchmode derleme, sahne kurma, test ve ekran görüntüsü işleri bu kopyada yapıldı; kaynak kod asıl projede yazıldı ve her testten önce kopyaya eşitlendi.
   İlk denemede kopya geçici klasördeydi; Windows'un 260 karakter yol sınırı yüzünden paket dosyaları okunamadı, kısa yola taşıdım.
2. **Testler için derleme tanımları (asmdef) eklendi:** `KayipAda` (oyun), `KayipAda.Editor` (editör), `KayipAda.Tests` (testler). Unity'nin testlerin oyun koduna erişebilmesi için gerekli.
3. **Quaternius gemi modeli tek parça.** Gemiyi "parça parça" gösterebilmek için aşağıdan yukarıya beliren özel bir shader yazdım (`BuildReveal`): teslim edilen her malzemeyle gemi biraz daha yükselir, kesilen kenarda sıcak bir parlama şeridi var. Erzak aşamasında güvertede variller ve sandıklar tek tek zıplayarak çıkıyor; gemi bitince inşa direkleri kayboluyor.
4. **"Gövde (kalas + taş)"** aşamasındaki taşı **tuğla** olarak yorumladım. Taşıyıcı işçiler sadece bina ürünlerini taşıyabiliyor; böylece işçiler de gemiye malzeme getirebiliyor ve Taş Atölyesi anlamlı oluyor. Ham taş ise toplara gülle olarak kullanılıyor.
5. **Kayıt sistemi (Bölüm D)** kaçış ekranındaki "geçen süre" ve "kaptanlık XP'si" için gerekli olduğundan Bölüm C ile birlikte yazıldı.
6. Denge için fiyatlar ve gemi malzemeleri artırıldı (ayrıntı: `Docs/Denge.md`).
