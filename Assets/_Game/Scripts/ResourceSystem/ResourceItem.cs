// ResourceItem.cs
// Dünyada duran, taşınan veya uçan tek bir kaynak parçası (3D model).
// Her türün kendi prefab'ı vardır (Prefabs/Items); Setup ile türü atanır.
// FlyTo ile hedefe kavis çizerek uçar; yeni bir uçuş eskisinin yerini alır (yarıda kesilen uçuş sorun çıkarmaz).

using System;
using UnityEngine;

namespace KayipAda
{
    public class ResourceItem : MonoBehaviour
    {
        public ResourceType Type { get; private set; }
        public bool IsFlying { get; private set; }

        private Vector3 flightStart;
        private Func<Vector3> flightTarget;
        private float flightHeight;
        private float flightDuration;
        private float flightTime;
        private Action flightDone;
        private bool shrinkWhileFlying;
        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void Setup(ResourceType type)
        {
            Type = type;
            name = type.DisplayName;
        }

        // Hedefe (hareket edebilir) kavis çizerek uçar; varınca onArrive çağrılır.
        public void FlyTo(Func<Vector3> target, float height, float duration, Action onArrive = null)
        {
            flightStart = transform.position;
            flightTarget = target;
            flightHeight = height;
            flightDuration = Mathf.Max(0.01f, duration);
            flightTime = 0f;
            flightDone = onArrive;
            shrinkWhileFlying = false;
            IsFlying = true;
        }

        // Hedefe uçar, küçülür ve kendini yok eder (satış, binada işlenme).
        public void FlyToAndDestroy(Vector3 target, float height, float duration, Action onArrive = null)
        {
            transform.SetParent(null, true);
            FlyTo(() => target, height, duration, () =>
            {
                onArrive?.Invoke();
                Destroy(gameObject);
            });
            shrinkWhileFlying = true;
        }

        private void Update()
        {
            if (!IsFlying) return;

            flightTime += Time.deltaTime;
            float t = Mathf.Clamp01(flightTime / flightDuration);
            Vector3 end = flightTarget();
            Vector3 pos = Vector3.LerpUnclamped(flightStart, end, Tween.EaseInOutQuad(t));
            pos.y += Mathf.Sin(t * Mathf.PI) * flightHeight;
            transform.position = pos;
            transform.Rotate(0f, 540f * Time.deltaTime * (1f - t), 0f, Space.World); // uçarken hafif döner
            if (shrinkWhileFlying) transform.localScale = baseScale * Mathf.Lerp(1f, 0.3f, t * t);

            if (t < 1f) return;
            IsFlying = false;
            Action done = flightDone;
            flightDone = null;
            done?.Invoke();
        }
    }
}
