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
                ModelCatalog.Sawmill, ModelCatalog.Workshop, ModelCatalog.RopeWorkshop, ModelCatalog.Loom, ModelCatalog.Barracks, ModelCatalog.Merchant, ModelCatalog.Tent,
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
            sb.AppendLine("Modern, 8-bit olmayan, hepsi CC0. Her sesin kaynağı, yazarı ve lisansı: **Docs/Lisanslar.md**. Adaylar:");
            sb.AppendLine("`Assets/_Game/ThirdParty/Audio/Adaylar/<ihtiyaç>/`; yedek: `ThirdParty/Audio/Claude_Uretim`. Toplama sesleri kısık ve");
            sb.AppendLine("3-4 çeşitli (perde ±%8), para ve kutlamalar öne çıkar. Arka plan müziği kutlamalarda kısılır, finalde zafer müziğine geçer.");
            sb.AppendLine();

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
