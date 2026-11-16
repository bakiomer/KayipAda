// UiFactory.cs
// Yazı (TextMeshPro) ve arayüz parçaları kurma yardımcıları.
// Tüm yazılar oyunun fontunu (Kayip Display = Lilita One + Türkçe harfler, OFL, Art/Fonts) ve aynı malzemeyi
// kullanır: krem dolgu, kalın koyu kahverengi kenar çizgisi, hafif alt gölge. Font asset'i yoksa TTF'ten üretilir;
// Türkçe harfler dahil gereken tüm karakterler atlasa önceden yazılır (oyunda kaynak font gerekmez).
// Dünyadaki yazılarda kutu yok; uzaktayken ve ekran kenarına yaklaşınca yumuşakça kaybolurlar (LabelFader).

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class UiFactory
    {
        public const string OutlineMaterialPath = ArtGenerator.MaterialsFolder + "/TMP_Outline.mat";
        public const string GameFontPath = BuilderUtil.Root + "/Art/Fonts/KayipDisplay-Regular.ttf";
        public const string GameFontAssetPath = BuilderUtil.Root + "/Art/Fonts/KayipDisplay SDF.asset";

        // Atlasa önceden yazılan karakterler: ASCII + Türkçe + oyunda kullanılan birkaç işaret.
        private const string FontCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÇçĞğİıÖöŞşÜüÂâÎîÛûÉé•…×";



        public static readonly Color TextDark = new Color(0.24f, 0.12f, 0.05f);   // koyu kahverengi kenar çizgisi
        public static readonly Color Cream = new Color(1f, 0.97f, 0.88f);         // yazı dolgusu
        public static readonly Color Gold = new Color(1f, 0.80f, 0.20f);

        private static TMP_FontAsset font;
        private static Material outline;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = BuilderUtil.Load<TMP_FontAsset>(GameFontAssetPath);
                if (font == null) font = CreateGameFont();
                if (font == null) font = BuilderUtil.Load<TMP_FontAsset>(ProjectSetup3D.TmpFontPath);
                return font;
            }
        }

        // TTF'ten TextMeshPro font asset'i üretir (sadece yoksa). Atlas ve malzeme asset'in içine kaydedilir.
        private static TMP_FontAsset CreateGameFont()
        {
            Font source = BuilderUtil.Load<Font>(GameFontPath);
            if (source == null)
            {
                Debug.LogWarning("Kayıp Ada: Oyun fontu bulunamadı: " + GameFontPath);
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024,
                                                                AtlasPopulationMode.Dynamic, true);
            asset.name = "KayipDisplay SDF";
            AssetDatabase.CreateAsset(asset, GameFontAssetPath);
            asset.atlasTextures[0].name = "KayipDisplay Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = "KayipDisplay Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            if (!asset.TryAddCharacters(FontCharacters, out string missing))
                Debug.LogWarning("Kayıp Ada: Fontta olmayan karakterler (yedek fonttan çizilir): " + missing);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            TMP_FontAsset fallback = BuilderUtil.Load<TMP_FontAsset>(ProjectSetup3D.TmpFontPath);
            if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(asset);
            EditorUtility.SetDirty(asset.atlasTextures[0]);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // Kenar çizgili + alt gölgeli yazı malzemesi (fontun malzemesinin kopyası).
        public static Material OutlineMaterial
        {
            get
            {
                if (outline != null) return outline;
                if (Font == null) return null;

                BuilderUtil.EnsureFolder(ArtGenerator.MaterialsFolder);
                Material mat = BuilderUtil.Load<Material>(OutlineMaterialPath);
                if (mat == null)
                {
                    mat = new Material(Font.material);
                    AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
                }
                else
                {
                    mat.CopyPropertiesFromMaterial(Font.material);
                    mat.shader = Font.material.shader;
                }
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                mat.SetFloat("_FaceDilate", 0.12f);
                mat.SetFloat("_OutlineWidth", 0.32f);
                mat.SetFloat("_OutlineSoftness", 0f);
                mat.SetFloat("_Sharpness", 0.35f); // uzak/küçük yazılar bulanık görünmesin
                mat.SetColor("_OutlineColor", TextDark);
                mat.SetColor("_UnderlayColor", new Color(0.12f, 0.06f, 0.02f, 0.5f));
                mat.SetFloat("_UnderlayOffsetX", 0.15f);
                mat.SetFloat("_UnderlayOffsetY", -0.75f);
                mat.SetFloat("_UnderlayDilate", 0.32f);
                mat.SetFloat("_UnderlaySoftness", 0.25f);
                EditorUtility.SetDirty(mat);
                outline = mat;
                return outline;
            }
        }

        private static void Style(TMP_Text text, string value, float size, Color color)
        {
            if (Font != null) text.font = Font;
            if (OutlineMaterial != null) text.fontSharedMaterial = OutlineMaterial;
            if (color == Color.white) color = Cream; // beyaz yerine sıcak krem dolgu
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal; // font zaten kalın; TMP'nin sahte kalınlığı harfleri bozar
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        // Dünyada duran 3D yazı (Canvas'sız). size: font boyutu (dünya ölçeğinde ~1 = 10 cm).
        public static TextMeshPro CreateWorldText(string name, Transform parent, Vector3 localPos, string value, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMeshPro text = go.AddComponent<TextMeshPro>();
            Style(text, value, size, color);
            ((RectTransform)go.transform).sizeDelta = new Vector2(8f, 2f);
            text.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return text;
        }

        // Dünyada kameraya dönük yazı (bina adı, gemi aşaması). Kutu yok; uzaktayken ve ekran kenarına yaklaşınca
        // yumuşakça kaybolur, yaklaşınca belirir (LabelFader).
        public static TextMeshPro CreateWorldLabel(string name, Transform parent, Vector3 localPos, string value, float size, Color color)
        {
            TextMeshPro text = CreateWorldText(name, parent, localPos, value, size, color);
            text.gameObject.AddComponent<Billboard>();
            AddFader(text.gameObject);
            return text;
        }

        // Nesnenin altındaki tüm yazı ve resimleri uzaklığa/ekran kenarına göre yumuşakça gösterir/gizler.
        public static LabelFader AddFader(GameObject go)
        {
            LabelFader fader = go.GetComponent<LabelFader>();
            if (fader == null) fader = go.AddComponent<LabelFader>();
            return fader;
        }

        // Canvas içinde yazı.
        public static TextMeshProUGUI CreateUIText(string name, Transform parent, string value, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            Style(text, value, size, color);
            return text;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Dünyada duran Canvas. sizePx: UI birimi; 100 UI birimi = 1 metre.
        public static Canvas CreateWorldCanvas(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector2 sizePx)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * 0.01f;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = sizePx;
            return canvas;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Size(Graphic graphic, Vector2 anchoredPos, Vector2 size)
        {
            RectTransform rt = graphic.rectTransform;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
