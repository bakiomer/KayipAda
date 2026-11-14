// TweenRunner.cs
// Tween animasyonlarını her karede ilerleten gizli yönetici. İlk animasyon başlatıldığında
// sahneye kendini ekler; elle kurulmaz. Aynı nesneye üst üste zıplama verilirse ölçek bozulmasın
// diye ilk (asıl) ölçeği hatırlar.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class TweenRunner : MonoBehaviour
    {
        private class Entry
        {
            public UnityEngine.Object Owner;
            public bool HasOwner;
            public float Duration;
            public float Delay;
            public float Time;
            public Action<float> OnUpdate;
            public Action OnComplete;
        }

        private static TweenRunner instance;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Entry> adding = new List<Entry>();
        private readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();

        public static void Add(UnityEngine.Object owner, float duration, float delay, Action<float> onUpdate, Action onComplete)
        {
            if (!Application.isPlaying) return;
            Ensure();
            instance.adding.Add(new Entry
            {
                Owner = owner,
                HasOwner = owner != null,
                Duration = Mathf.Max(0.0001f, duration),
                Delay = delay,
                OnUpdate = onUpdate,
                OnComplete = onComplete
            });
        }

        // Nesnenin zıplamadan önceki asıl ölçeği (ilk çağrıda kaydedilir).
        public static Vector3 GetBaseScale(Transform target)
        {
            Ensure();
            if (!instance.baseScales.TryGetValue(target, out Vector3 scale))
            {
                scale = target.localScale;
                instance.baseScales[target] = scale;
            }
            return scale;
        }

        // Nesnenin asıl ölçeği bilerek değişince (örn. tükenen ağaç küçülünce) çağrılır.
        public static void SetBaseScale(Transform target, Vector3 scale)
        {
            Ensure();
            instance.baseScales[target] = scale;
        }

        private static void Ensure()
        {
            if (instance != null) return;
            GameObject go = new GameObject("TweenRunner");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TweenRunner>();
        }

        private void Update()
        {
            if (adding.Count > 0)
            {
                entries.AddRange(adding);
                adding.Clear();
            }

            float dt = Time.deltaTime;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry e = entries[i];
                if (e.HasOwner && e.Owner == null) // hedef yok edildi
                {
                    entries.RemoveAt(i);
                    continue;
                }

                if (e.Delay > 0f)
                {
                    e.Delay -= dt;
                    continue;
                }

                e.Time += dt;
                float t = Mathf.Clamp01(e.Time / e.Duration);
                e.OnUpdate?.Invoke(t);
                if (t >= 1f)
                {
                    entries.RemoveAt(i);
                    e.OnComplete?.Invoke();
                }
            }
        }
    }
}
