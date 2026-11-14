// FloatingText.cs
// Dünyada beliren, yükselip kaybolan kısa yazı ("+5" gibi). Effects yöneticisi bunları havuzdan kullanır:
// bitince yok edilmez, gizlenip tekrar kullanılır.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;
        [Tooltip("Yazının ömrü boyunca yükseleceği mesafe (metre).")]
        [SerializeField] private float rise = 1.4f;

        public bool IsBusy => gameObject.activeSelf;

        public void Play(Vector3 position, string text, Color color, float duration)
        {
            gameObject.SetActive(true);
            transform.position = position;
            label.text = text;
            label.color = color;

            Vector3 start = position;
            Tween.Value(this, duration, t =>
            {
                transform.position = start + Vector3.up * (rise * Tween.EaseOutQuad(t));
                float pop = t < 0.2f ? Tween.EaseOutBack(t / 0.2f) : 1f;
                transform.localScale = Vector3.one * pop;
                Color c = color;
                c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                label.color = c;
            }, () => gameObject.SetActive(false));
        }
    }
}
