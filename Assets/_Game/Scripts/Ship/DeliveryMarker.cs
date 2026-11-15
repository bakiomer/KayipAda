// DeliveryMarker.cs
// Gemi teslim alanının üstünde aşağıyı gösteren, yukarı-aşağı zıplayan ok ve yerdeki nabız atan işaret.
// Oyuncu malzemeyi nereye götüreceğini uzaktan görsün diye. Gemi bitince (tören) gizlenir.
// Zıplama ve nabız sayıları GameConfig'teki rehber oku ayarlarını kullanır.

using UnityEngine;

namespace KayipAda
{
    public class DeliveryMarker : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Zıplayan ok (kameraya dönük).")]
        [SerializeField] private Transform arrow;
        [Tooltip("Yerde nabız atan işaret (daire ve oklar).")]
        [SerializeField] private Transform groundMark;

        private Vector3 arrowBase;
        private Vector3 markScale;

        private void Awake()
        {
            if (arrow != null) arrowBase = arrow.localPosition;
            if (groundMark != null) markScale = groundMark.localScale;
        }

        private void Update()
        {
            bool show = ShipBuilder.Instance == null || !ShipBuilder.Instance.IsComplete;
            if (arrow != null && arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (groundMark != null && groundMark.gameObject.activeSelf != show) groundMark.gameObject.SetActive(show);
            if (!show) return;

            float wave = Mathf.Sin(Time.time * config.GuidePulseSpeed);
            if (arrow != null) arrow.localPosition = arrowBase + Vector3.up * (Mathf.Abs(wave) * config.GuideArrowBob);
            if (groundMark != null) groundMark.localScale = markScale * (1f + wave * config.GuidePulseScale);
        }
    }
}
