// Sway.cs
// Rüzgârda hafifçe sallanma (keten/saz tarlası bitkileri). Her bitki kendi rastgele ritmiyle sallanır.
// Açı ve hız GameConfig'te (SwayAngle, SwaySpeed).

using UnityEngine;

namespace KayipAda
{
    public class Sway : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Sallanma oranı (1 = GameConfig.SwayAngle; palmiye tepeleri daha az sallanır).")]
        [SerializeField] private float amount = 1f;

        private Quaternion baseRotation;
        private float phase;
        private Vector3 axis;

        private void Awake()
        {
            baseRotation = transform.localRotation;
            phase = Random.value * 10f;
            axis = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right;
        }

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * config.SwaySpeed + phase) * config.SwayAngle * amount;
            transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, axis);
        }
    }
}
