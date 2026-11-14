// BatchTools.cs
// Komut satırından (batchmode) çalıştırılan araçlar. Örnek:
//   Unity.exe -batchmode -projectPath <proje> -executeMethod KayipAda.EditorTools.BatchTools.BuildScene -quit
// - BuildScene: Tools > Kayıp Ada > Prototip Sahnesini Kur ile aynı işi soru sormadan yapar.
// - CaptureEditShots: sahneyi açıp oyun kamerasından ve yukarıdan PNG ekran görüntüleri alır
//   (isim öneki "-shotPrefix <ad>" ile verilir).
// Hata olursa Unity'yi 1 koduyla kapatır ki betik anlayabilsin.

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class BatchTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Prototype.unity";

        public static void BuildScene()
        {
            try
            {
                if (!ProjectSetup3D.Run())
                {
                    Debug.LogError("KAYIPADA: TextMeshPro kaynakları eksikti, eklendi. Komutu tekrar çalıştırın.");
                    EditorApplication.Exit(2);
                    return;
                }
                PrototypeSceneBuilder.BuildNow();
                Debug.Log("KAYIPADA: BuildScene tamam");
            }
            catch (Exception e)
            {
                Debug.LogError("KAYIPADA: BuildScene hata: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureEditShots()
        {
            try
            {
                string prefix = Arg("-shotPrefix", "edit");
                EditorSceneManager.OpenScene(ScenePath);
                Camera main = Camera.main;
                if (main != null) ScreenshotUtil.Capture(main, prefix + "_oyunKamerasi");
                ScreenshotUtil.CaptureOverview(prefix + "_yukaridan", new Vector3(0f, 95f, 3f), new Vector3(90f, 0f, 0f), 40f);
                ScreenshotUtil.CaptureOverview(prefix + "_egik", new Vector3(0f, 45f, -42f), new Vector3(42f, 0f, 0f), 45f);
                Debug.Log("KAYIPADA: CaptureEditShots tamam");
            }
            catch (Exception e)
            {
                Debug.LogError("KAYIPADA: CaptureEditShots hata: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return fallback;
        }
    }
}
