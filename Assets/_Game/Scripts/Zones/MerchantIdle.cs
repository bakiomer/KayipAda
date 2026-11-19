// MerchantIdle.cs
// Pazar satıcısının boştaki hareketleri: bir süre gelenlere el sallar, sonra yan tezgaha dönüp ürünleri düzenler
// (başını sallayarak eğilir), kısa bekler ve tekrar el sallar. Animator'da "Wave", "Yes", "Idle" durumları olmalı.
// Süreler GameConfig > "Pazar".

using UnityEngine;

namespace KayipAda
{
    public class MerchantIdle : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Animator animator;
        [Tooltip("Ürün düzenlerken dönülen açı (sağa ve sola sırayla).")]
        [SerializeField] private float arrangeTurn = 55f;

        private Quaternion baseRotation;
        private float timer;
        private int phase;
        private int side = 1;
        private float targetYaw;
        private float yaw;

        public int Phase => phase;

        private void Awake()
        {
            baseRotation = transform.localRotation;
            Enter(0);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Enter((phase + 1) % 3);
            yaw = Mathf.MoveTowards(yaw, targetYaw, 180f * Time.deltaTime);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);
        }

        private void Enter(int next)
        {
            phase = next;
            switch (phase)
            {
                case 0: // el salla
                    targetYaw = 0f;
                    Play("Wave");
                    timer = config.MerchantWaveTime;
                    break;
                case 1: // yan tezgahtaki ürünleri düzenle
                    side = -side;
                    targetYaw = side * arrangeTurn;
                    Play("Yes");
                    timer = config.MerchantArrangeTime;
                    break;
                default: // kısa dinlen
                    targetYaw = 0f;
                    Play("Idle");
                    timer = config.MerchantArrangeTime * 0.5f;
                    break;
            }
        }

        private void Play(string state)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.CrossFadeInFixedTime(state, 0.25f);
        }
    }
}
