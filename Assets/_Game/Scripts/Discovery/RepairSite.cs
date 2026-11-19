// RepairSite.cs
// Kırık bir yapı (kuyu, çit, iskele): malzeme getirildikçe kırık parçalar yerine oturur; bitince kırık görünüş
// kaybolur, sağlam yapı zıplayarak belirir (parıltı + ses). Yan görevler (QuestManager) bunu dinler.

using UnityEngine;

namespace KayipAda
{
    public class RepairSite : ConstructionSite
    {
        [Tooltip("Kırıkken görünen yapı.")]
        [SerializeField] private GameObject brokenVisual;
        [Tooltip("Onarılınca görünen yapı.")]
        [SerializeField] private GameObject fixedVisual;

        protected override void Awake()
        {
            base.Awake();
            if (fixedVisual != null) fixedVisual.SetActive(false);
        }

        protected override void OnPieceAdded(ResourceType type, int index, bool animate)
        {
            if (animate && brokenVisual != null) Tween.PunchScale(brokenVisual.transform, 0.06f, 0.2f);
        }

        protected override void OnCompleted(bool animate)
        {
            if (brokenVisual != null) brokenVisual.SetActive(false);
            if (fixedVisual == null) return;
            fixedVisual.SetActive(true);
            if (!animate) return;
            Tween.PopIn(fixedVisual.transform, fixedVisual.transform.localScale, config.PopInDuration);
            if (Effects.Instance != null)
            {
                Effects.Instance.Sparkle(fixedVisual.transform.position + Vector3.up);
                Effects.Instance.ShowText(fixedVisual.transform.position + Vector3.up * 2.4f, DisplayName + " onarıldı!", new Color(0.6f, 1f, 0.6f));
            }
            AudioManager.Celebrate(CelebrationTier.Small);
        }
    }
}
