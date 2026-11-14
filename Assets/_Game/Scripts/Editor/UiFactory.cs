// UiFactory.cs
// Yazı (TextMeshPro) ve arayüz parçaları kurma yardımcıları.
// Tüm yazılar aynı kalın, kenar çizgili ve hafif gölgeli malzemeyi kullanır (okunaklı, "vitrin" görünümü).
// Türkçe harfler (ş, ğ, ı, İ) TMP'nin yedek (dinamik) fontuyla otomatik çizilir.

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class UiFactory
    {
        public const string OutlineMaterialPath = ArtGenerator.MaterialsFolder + "/TMP_Outline.mat";

        public static readonly Color TextDark = new Color(0.16f, 0.10f, 0.05f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.20f);

        private static TMP_FontAsset font;
        private static Material outline;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = BuilderUtil.Load<TMP_FontAsset>(ProjectSetup3D.TmpFontPath);
                return font;
            }
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
                mat.SetFloat("_FaceDilate", 0.15f);
                mat.SetFloat("_OutlineWidth", 0.22f);
                mat.SetColor("_OutlineColor", TextDark);
                mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.45f));
                mat.SetFloat("_UnderlayOffsetY", -0.7f);
                mat.SetFloat("_UnderlaySoftness", 0.3f);
                EditorUtility.SetDirty(mat);
                outline = mat;
                return outline;
            }
        }

        private static void Style(TMP_Text text, string value, float size, Color color)
        {
            if (Font != null) text.font = Font;
            if (OutlineMaterial != null) text.fontSharedMaterial = OutlineMaterial;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
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
