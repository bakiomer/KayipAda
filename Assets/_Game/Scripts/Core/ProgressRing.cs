// ProgressRing.cs
// Dünyada duran yuvarlak ilerleme halkası (bina üstündeki işleme halkası, açma karesinin dolan kenarı).
// Dairesel doldurulan bir UI Image kullanır; SetProgress(0..1) ile güncellenir.
// Değer yumuşakça hedefe kayar ki halka titremesin.

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class ProgressRing : MonoBehaviour
    {
        [Tooltip("Dairesel doldurulan halka resmi (Image Type = Filled, Radial 360).")]
        [SerializeField] private Image fill;
        [Tooltip("Boşken halka gizlensin mi? (Bina halkası için evet.)")]
        [SerializeField] private bool hideWhenEmpty;
        [Tooltip("Gizlenecek görsel (boşsa kendisi).")]
        [SerializeField] private GameObject visualRoot;

        private float target;
        private float shown;

        public void SetProgress(float progress)
        {
            target = Mathf.Clamp01(progress);
            if (target < shown) shown = target; // geri sarma anında olsun (yeni işlem başladı)
        }

        private void LateUpdate()
        {
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 4f);
            fill.fillAmount = shown;

            if (hideWhenEmpty && visualRoot != null)
            {
                bool visible = target > 0.001f;
                if (visualRoot.activeSelf != visible) visualRoot.SetActive(visible);
            }
        }
    }
}
