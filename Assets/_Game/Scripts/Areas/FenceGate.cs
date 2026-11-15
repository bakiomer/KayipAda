// FenceGate.cs
// Kapalı bölgenin çit kapısı: bölge kapalıyken iki kanat kapalıdır ve kapı aralığında bir engel (NavObstacle)
// vardır; kimse içeri giremez. Bölge açılınca kanatlar dışarı doğru dönerek açılır, engel kalkar.
// Oyun başında zaten açık olan bölgede kapı efektsiz açık başlar. Açılma süresi GameConfig.GateOpenDuration.

using UnityEngine;

namespace KayipAda
{
    public class FenceGate : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private AreaDefinition area;
        [Tooltip("Sol kanat menteşesi (dönen nokta).")]
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [Tooltip("Açılınca sol kanadın Y ekseninde döneceği açı (sağ kanat ters yöne döner).")]
        [SerializeField] private float leftOpenAngle = 100f;
        [SerializeField] private float rightOpenAngle = -100f;
        [Tooltip("Kapı aralığını kapalıyken kapatan engel.")]
        [SerializeField] private NavObstacle blocker;

        public bool IsOpen { get; private set; }

        private Quaternion leftClosed, rightClosed;

        private void Awake()
        {
            leftClosed = leftHinge.localRotation;
            rightClosed = rightHinge.localRotation;
        }

        private void Start()
        {
            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            if (AreaManager.Instance.IsUnlocked(area)) SetOpen(1f);
            else SetOpen(0f);
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
        }

        private void OnAreaUnlocked(AreaDefinition unlocked)
        {
            if (unlocked != area) return;
            AudioManager.Play(SoundId.GateOpen);
            Tween.Value(this, config.GateOpenDuration, t => SetOpen(Tween.EaseOutBack(t)), () => SetOpen(1f));
        }

        private void SetOpen(float k)
        {
            IsOpen = k >= 1f;
            leftHinge.localRotation = leftClosed * Quaternion.Euler(0f, leftOpenAngle * k, 0f);
            rightHinge.localRotation = rightClosed * Quaternion.Euler(0f, rightOpenAngle * k, 0f);
            if (blocker == null) return;
            bool block = k < 0.5f; // kanatlar yarı açılınca geçiş açılır
            if (blocker.enabled != block) blocker.enabled = block;
        }
    }
}
