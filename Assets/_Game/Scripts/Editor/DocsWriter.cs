// DocsWriter.cs
// Docs/Modeller.md dosyasını ModelCatalog'dan yeniden yazar: hangi nesne için hangi model seçildi,
// dosya projede var mı, içe aktarılabildi mi; ayrıca pakette olmayanlar ve seçilen sesler.
// Sahne kurma komutu her çalıştığında güncellenir.

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class DocsWriter
    {
        public static void Write(List<string> missing)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Kayıp Ada — Model ve Ses Seçimleri");
            sb.AppendLine();
            sb.AppendLine("> Bu dosya **Tools > Kayıp Ada > Prototip Sahnesini Kur** komutu tarafından yeniden yazılır.");
            sb.AppendLine("> Modeli değiştirmek için: `Assets/_Game/Scripts/Editor/ModelCatalog.cs` içindeki dosya adını değiştir, komutu çalıştır.");
            sb.AppendLine("> Ana stil **Quaternius Pirate** (low-poly). Kenney paketleri sadece Quaternius'ta olmayan eşyalar için.");
            sb.AppendLine();

            Section(sb, "Karakterler", new[] { ModelCatalog.Player, ModelCatalog.Lumberjack, ModelCatalog.Carrier, ModelCatalog.FiberWorker });
            Section(sb, "Kullanılmayan karakterler (prefab hazır, sahnede yok)", ModelCatalog.Enemies);
            Section(sb, "Taşınan kaynaklar ve para", new[] { ModelCatalog.Wood, ModelCatalog.Stone, ModelCatalog.Fruit, ModelCatalog.Plank, ModelCatalog.Brick, ModelCatalog.Fiber, ModelCatalog.Cloth, ModelCatalog.Coin });
            List<ModelSpec> nodes = new List<ModelSpec>(ModelCatalog.Palms);
            nodes.AddRange(ModelCatalog.Rocks);
            nodes.Add(ModelCatalog.FruitTree);
            nodes.Add(ModelCatalog.FlaxClump);
            nodes.Add(ModelCatalog.Stump);
            nodes.Add(ModelCatalog.Rubble);
            Section(sb, "Kaynak noktaları", nodes.ToArray());
            Section(sb, "Binalar ve alanlar", new[]
            {
                ModelCatalog.Sawmill, ModelCatalog.Workshop, ModelCatalog.RopeWorkshop, ModelCatalog.Loom, ModelCatalog.MarketStall, ModelCatalog.MarketChest, ModelCatalog.Tent,
                ModelCatalog.Campfire, ModelCatalog.Fence, ModelCatalog.Sign, ModelCatalog.ShipFlag, ModelCatalog.Dock, ModelCatalog.DockPole, ModelCatalog.ShipWreck,
                ModelCatalog.UpgradeBench, ModelCatalog.Ship
            });
            Section(sb, "Süsler", ModelCatalog.Decor);

            sb.AppendLine("## Bulunamayan / içe aktarılamayan modeller");
            sb.AppendLine();
            if (missing == null || missing.Count == 0) sb.AppendLine("Yok — tüm modeller yüklendi.");
            else foreach (string m in missing) sb.AppendLine("- " + m + " (yerine pembe kutu kondu)");
            sb.AppendLine();

            sb.AppendLine("## Paketlerde olmayanlar ve çözümü");
            sb.AppendLine();
            foreach (string note in ModelCatalog.NotInPacks) sb.AppendLine("- " + note);
            sb.AppendLine();
            sb.AppendLine("## Font");
            sb.AppendLine("- **Kayip Display** = Google Fonts'taki **Lilita One** (SIL Open Font License 1.1) + eklenen Türkçe harfler " +
                          "(İ, Ğ, ğ, Ş, ş; fontun kendi harf ve işaretlerinden birleştirildi). OFL gereği değiştirilmiş sürüm \"Lilita\" " +
                          "adını taşıyamaz. Dosya: `Assets/_Game/Art/Fonts/KayipDisplay-Regular.ttf`, lisans: `OFL.txt`, açıklama: `BENIOKU.txt`.");
            sb.AppendLine();
            sb.AppendLine("## Kodla üretilenler");
            sb.AppendLine("- Kaynak ikonları (`Art/Generated/Icon_*.png`, IconGenerator), meyve modeli (FruitMesh), omurga ve kaburgalar,");
            sb.AppendLine("  gemi parçaları (gövde/direkler/yelkenler Quaternius gemisinden bölünür: `Art/Generated/Ship_*.asset`), kızak,");
            sb.AppendLine("  pop ve vuuş sesleri (`Assets/_Game/Audio/Generated`), kuş ve dalga ortam sesleri (AudioManager).");
            sb.AppendLine();

            sb.AppendLine("## Sesler (`Assets/_Game/Data/SoundLibrary.asset`)");
            sb.AppendLine();
            sb.AppendLine("Toplama sesleri kısık ve 3-5 çeşitli (her çalışta perde ±%8 değişir); para ve kutlamalar öne çıkar.");
            sb.AppendLine();
            sb.AppendLine("| Olay | Ses dosyaları |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Odun (balta) | RPG/chop, Impact/impactWood_heavy_000/001/003 |");
            sb.AppendLine("| Taş (kazma) | Impact/impactMining_000..002, impactMetal_light_000/002 |");
            sb.AppendLine("| Meyve (pop) | Audio/Generated/pop_1..4 (kodla üretildi) |");
            sb.AppendLine("| Lif (hışırtı) | RPG/cloth1..4 |");
            sb.AppendLine("| Toplama | Interface/pluck_001, pluck_002 |");
            sb.AppendLine("| Satış (para) | RPG/handleCoins, handleCoins2 |");
            sb.AppendLine("| Para akışı (açma karesi) | Interface/tick_001, tick_002, tick_004 |");
            sb.AppendLine("| Alan açıldı | Audio/Generated/whoosh_1..2 + Impact/impactBell_heavy_000/002 + Jingles/STEEL01/04/05 |");
            sb.AppendLine("| Çit kapısı | RPG/creak1..3 |");
            sb.AppendLine("| Buton | Interface/click_001, click_002 |");
            sb.AppendLine("| Adım | Impact/footstep_grass_000..003 |");
            sb.AppendLine("| Binaya bırakma / ürün | Impact/impactWood_light_000..002 / impactPlank_medium_000..001 |");
            sb.AppendLine("| Geliştirme, yıldız | Interface/maximize_006 |");
            sb.AppendLine("| Gemiye malzeme / parça oturdu | Impact/impactWood_medium_000..002 / impactWood_heavy_002/004, impactPlank_medium_002 |");
            sb.AppendLine("| Gemi aşaması | Jingles/STEEL07, STEEL02 |");
            sb.AppendLine("| Final zafer müziği (sırayla) | Jingles/STEEL06, 03, 14, 12, PIZZI07, STEEL07, 02 |");
            sb.AppendLine("| Gemi suya indi / havai fişek | Impact/impactSoft_heavy_000..001 / impactGlass_light_000..002 |");
            sb.AppendLine("| Kaçış ekranı | Jingles/SAX07 |");
            sb.AppendLine("| Ortam (dalga + kuşlar) | Pakette yok → AudioManager üretir |");
            sb.AppendLine();
            sb.AppendLine("Jingles: Kenney \"Music Jingles\" (CC0), `Assets/_Game/ThirdParty/Audio/Jingles` (+ License.txt). " +
                          "Uzun CC0 arka plan müziği bulunamadı; oyun içinde dalga + kuş ortam sesi çalar.");

            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Modeller.md"), sb.ToString(), new UTF8Encoding(false));
        }

        private static void Section(StringBuilder sb, string title, ModelSpec[] specs)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            sb.AppendLine("| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (ModelSpec s in specs)
            {
                bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(s.Path) != null;
                string axis = s.Axis == SizeAxis.Height ? "yükseklik" : "en büyük kenar";
                sb.AppendLine($"| {s.Note} | {s.Key} | {s.Pack} | {s.File}.fbx | {s.Size:0.##} m ({axis}) | {(exists ? "✅" : "❌ bulunamadı")} |");
            }
            sb.AppendLine();
        }
    }
}
