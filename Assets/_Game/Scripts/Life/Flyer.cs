// Flyer.cs
// Uçan canlılar: martı (gökyüzünde yavaşça daire çizer, gölgesi yere düşer) ve kelebek (bahçede dalgalı uçuşur).
// Kanatlar menteşelerinden çırpılır. Çarpışması yoktur. Hız, yükseklik vb. GameConfig'te (Canlılar).

using UnityEngine;

namespace KayipAda
{
    public class Flyer : MonoBehaviour
    {
        public enum Kind { Seagull, Butterfly }

        [SerializeField] private GameConfig config;
        [SerializeField] private Kind kind;
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [Tooltip("Yere düşen yumuşak gölge (martı). Boş olabilir.")]
        [SerializeField] private Transform shadow;

        private Vector3 center;
        private float radius;
        private float angle;
        private float height;
        private float phase;

        public void Setup(Vector3 areaCenter, float areaRadius)
        {
            center = areaCenter;
            radius = areaRadius;
            angle = Random.Range(0f, Mathf.PI * 2f);
            phase = Random.Range(0f, 10f);
            height = kind == Kind.Seagull ? Random.Range(config.GullHeightMin, config.GullHeightMax) : config.ButterflyHeight;
        }

        private void Update()
        {
            float t = Time.time + phase;
            Vector3 pos;
            float flap;
            if (kind == Kind.Seagull)
            {
                angle += config.GullSpeed / Mathf.Max(1f, radius) * Time.deltaTime;
                pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + Vector3.up * (height + Mathf.Sin(t * 0.7f) * 0.4f);
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, 0f, 12f); // dönüşe yatık
                flap = Mathf.Sin(t * 5f) > 0.6f ? Mathf.Sin(t * 10f) * 28f : 6f; // çoğu zaman süzülür, ara sıra çırpar
            }
            else
            {
                // Kelebek: bahçe içinde düzensiz (iki dalgalı) yol.
                angle += config.ButterflySpeed * Time.deltaTime;
                Vector3 wander = new Vector3(Mathf.Sin(angle * 1.3f + phase) * radius, 0f, Mathf.Sin(angle * 0.9f + phase * 2f) * radius);
                pos = center + wander + Vector3.up * (height + Mathf.Sin(t * 3f) * 0.25f);
                Vector3 vel = pos - transform.position;
                vel.y = 0f;
                if (vel.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vel), Time.deltaTime * 4f);
                flap = Mathf.Sin(t * 22f) * 55f;
            }
            transform.position = pos;
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
            if (shadow != null)
            {
                shadow.position = new Vector3(pos.x, 0.04f, pos.z);
                shadow.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }
}
