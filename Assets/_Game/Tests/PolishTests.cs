// PolishTests.cs
// Cila turu testleri: Türkçe harflerin yakın çekimi, sırttaki yığının sütunlu dizilişi ve "x40" etiketi,
// adadaki canlılar (yengeç oyuncudan kaçar), sesler (8-bit/jingle yok, müzik ve ortam bağlı), açma karesinde ad olmaması.

using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class PolishTests : GameTestBase
    {
        [UnityTest]
        public IEnumerator Font_TurkishLettersCloseUp()
        {
            TMP_Text sample = Object.FindAnyObjectByType<TextMeshProUGUI>();
            Assert.IsNotNull(sample, "Arayüz yazısı yok");
            StringAssert.Contains("Kayip", sample.font.name, "Oyun fontu kullanılmıyor");
            GameObject go = new GameObject("FontTest", typeof(RectTransform));
            go.transform.SetParent(sample.canvas.transform, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.font = sample.font;
            t.fontSharedMaterial = sample.fontSharedMaterial;
            t.text = "Ğğ Şş İı\nÇç Öö Üü\nKAYALIK  TAŞ ATÖLYESİ\nMeyve Bahçesi";
            t.fontSize = 120;
            t.alignment = TextAlignmentOptions.Center;
            ((RectTransform)go.transform).sizeDelta = new Vector2(1000f, 900f);
            yield return new WaitForSeconds(0.3f);
            foreach (char c in "ĞğŞşİı") Assert.IsTrue(t.font.HasCharacter(c), "Fontta yok: " + c);
            Shot("F_font_yakin");
            Object.Destroy(go);

            // Bölge tabelası yakından (Kayalık tabelası kapının yanında).
            Teleport(new Vector3(-3.8f, 0f, 3.2f));
            yield return new WaitForSeconds(1.5f);
            Shot("F_tabela_yakin");
        }

        [UnityTest]
        public IEnumerator Stack_ColumnsAndCountLabel()
        {
            ResourceType plank = null;
            foreach (ResourceType r in Resources.FindObjectsOfTypeAll<ResourceType>()) if (r.Id == "plank") plank = r;
            Assert.IsNotNull(plank);
            Player.enabled = false; // PlayerController her karede kapasiteyi ayardan yeniler; test için kapat
            PlayerStack.Capacity = 40;
            for (int i = 0; i < 40; i++) PlayerStack.TryAdd(plank.Spawn(PlayerStack.transform.position + Vector3.up));
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(40, PlayerStack.Count);
            Assert.Greater(PlayerStack.VisibleCapacity, 3, "Yığın tek sütun");
            Assert.Less(PlayerStack.VisibleCapacity, 40, "Görünen sınır yok");
            int visible = 0;
            float top = 0f;
            foreach (ResourceItem item in Object.FindObjectsByType<ResourceItem>(FindObjectsSortMode.None))
            {
                if (item.Type != plank) continue;
                visible++;
                top = Mathf.Max(top, item.transform.position.y);
            }
            Assert.LessOrEqual(visible, PlayerStack.VisibleCapacity, "Sınırın üstündeki parçalar görünüyor");
            Assert.Less(top, 2.6f, "Yığın omuz hizasının çok üstüne çıkıyor");
            TMP_Text label = null;
            foreach (TMP_Text t in Player.GetComponentsInChildren<TMP_Text>(true)) if (t.name == "StackCount") label = t;
            Assert.IsNotNull(label, "Sayı etiketi yok");
            Assert.IsTrue(label.gameObject.activeInHierarchy, "Sayı etiketi görünmüyor");
            Assert.AreEqual("x40", label.text);
            Shot("S_yigin_x40");
            Player.enabled = true;
        }

        [UnityTest]
        public IEnumerator Life_CrabsFleeGullsAndButterfliesFly()
        {
            yield return new WaitForSeconds(0.5f);
            Crab[] crabs = Object.FindObjectsByType<Crab>(FindObjectsSortMode.None);
            Flyer[] flyers = Object.FindObjectsByType<Flyer>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(crabs.Length, 4, "Yengeç yok");
            Assert.GreaterOrEqual(flyers.Length, 4, "Martı/kelebek yok");
            Crab crab = crabs[0];
            Transform body = crab.transform.Find("Body");
            Assert.IsTrue(body.gameObject.activeSelf);
            Teleport(crab.transform.position + Vector3.back * 4.5f);
            yield return new WaitForSeconds(1f);
            Shot("L_yengec_sahil");
            Teleport(crab.transform.position + Vector3.right * 0.5f);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(body.gameObject.activeSelf, "Oyuncu yaklaşınca yengeç kuma gömülmedi");
            yield return new WaitForSeconds(5f);
            Assert.IsTrue(body.gameObject.activeSelf, "Yengeç başka yerde ortaya çıkmadı");
        }

        [UnityTest]
        public IEnumerator Sounds_ModernAndAssigned()
        {
            yield return null;
            SoundLibrary library = null;
            foreach (SoundLibrary l in Resources.FindObjectsOfTypeAll<SoundLibrary>()) library = l;
            Assert.IsNotNull(library, "Ses kütüphanesi yok");
            Assert.IsNotNull(library.Music, "Arka plan müziği bağlı değil");
            Assert.IsNotNull(library.Ambient, "Ortam sesi bağlı değil");
            SoundId[] needed = { SoundId.Chop, SoundId.Mine, SoundId.Pop, SoundId.Rustle, SoundId.Sell, SoundId.Unlock, SoundId.Whoosh,
                                 SoundId.StageComplete, SoundId.VictoryMusic, SoundId.Splash, SoundId.Firework, SoundId.Button, SoundId.Seagull };
            foreach (SoundId id in needed)
            {
                SoundLibrary.Entry e = library.Find(id);
                Assert.IsTrue(e != null && e.Clips.Length > 0, id + " sesi yok");
                foreach (AudioClip c in e.Clips) StringAssert.DoesNotContain("jingles_", c.name, id + " eski 8-bit jingle kullanıyor");
            }
            Assert.GreaterOrEqual(library.Find(SoundId.Sell).Clips.Length, 3, "Para sesinde 3 çeşit yok");
            Assert.GreaterOrEqual(library.Find(SoundId.Chop).Clips.Length, 3, "Odun sesinde 3 çeşit yok");
        }

        [UnityTest]
        public IEnumerator UnlockPad_ShowsOnlyPriceNoName()
        {
            yield return new WaitForSeconds(0.5f);
            foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (TMP_Text t in pad.GetComponentsInChildren<TMP_Text>(true))
                    Assert.AreNotEqual(pad.Area.DisplayName, t.text, pad.name + " karesinde bölge adı yazıyor (sadece tabelada olmalı)");
            }
        }
    }
}
