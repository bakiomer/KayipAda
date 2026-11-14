// UpgradeStation.cs
// Kamptaki geliştirme tezgahı. Oyuncu üstüne gelince alttan geliştirme paneli açılır; çıkınca kapanır.
// Panel kapatıldıktan sonra oyuncu alandan çıkıp tekrar girene kadar yeniden açılmaz.

using UnityEngine;

namespace KayipAda
{
    public class UpgradeStation : InteractionZone
    {
        [SerializeField] private UpgradePanel panel;
        [Tooltip("Satın alınca parlayan nokta (tezgahın üstü).")]
        [SerializeField] private Transform effectPoint;

        public Transform EffectPoint => effectPoint != null ? effectPoint : transform;

        protected override void OnVisitorEnter(CarryStack visitor)
        {
            if (visitor.IsPlayer && panel != null) panel.Open(this);
        }

        protected override void OnVisitorExit(CarryStack visitor)
        {
            if (visitor.IsPlayer && panel != null) panel.Close();
        }
    }
}
