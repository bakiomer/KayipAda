// Tween.cs
// Basit animasyon yardımcısı (harici paket yok): zıplama, sallanma, kavisli uçuş, belirme gibi
// kısa hareketleri başlatır. Asıl güncelleme TweenRunner'dadır (sahneye kendini ekler).
// Hedef nesne yok edilirse animasyon sessizce biter.

using System;
using UnityEngine;

namespace KayipAda
{
    public static class Tween
    {
        // Genel: duration boyunca onUpdate(0..1) çağırır, bitince onComplete. owner yok olursa durur.
        public static void Value(UnityEngine.Object owner, float duration, Action<float> onUpdate,
                                 Action onComplete = null, float delay = 0f)
        {
            TweenRunner.Add(owner, duration, delay, onUpdate, onComplete);
        }

        // Ölçeği kısa süre büyütüp esneterek eski haline getirir ("sıkış-genişle").
        public static void PunchScale(Transform target, float amount, float duration)
        {
            if (target == null) return;
            Vector3 baseScale = TweenRunner.GetBaseScale(target);
            Value(target, duration, t =>
            {
                // Hızla büyüyüp sönümlenen yay hareketi; yatayda ters yönde esner (squash & stretch).
                float wave = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
                target.localScale = new Vector3(baseScale.x * (1f - wave * amount * 0.6f),
                                                baseScale.y * (1f + wave * amount),
                                                baseScale.z * (1f - wave * amount * 0.6f));
            }, () => { if (target != null) target.localScale = baseScale; });
        }

        // Kısa süre sağa sola sallanır (dönme).
        public static void Shake(Transform target, float angle, float duration)
        {
            if (target == null) return;
            Quaternion baseRotation = target.localRotation;
            float axisAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3 axis = Quaternion.Euler(0f, axisAngle, 0f) * Vector3.right;
            Value(target, duration, t =>
            {
                float wave = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * angle;
                target.localRotation = baseRotation * Quaternion.AngleAxis(wave, axis);
            }, () => { if (target != null) target.localRotation = baseRotation; });
        }

        // 0'dan hedef ölçeğe zıplayarak büyür (açılan nesneler için).
        public static void PopIn(Transform target, Vector3 finalScale, float duration, float delay = 0f)
        {
            if (target == null) return;
            target.localScale = Vector3.zero;
            Value(target, duration, t => target.localScale = finalScale * EaseOutBack(t),
                  () => { if (target != null) target.localScale = finalScale; }, delay);
        }

        // Hedef ölçekten 0'a küçülür, bitince onComplete (örn. gizle).
        public static void PopOut(Transform target, float duration, Action onComplete = null)
        {
            if (target == null) return;
            Vector3 startScale = target.localScale;
            Value(target, duration, t => target.localScale = startScale * (1f - EaseInBack(t)), onComplete);
        }

        // Başlangıçtan (hareket edebilen) hedefe kavis çizerek uçar.
        public static void MoveArc(Transform target, Vector3 from, Func<Vector3> to, float height, float duration,
                                   Action onComplete = null)
        {
            if (target == null) return;
            Value(target, duration, t =>
            {
                Vector3 end = to();
                Vector3 pos = Vector3.LerpUnclamped(from, end, EaseInOutQuad(t));
                pos.y += Mathf.Sin(t * Mathf.PI) * height;
                target.position = pos;
            }, () =>
            {
                if (target == null) return;
                target.position = to();
                onComplete?.Invoke();
            });
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        public static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
