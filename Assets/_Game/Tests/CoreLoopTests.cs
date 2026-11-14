// CoreLoopTests.cs
// Temel döngü testleri: odun toplama, satış, açma karesi, bina işleme, işçilerin kendi kendine para kazandırması.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class CoreLoopTests : GameTestBase
    {
        private ResourceNode FirstPalm()
        {
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (node.isActiveAndEnabled && node.name.StartsWith("Node_Palm")) return node;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Player_CollectsWoodFromTree()
        {
            ResourceNode palm = FirstPalm();
            Assert.IsNotNull(palm, "Palmiye bulunamadı");
            Teleport(palm.GetStandPoint(palm.transform.position + Vector3.back * 3f));
            yield return WaitFor(() => PlayerStack.Count >= 3, 6f);
            Assert.GreaterOrEqual(PlayerStack.Count, 3, "Oyuncu ağaçtan odun toplamadı");
        }

        [UnityTest]
        public IEnumerator Market_SellingIncreasesCoins()
        {
            ResourceNode palm = FirstPalm();
            Teleport(palm.GetStandPoint(palm.transform.position + Vector3.back * 3f));
            yield return WaitFor(() => PlayerStack.Count >= 3, 6f);
            Assert.Greater(PlayerStack.Count, 0, "Satılacak odun toplanamadı");

            int before = CurrencyManager.Instance.Coins;
            Teleport(FindAny<Market>().transform.position);
            yield return WaitFor(() => PlayerStack.IsEmpty && CurrencyManager.Instance.Coins > before, 8f);
            Assert.Greater(CurrencyManager.Instance.Coins, before, "Pazarda satınca para artmadı");
            Assert.IsTrue(PlayerStack.IsEmpty, "Yığın boşalmadı");
        }

        [UnityTest]
        public IEnumerator UnlockPad_TakesCoinsAndOpensArea()
        {
            UnlockPad pad = Find<UnlockPad>("Pad Rocky");
            Assert.IsNotNull(pad, "Kayalık karesi bulunamadı");
            CurrencyManager.Instance.Add(AreaManager.Instance.PriceOf(pad.Area) + 50);
            int before = CurrencyManager.Instance.Coins;

            Teleport(pad.transform.position);
            yield return WaitFor(() => AreaManager.Instance.IsUnlocked(pad.Area), 8f);
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(pad.Area), "Kare ödendi ama alan açılmadı");
            Assert.AreEqual(before - AreaManager.Instance.PriceOf(pad.Area), CurrencyManager.Instance.Coins, "Kare yanlış miktarda para aldı");
            yield return new WaitForSeconds(1f);
            Shot("A_kayalik_acildi");
        }

        [UnityTest]
        public IEnumerator Building_ProcessesRawMaterial()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return null;
            ProcessingBuilding sawmill = null;
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
            {
                if (b.isActiveAndEnabled && b.Type.Input.Id == "wood") sawmill = b;
            }
            Assert.IsNotNull(sawmill, "Bıçkıhane açılmadı");

            for (int i = 0; i < 4; i++) sawmill.InputPile.TryAdd(sawmill.Type.Input.Spawn(sawmill.transform.position + Vector3.up));
            yield return WaitFor(() => sawmill.OutputPile.Count >= 1, sawmill.Type.ProcessTime * 2f + 2f);
            Assert.GreaterOrEqual(sawmill.OutputPile.Count, 1, "Bina odunu kalasa çevirmedi");
        }

        [UnityTest]
        public IEnumerator Workers_EarnMoneyOnTheirOwn()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            AreaManager.Instance.Unlock(PadArea("Pad Lumberjack"));
            AreaManager.Instance.Unlock(PadArea("Pad Carrier"));
            Teleport(new Vector3(-3f, 0f, 3f)); // oyuncu kenarda dursun, hiçbir alana girmesin
            int before = CurrencyManager.Instance.Coins;

            Time.timeScale = 4f;
            float end = Time.time + 150f, nextLog = 0f;
            while (CurrencyManager.Instance.Coins <= before && Time.time < end)
            {
                if (Time.time >= nextLog)
                {
                    nextLog = Time.time + 10f;
                    LogWorkers();
                }
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.Greater(CurrencyManager.Instance.Coins, before, "İşçiler kendi kendine para kazandırmadı");
            Shot("A_isciler_calisiyor");
        }

        // Tanılama: işçilerin durumu, konumu ve yığını; binaların yığınları.
        private static void LogWorkers()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("KAYIPADA işçiler t=" + Time.time.ToString("0") + ": ");
            foreach (Worker w in Object.FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                object state = w.GetType().GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(w);
                CarryStack st = w.GetComponent<CarryStack>();
                sb.Append(w.name).Append('[').Append(state).Append(" pos=").Append(w.transform.position.ToString("0.0"))
                  .Append(" yük=").Append(st.Count).Append('/').Append(st.Capacity)
                  .Append(" hedef=").Append(st.WorkTarget != null ? st.WorkTarget.name : "-").Append("] ");
            }
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
            {
                sb.Append(b.Type.DisplayName).Append(" giriş=").Append(b.InputPile.Count).Append(" çıkış=").Append(b.OutputPile.Count).Append(' ');
            }
            Debug.Log(sb.ToString());
        }
    }
}
