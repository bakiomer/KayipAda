// ThirdPartyImportProcessor.cs
// Assets/_Game/ThirdParty altındaki model, doku ve seslerin içe aktarma ayarlarını otomatik düzeltir:
// - Modeller: kamera ve ışıklar alınmaz, malzemeler model içinden okunur, collider eklenmez.
// - Quaternius karakterleri: Generic rig, avatar kendi modelinden; animasyonlar "Idle", "Walk", "Run"...
//   diye ayrılır ve yürüme/koşu/bekleme döngüye alınır. Diğer modellerde animasyon alınmaz.
// - Renk dokuları (Atlas, colormap): keskin (Point), sıkıştırmasız → renkler bozulmaz.
// - Sesler: mono, belleğe açılmış (kısa efektler için hızlı).
// Ayar değişirse Version artırılır; Unity bu klasördeki dosyaları kendiliğinden yeniden içe aktarır.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public class ThirdPartyImportProcessor : AssetPostprocessor
    {
        private const string Root = "Assets/_Game/ThirdParty/";
        private const uint Version = 4;

        // Döngüde çalması gereken animasyonlar.
        private static readonly HashSet<string> LoopClips = new HashSet<string>
        {
            "Idle", "Walk", "Run", "Jump_Idle", "Swim", "Swim_Fast", "Tentacle_Idle"
        };

        public override uint GetVersion() => Version;

        public static bool IsCharacter(string path)
        {
            return path.Replace('\\', '/').Contains("/Quaternius_Pirate/") && System.IO.Path.GetFileName(path).StartsWith("Characters_");
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            ModelImporter importer = (ModelImporter)assetImporter;

            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            if (IsCharacter(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.optimizeGameObjects = false; // taşıma pozu kol kemiklerine erişir
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root) || !IsCharacter(assetPath)) return;
            ModelImporter importer = (ModelImporter)assetImporter;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = ShortName(clip.name);
                clip.loopTime = LoopClips.Contains(clip.name);
                clip.loopPose = false;
            }
            importer.clipAnimations = clips;
        }

        // "CharacterArmature|Idle" → "Idle"
        public static string ShortName(string clipName)
        {
            int bar = clipName.LastIndexOf('|');
            return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            AudioImporter importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
