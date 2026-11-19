// ScreenSizeLimit.cs
// Bina başlık levhası (ahşap tabela) kameraya yakınken ekranı kaplamasın: ekrandaki yüksekliği/genişliği
// GameConfig > "Ekrandaki yazılar" sınırını aşarsa levha o kadar küçülür; uzakta asıl boyuna döner.

using UnityEngine;

namespace KayipAda
{
    public class ScreenSizeLimit : MonoBehaviour
    {
        [Tooltip("Levhanın asıl boyu (metre, ölçek 1'de): genişlik × yükseklik.")]
        [SerializeField] private Vector2 size = new Vector2(2f, 0.9f);

        private Vector3 baseScale;
        private Camera cam;

        public float LastScreenHeight { get; private set; }
        public float LastScreenWidth { get; private set; }

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || CameraFollow.Main == null) return;
            GameConfig cfg = CameraFollow.Main.Config;
            transform.localScale = baseScale;
            float dist = Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward);
            if (dist <= 0.1f) return;
            float viewH = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewW = viewH * cam.aspect;
            Vector3 s = transform.lossyScale;
            float h = size.y * s.y / viewH, w = size.x * s.x / viewW;
            float k = Mathf.Min(1f, cfg.SignMaxScreenHeight / Mathf.Max(0.0001f, h), cfg.SignMaxScreenWidth / Mathf.Max(0.0001f, w));
            transform.localScale = baseScale * k;
            LastScreenHeight = h * k;
            LastScreenWidth = w * k;
        }
    }
}
