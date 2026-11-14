// CharacterVisual.cs
// Oyuncu ve işçilerin görünüşünü yönetir (hareket mantığı yok, sadece görsel):
// - Gerçek hıza göre Animator'daki "Speed" değerini ayarlar (Idle → Walk → Run geçişi),
// - modeli yürüdüğü yöne yumuşakça döndürür,
// - kaynak taşırken kolları öne alır (pakette taşıma animasyonu olmadığı için kodla yapılan taşıma pozu),
// - koşarken arkada toz bırakır, istenirse adım sesi çalar.

using UnityEngine;

namespace KayipAda
{
    public class CharacterVisual : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private GameConfig config;
        [SerializeField] private Animator animator;
        [Tooltip("Yürüme yönüne dönen model kökü.")]
        [SerializeField] private Transform model;
        [Tooltip("Taşıma pozu için: yığın boş değilse kollar öne gelir.")]
        [SerializeField] private CarryStack stack;
        [Tooltip("Koşarken çıkan toz (boş olabilir).")]
        [SerializeField] private ParticleSystem dust;
        [Tooltip("Adım sesi çalınsın mı? (Sadece oyuncu.)")]
        [SerializeField] private bool footsteps;

        [Header("Taşıma pozu (kemik adları modelden bulunur)")]
        [SerializeField] private string upperArmLeft = "UpperArm.L";
        [SerializeField] private string lowerArmLeft = "LowerArm.L";
        [SerializeField] private string upperArmRight = "UpperArm.R";
        [SerializeField] private string lowerArmRight = "LowerArm.R";

        private Transform upperL, lowerL, upperR, lowerR;
        private Vector3 lastPosition;
        private float smoothedSpeed;
        private float carryWeight;
        private float stepDistance;
        private ParticleSystem.EmissionModule dustEmission;

        private void Awake()
        {
            if (model == null) model = transform;
            upperL = FindBone(upperArmLeft);
            lowerL = FindBone(lowerArmLeft);
            upperR = FindBone(upperArmRight);
            lowerR = FindBone(lowerArmRight);
            if (dust != null) dustEmission = dust.emission;
            lastPosition = transform.position;
        }

        private Transform FindBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return null;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == boneName) return t;
            }
            return null;
        }

        private void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;
            float speed = delta.magnitude / dt;

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-12f * dt));
            if (animator != null) animator.SetFloat(SpeedHash, smoothedSpeed / config.AnimRunSpeed);

            // Yürüdüğü yöne dön.
            if (speed > 0.2f)
            {
                Quaternion look = Quaternion.LookRotation(delta.normalized, Vector3.up);
                model.rotation = Quaternion.Slerp(model.rotation, look, 1f - Mathf.Exp(-config.TurnSpeed * dt));
            }

            // Koşarken toz.
            if (dust != null) dustEmission.rateOverTimeMultiplier = smoothedSpeed > config.AnimRunSpeed * 0.6f ? 10f : 0f;

            // Adım sesi: belirli mesafede bir.
            if (footsteps)
            {
                stepDistance += delta.magnitude;
                if (stepDistance >= config.FootstepDistance)
                {
                    stepDistance = 0f;
                    AudioManager.Play(SoundId.Footstep, 0.35f);
                }
            }

            float carryTarget = stack != null && !stack.IsEmpty ? 1f : 0f;
            carryWeight = Mathf.MoveTowards(carryWeight, carryTarget, dt * 6f);
        }

        // Animasyon kemikleri yerleştirdikten SONRA kolları taşıma pozuna büker.
        private void LateUpdate()
        {
            if (carryWeight <= 0.001f) return;
            ApplyArm(upperL, lowerL, -1f);
            ApplyArm(upperR, lowerR, 1f);
        }

        private void ApplyArm(Transform upper, Transform lower, float side)
        {
            if (upper == null || lower == null) return;

            Vector3 forward = model.forward;
            Vector3 right = model.right * side;

            // Üst kol: öne ve biraz aşağı. Alt kol: öne, hafif yukarı ve içe (yığını kavrar).
            Vector3 upperTarget = (forward * 0.8f - Vector3.up * 0.5f + right * 0.15f).normalized;
            Vector3 lowerTarget = (forward * 0.9f + Vector3.up * 0.3f - right * 0.3f).normalized;

            RotateBoneToward(upper, lower.position, upperTarget);
            if (lower.childCount > 0) RotateBoneToward(lower, lower.GetChild(0).position, lowerTarget);
        }

        private void RotateBoneToward(Transform bone, Vector3 childPosition, Vector3 targetDirection)
        {
            Vector3 current = childPosition - bone.position;
            if (current.sqrMagnitude < 0.000001f) return;
            Quaternion delta = Quaternion.FromToRotation(current.normalized, targetDirection);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, delta, carryWeight) * bone.rotation;
        }
    }
}
