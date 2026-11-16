// TrailEmitter.cs
// Karakterin yürürken iz bırakması: belli aralıkla (oyuncu sık, işçiler seyrek) sol-sağ ayak sırayla TrailPool'a
// adım bildirir; havuz zemine göre ayak izi, su halkası ya da yaprak çıkarır. Koşarken toz CharacterVisual'da.

using UnityEngine;

namespace KayipAda
{
    public class TrailEmitter : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Oyuncu mu? (İşçiler daha seyrek iz bırakır.)")]
        [SerializeField] private bool isPlayer;
        [Tooltip("Ayakların merkezden yana uzaklığı (metre).")]
        [SerializeField] private float footSpread = 0.13f;

        private Vector3 last;
        private float travelled;
        private int foot = 1;

        private void OnEnable()
        {
            last = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 now = transform.position;
            Vector3 delta = now - last;
            delta.y = 0f;
            last = now;
            float d = delta.magnitude;
            if (d < 0.0001f || d > 2f) return; // durdu ya da ışınlandı

            travelled += d;
            float step = isPlayer ? config.FootprintStep : config.WorkerFootprintStep;
            if (travelled < step) return;
            travelled = 0f;
            if (TrailPool.Instance == null) return;

            Vector3 forward = delta / d;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * (footSpread * foot);
            foot = -foot;
            bool running = d / Mathf.Max(0.0001f, Time.deltaTime) > 4f;
            TrailPool.Instance.Step(now + side, forward, running);
        }
    }
}
