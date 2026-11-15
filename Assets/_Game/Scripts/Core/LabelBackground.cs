// LabelBackground.cs
// Dünyadaki yazının (bina adı, gemi aşaması, bölge adı) arkasındaki yarı saydam, yuvarlak köşeli koyu kutu.
// Yazı değişince kutu yazının boyuna göre yeniden boyutlanır; yazı boşsa kutu gizlenir.
// Yazıyla birlikte döner (Billboard yazının üstündedir), böylece her zaman kameraya dönük ve okunur durur.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class LabelBackground : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [Tooltip("Yazının çevresinde bırakılan boşluk (metre).")]
        [SerializeField] private Vector2 padding = new Vector2(0.3f, 0.14f);

        private SpriteRenderer box;
        private string shown;

        private void Awake()
        {
            box = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (text == null) return;
            bool visible = text.enabled && !string.IsNullOrEmpty(text.text) && text.color.a > 0.01f;
            if (box.enabled != visible) box.enabled = visible;
            if (!visible || ReferenceEquals(text.text, shown)) return;

            shown = text.text;
            Vector2 size = text.GetPreferredValues(shown);
            box.size = size + padding * 2f;
        }
    }
}
