// ScreenshotUtil.cs
// Test ve otomatik kontrol için ekran görüntüsü alır: verilen kameradan 1080x1920 PNG.
// Ekran arayüzü (para paneli) de görünsün diye kayıt sırasında Canvas'lar geçici olarak kameraya bağlanır.
// Kayıt klasörü komut satırından "-shotsDir <klasör>" ile verilebilir; yoksa proje kökündeki Docs/Screenshots.

using System.IO;
using UnityEngine;

namespace KayipAda
{
    public static class ScreenshotUtil
    {
        public static string Folder
        {
            get
            {
                string[] args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-shotsDir") return args[i + 1];
                }
                return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "Screenshots");
            }
        }

        public static string Capture(Camera cam, string fileName, int width = 1080, int height = 1920)
        {
            Directory.CreateDirectory(Folder);
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;

            // Ekran arayüzünü bu kameraya bağla (yoksa ekran görüntüsünde görünmez).
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            RenderMode[] modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.05f;
            }
            Canvas.ForceUpdateCanvases();

            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = previous;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (modes[i] == RenderMode.ScreenSpaceOverlay) canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
            }

            string path = Path.Combine(Folder, fileName + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            rt.Release();
            Kill(tex);
            Kill(rt);
            Debug.Log("KAYIPADA ekran görüntüsü: " + path);
            return path;
        }

        // Adanın üstünden ya da eğik bakan geçici bir kamerayla görüntü (genel kontrol için).
        public static string CaptureOverview(string fileName, Vector3 position, Vector3 euler, float fov)
        {
            GameObject go = new GameObject("ScreenshotCamera");
            Camera cam = go.AddComponent<Camera>();
            Camera main = Camera.main;
            if (main != null)
            {
                cam.CopyFrom(main);
                cam.farClipPlane = 400f;
            }
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            cam.fieldOfView = fov;
            string path = Capture(cam, fileName);
            Kill(go);
            return path;
        }

        // Oyunda Destroy, editörde DestroyImmediate.
        private static void Kill(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
