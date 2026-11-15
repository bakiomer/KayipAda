// FloatingText.cs
// Dünyada beliren, yükselip kaybolan kısa yazı ("+5", "Seviye 4!"). Effects yöneticisi bunları havuzdan kullanır.
// Üst üste binmesinler diye: aynı yazı aynı yere tekrar gelirse yenisi açılmaz, bu yazı baştan oynar (Restart);
// farklı bir yazı gelirse yakındaki eski yazılar biraz daha yukarı kayar (Bump).

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;
        [Tooltip("Yazının ömrü boyunca yükseleceği mesafe (metre).")]
        [SerializeField] private float rise = 1.4f;

        private Vector3 anchor;
        private Color color;
        private float duration;
        private float age;
        private float lift;
        private float liftTarget;

        public bool IsBusy => gameObject.activeSelf;
        public string Text => label.text;
        public Vector3 Anchor => anchor;

        public void Play(Vector3 position, string text, Color textColor, float seconds)
        {
            gameObject.SetActive(true);
            anchor = position;
            label.text = text;
            color = textColor;
            duration = seconds;
            age = 0f;
            lift = liftTarget = 0f;
            UpdateVisual();
        }

        // Aynı yazı tekrar geldi: baştan zıplayarak oynar (yeni bir kopya açılmaz).
        public void Restart()
        {
            age = 0f;
            UpdateVisual();
        }

        // Yeni bir yazı altına geldi: biraz daha yukarı kay.
        public void Bump(float amount)
        {
            liftTarget += amount;
        }

        private void Update()
        {
            age += Time.deltaTime;
            lift = Mathf.MoveTowards(lift, liftTarget, Time.deltaTime * 6f);
            if (age >= duration)
            {
                gameObject.SetActive(false);
                return;
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
            transform.position = anchor + Vector3.up * (rise * Tween.EaseOutQuad(t) + lift);
            float pop = t < 0.2f ? Tween.EaseOutBack(t / 0.2f) : 1f;
            transform.localScale = Vector3.one * pop;
            Color c = color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            label.color = c;
        }
    }
}
