// SaveTests.cs
// Kayıt sistemi testi: para, açılan alan, geliştirme ve gemi ilerlemesi kaydedilip sahne yeniden yüklenince geri geliyor mu?
// Cihazda zaten bir kayıt varsa test onu yedekler ve sonunda geri koyar (oyuncunun kaydı bozulmaz).

using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class SaveTests : GameTestBase
    {
        private string backup;

        private void BackupSave()
        {
            backup = SaveManager.FilePath + ".testbackup";
            if (File.Exists(SaveManager.FilePath)) File.Copy(SaveManager.FilePath, backup, true);
            SaveManager.Delete();
        }

        private void RestoreSave()
        {
            SaveManager.Delete();
            if (File.Exists(backup))
            {
                File.Copy(backup, SaveManager.FilePath, true);
                File.Delete(backup);
            }
            SaveManager.Enabled = false;
        }

        [UnityTest]
        public IEnumerator Save_RestoresProgressAfterReload()
        {
            BackupSave();
            try
            {
                SaveManager.Enabled = true;
                SceneManager.LoadScene("Prototype");
                yield return null;
                yield return null;

                AreaDefinition rocky = PadArea("Pad Rocky");
                CurrencyManager.Instance.SetCoins(777);
                AreaManager.Instance.Unlock(rocky);
                UpgradeType first = UpgradeManager.Instance.Upgrades[0];
                UpgradeManager.Instance.SetLevel(first, 2);
                ShipBuilder ship = ShipBuilder.Instance;
                ResourceType keelResource = ship.Config.Stages[0].Requirements[0].Resource;
                ship.Deliver(keelResource.Spawn(ship.Delivery.transform.position));
                ship.Deliver(keelResource.Spawn(ship.Delivery.transform.position));
                SaveManager.Instance.Save();
                Assert.IsTrue(File.Exists(SaveManager.FilePath), "Kayıt dosyası yazılmadı");

                SceneManager.LoadScene("Prototype");
                yield return null;
                yield return null;

                Assert.AreEqual(777, CurrencyManager.Instance.Coins, "Para geri gelmedi");
                Assert.IsTrue(AreaManager.Instance.IsUnlocked(rocky), "Açılan alan geri gelmedi");
                Assert.AreEqual(2, UpgradeManager.Instance.GetLevel(first), "Geliştirme seviyesi geri gelmedi");
                Assert.AreEqual(2, ShipBuilder.Instance.Delivered(keelResource), "Gemi ilerlemesi geri gelmedi");

                // Kaçış: kaptanlık XP'si kazanılır ve "Tekrar oyna"dan sonra kalır.
                int xp = SaveManager.Instance.CaptainXp;
                SaveManager.Instance.RegisterEscape();
                SaveManager.Instance.RestartRun();
                yield return null;
                yield return null;
                Assert.Greater(SaveManager.Instance.CaptainXp, xp, "Kaptanlık XP'si kalıcı değil");
                Assert.IsFalse(AreaManager.Instance.IsUnlocked(rocky), "Tekrar oyna ilerlemeyi sıfırlamadı");
                Assert.AreEqual(SaveManager.Instance.CaptainLevel, UpgradeManager.Instance.CaptainLevel, "Kaptanlık bonusu uygulanmadı");
            }
            finally
            {
                RestoreSave();
            }
        }
    }
}
