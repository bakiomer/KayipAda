// CrystalUI.cs
// Para panelinin altındaki küçük kristal göstergesi (mor taş + sayı). Kristal artınca zıplar ve sayarak artar.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class CrystalUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform punchTarget;

        private float shown;
        private int target;
        private float punch;

        private void Start()
        {
            if (ProfileManager.Instance == null) return;
            ProfileManager.Instance.CrystalsChanged += OnChanged;
            target = ProfileManager.Instance.Crystals;
            shown = target;
            label.text = target.ToString();
        }

        private void OnDestroy()
        {
            if (ProfileManager.Instance != null) ProfileManager.Instance.CrystalsChanged -= OnChanged;
        }

        private void OnChanged(int value)
        {
            if (value > target) punch = 0.35f;
            target = value;
        }

        private void Update()
        {
            if (!Mathf.Approximately(shown, target))
            {
                shown = Mathf.MoveTowards(shown, target, Mathf.Max(8f, Mathf.Abs(target - shown) * 6f) * Time.unscaledDeltaTime);
                label.text = Mathf.RoundToInt(shown).ToString();
            }
            if (punch <= 0f || punchTarget == null) return;
            punch -= Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(punch / 0.35f);
            punchTarget.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.25f);
        }
    }
}
