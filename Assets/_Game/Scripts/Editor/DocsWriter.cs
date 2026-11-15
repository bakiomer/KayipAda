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
            nodes.Add(ModelCatalog.FiberPalm);
            nodes.Add(ModelCatalog.Stump);
            nodes.Add(ModelCatalog.Rubble);
            Section(sb, "Kaynak noktaları", nodes.ToArray());
            Section(sb, "Binalar ve alanlar", new[]
            {
                ModelCatalog.Sawmill, ModelCatalog.Workshop, ModelCatalog.RopeWorkshop, ModelCatalog.Loom, ModelCatalog.MarketStall, ModelCatalog.MarketChest, ModelCatalog.Tent,
                ModelCatalog.Campfire, ModelCatalog.Fence, ModelCatalog.Dock, ModelCatalog.DockPole, ModelCatalog.ShipWreck,
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
            sb.AppendLine("- **Paytone One** (Google Fonts, SIL Open Font License 1.1 — ücretsiz, ticari kullanım serbest). " +
                          "Dosya: `Assets/_Game/Art/Fonts/PaytoneOne-Regular.ttf`, lisans: `Assets/_Game/Art/Fonts/OFL.txt`. " +
                          "Türkçe harflerin hepsi (ı, ğ, ş, ç, ö, ü, İ) fontta var.");
            sb.AppendLine();

            sb.AppendLine("## Sesler (`Assets/_Game/Data/SoundLibrary.asset`)");
            sb.AppendLine();
            sb.AppendLine("| Olay | Ses dosyaları (ThirdParty/Audio) |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Toplama | Interface/pluck_001, pluck_002 |");
            sb.AppendLine("| Vuruş (ağaç) | RPG/chop |");
            sb.AppendLine("| Vuruş (kaya) | Impact/impactMining_000..003 |");
            sb.AppendLine("| Satış | RPG/handleCoins, handleCoins2 |");
            sb.AppendLine("| Para akışı (açma karesi) | Interface/tick_001, tick_002, tick_004 |");
            sb.AppendLine("| Alan açıldı | Interface/confirmation_002 |");
            sb.AppendLine("| Buton | Interface/click_001 |");
            sb.AppendLine("| Adım | Impact/footstep_grass_000..004 |");
            sb.AppendLine("| Binaya bırakma | Impact/impactWood_light_000..002 |");
            sb.AppendLine("| Ürün çıktı | Impact/impactPlank_medium_000..001 |");
            sb.AppendLine("| Geliştirme alındı | Interface/maximize_006 |");
            sb.AppendLine("| Gemiye malzeme | Impact/impactWood_medium_000..002 |");
            sb.AppendLine("| Gemi aşaması tamamlandı | Interface/maximize_008 |");
            sb.AppendLine("| Gemi suya indi | Impact/impactSoft_heavy_000..001 |");
            sb.AppendLine("| Havai fişek | Impact/impactGlass_light_000..002 |");
            sb.AppendLine("| Kaçış | Interface/confirmation_004 |");
            sb.AppendLine("| Ortam (dalga) | Pakette yok → AudioManager üretir |");

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
