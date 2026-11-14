// BuilderUtil.cs
// Editör komutlarının ortak yardımcıları: klasör oluşturma, [SerializeField] alanlara değer atama
// (Inspector'da sürükle-bırak yapmak gibi), dosya yükleme, statik işaretleme.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class BuilderUtil
    {
        public const string Root = "Assets/_Game";
        public const string ThirdParty = Root + "/ThirdParty";

        // "Assets/_Game/Art/Generated" gibi bir yolu (eksik ara klasörlerle birlikte) oluşturur.
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        public static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // Var olan dosyanın üzerine yazar (varsa içeriği kopyalayarak GUID korunur), yoksa oluşturur.
        public static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset, true);
            return existing;
        }

        // Dokuyu PNG olarak yazar ve içe aktarır.
        public static void WritePng(Texture2D texture, string path)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public static void SetField(Object target, string fieldName, Object value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string fieldName, Object[] values)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string fieldName, float value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string fieldName, int value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string fieldName, bool value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string fieldName, string value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector2(Object target, string fieldName, Vector2 value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector3(Object target, string fieldName, Vector3 value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string fieldName, Color value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Kayıp Ada: {target.GetType().Name} içinde '{fieldName}' alanı bulunamadı.");
            }
            return property;
        }

        // Hiç hareket etmeyen süs nesnelerini statik işaretler (Unity bunları birleştirip ucuz çizer).
        public static void MarkStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        // Bütün alt nesnelerin gölge düşürmesini açar/kapatır.
        public static void SetShadows(GameObject go, bool cast)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        }
    }
}
