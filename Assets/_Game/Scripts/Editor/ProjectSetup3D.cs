// ProjectSetup3D.cs
// Projeyi 3D URP'ye geçiren ayarlar (sahne kurma komutu bunu kendisi çağırır; ayrıca menüden de çalışır):
// - Universal Renderer kullanan mobil URP ayar dosyası (Assets/_Game/Settings/URP_Mobile): MSAA 2x,
//   tek kaskad yumuşak (ucuz) gölge, ek ışık yok, HDR kapalı, derinlik/opak dokusu kapalı.
// - Bu dosyayı Graphics ve tüm Quality seviyelerine atar, renk uzayını Linear yapar.
// - TextMeshPro temel kaynaklarını (font, ayarlar) projeye ekler.

using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KayipAda.EditorTools
{
    public static class ProjectSetup3D
    {
        public const string SettingsFolder = BuilderUtil.Root + "/Settings";
        public const string PipelinePath = SettingsFolder + "/URP_Mobile.asset";
        public const string RendererPath = SettingsFolder + "/URP_Mobile_Renderer.asset";
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/Kayıp Ada/Kurulum/1 - 3D Proje Ayarları (URP)")]
        public static void RunFromMenu()
        {
            if (Run()) Debug.Log("Kayıp Ada: 3D proje ayarları tamam (URP_Mobile atandı).");
        }

        // Hazırsa true döner. TextMeshPro kaynakları yeni ekleniyorsa false (içe aktarma bitince tekrar çalıştırılır).
        public static bool Run()
        {
            BuilderUtil.EnsureFolder(SettingsFolder);
            UniversalRenderPipelineAsset pipeline = CreateOrUpdatePipeline();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();

            if (!File.Exists(TmpSettingsPath))
            {
                Debug.Log("Kayıp Ada: TextMeshPro temel kaynakları ekleniyor...");
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                return false;
            }
            return true;
        }

        private static UniversalRenderPipelineAsset CreateOrUpdatePipeline()
        {
            UniversalRendererData renderer = BuilderUtil.Load<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                // Varsayılan post-process verisi (URP'de "internal"; menüden oluşturulan dosyayla aynı sonuç).
                MethodInfo getDefault = typeof(PostProcessData).GetMethod("GetDefaultPostProcessData",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (getDefault != null) renderer.postProcessData = (PostProcessData)getDefault.Invoke(null, null);
                if (renderer.postProcessData == null)
                {
                    renderer.postProcessData = BuilderUtil.Load<PostProcessData>(
                        "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                }
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            renderer.renderingMode = RenderingMode.Forward;
            EditorUtility.SetDirty(renderer);

            UniversalRenderPipelineAsset pipeline = BuilderUtil.Load<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Mobil ayarları (her çalıştırmada uygulanır).
            pipeline.msaaSampleCount = 2;
            pipeline.supportsHDR = false;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 45f;
            pipeline.shadowCascadeCount = 1;
            pipeline.mainLightShadowmapResolution = 1024;

            SerializedObject so = new SerializedObject(pipeline);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", 1);               // Low: yumuşak ama ucuz
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);   // ek ışık yok
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            SetBool(so, "m_RequireDepthTexture", false);        // su derinlik dokusu kullanmaz
            SetBool(so, "m_RequireOpaqueTexture", false);
            SetBool(so, "m_UseSRPBatcher", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.boolValue = value;
            else Debug.LogWarning("Kayıp Ada: URP ayarı bulunamadı: " + name);
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                Debug.LogWarning("Kayıp Ada: URP ayarı bulunamadı: " + name);
                return;
            }
            p.intValue = value; // enum'larda da asıl sayı değeri yazılır
        }
    }
}
