// GameTestBase.cs
// Oynanış testlerinin ortak temeli: Prototype sahnesini yükler, oyuncuyu ışınlama, alan bulma, koşul bekleme
// ve ekran görüntüsü alma yardımcıları. Testler batchmode'da -runTests ile de çalışır.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public abstract class GameTestBase
    {
        protected PlayerController Player { get; private set; }
        protected CarryStack PlayerStack { get; private set; }

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            SaveManager.Enabled = false;          // testler kayıt okumaz/yazmaz, hep temiz başlar
            PlayerController.InputBlocked = false;
            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;
            Player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            PlayerStack = Player != null ? Player.GetComponent<CarryStack>() : null;
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        protected void Teleport(Vector3 position)
        {
            Rigidbody rb = Player.GetComponent<Rigidbody>();
            rb.position = position;
            Player.transform.position = position;
        }

        protected static T Find<T>(string objectName) where T : Component
        {
            foreach (T c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.gameObject.name == objectName) return c;
            }
            return null;
        }

        protected static T FindAny<T>() where T : Component
        {
            return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }

        protected static AreaDefinition PadArea(string padName)
        {
            UnlockPad pad = Find<UnlockPad>(padName);
            return pad != null ? pad.Area : null;
        }

        // Koşul gerçekleşene kadar bekler (oyun zamanıyla en fazla seconds saniye).
        protected static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float end = Time.time + seconds;
            while (!condition() && Time.time < end) yield return null;
        }

        protected static void Shot(string name)
        {
            if (Camera.main != null) ScreenshotUtil.Capture(Camera.main, ShotPrefix + "_" + name);
        }

        protected static string ShotPrefix
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-shotPrefix") return args[i + 1];
                }
                return "test";
            }
        }
    }
}
