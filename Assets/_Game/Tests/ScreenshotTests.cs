// ScreenshotTests.cs
// Görsel kontrol için oyun içinden ekran görüntüleri alır (Docs/Screenshots, ad öneki -shotPrefix).
// Başlangıç, kaynak taşıma, her şey açık hali ve yukarıdan/eğik genel görünüm.

using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class ScreenshotTests : GameTestBase
    {
        [UnityTest]
        public IEnumerator Capture_GameViews()
        {
            yield return new WaitForSeconds(1f);
            Shot("1_baslangic");

            // Odun topla, yürürken yığını göster.
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (!node.name.StartsWith("Node_Palm")) continue;
                Teleport(node.GetStandPoint(node.transform.position + Vector3.back * 3f));
                break;
            }
            yield return WaitFor(() => PlayerStack.IsFull, 8f);
            yield return new WaitForSeconds(0.6f);
            Shot("2_odun_tasiyor");

            // Her şeyi aç.
            foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                AreaManager.Instance.Unlock(pad.Area);
            }
            yield return new WaitForSeconds(2.5f);
            Teleport(new Vector3(0f, 0f, 2f));
            yield return new WaitForSeconds(1.5f);
            Shot("3_hepsi_acik_orta");
            Teleport(new Vector3(-6f, 0f, 9f));
            yield return new WaitForSeconds(1.5f);
            Shot("4_kayalik");
            Teleport(new Vector3(6f, 0f, 9f));
            yield return new WaitForSeconds(1.5f);
            Shot("5_bahce");
            Teleport(new Vector3(0f, 0f, 18f));
            yield return new WaitForSeconds(1.5f);
            Shot("6_iskele");

            ScreenshotUtil.CaptureOverview(ShotPrefix + "_7_yukaridan", new Vector3(0f, 95f, 3f), new Vector3(90f, 0f, 0f), 40f);
            ScreenshotUtil.CaptureOverview(ShotPrefix + "_8_egik", new Vector3(0f, 45f, -42f), new Vector3(42f, 0f, 0f), 45f);
        }
    }
}
