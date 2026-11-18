// UpgradeStation.cs
// Kamptaki geliştirme tezgahı / Tayfa Barakası. Oyuncu alanda durunca (PanelZone) alttan panel açılır;
// hareket edince ya da çıkınca kapanır.

using UnityEngine;

namespace KayipAda
{
    public class UpgradeStation : PanelZone
    {
        [SerializeField] private UpgradePanel panel;
        [Tooltip("Satın alınca parlayan nokta (tezgahın üstü).")]
        [SerializeField] private Transform effectPoint;

        public Transform EffectPoint => effectPoint != null ? effectPoint : transform;

        protected override void OpenPanel(CarryStack visitor)
        {
            if (panel != null) panel.Open(this);
        }

        protected override void ClosePanel()
        {
            if (panel != null) panel.Close(this);
        }
    }
}
