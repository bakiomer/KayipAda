// IslandShape.cs
// Adanın organik kıyı şeklini tek yerde tanımlar: elips + birkaç dalga (kıyı girintileri).
// Sahne kurma komutu adanın zemin modelini, kum/çimen dokusunu ve suyun kıyı köpüğünü bu şekilden üretir;
// oyunda ise karakterlerin karada kalması (ClampInside) ve işçi yollarının kontrolü (Contains) bunu kullanır.

using UnityEngine;

namespace KayipAda
{
    public class IslandShape : MonoBehaviour
    {
        public static IslandShape Instance { get; private set; }

        [Tooltip("Adanın merkezi (XZ).")]
        [SerializeField] private Vector2 center = Vector2.zero;
        [Tooltip("Elipsin yarı çapları (X = genişlik, Y = derinlik/Z), metre.")]
        [SerializeField] private Vector2 radius = new Vector2(16f, 20f);
        [Tooltip("Kıyı dalgaları: her biri (frekans, genlik, faz).")]
        [SerializeField] private Vector3[] waves =
        {
            new Vector3(2f, 0.035f, 0.4f),
            new Vector3(3f, 0.03f, 1.7f),
            new Vector3(5f, 0.02f, 4.1f),
            new Vector3(7f, 0.012f, 2.3f)
        };

        public Vector2 Center => center;
        public Vector2 Radius => radius;

        private void Awake()
        {
            Instance = this;
        }

#if UNITY_EDITOR
        // Sadece sahne kurma komutu için: şekli ayarlar.
        public void EditorSetup(Vector2 newCenter, Vector2 newRadius)
        {
            center = newCenter;
            radius = newRadius;
            Instance = this;
        }
#endif

        // Verilen açıda (radyan) kıyının, elips yarıçapına oranı (~1).
        public float CoastFactor(float angle)
        {
            float f = 1f;
            foreach (Vector3 w in waves) f += w.y * Mathf.Cos(w.x * angle + w.z);
            return f;
        }

        // Verilen açıdaki kıyı noktası (XZ).
        public Vector2 CoastPoint(float angle)
        {
            float f = CoastFactor(angle);
            return center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y) * f;
        }

        // Kıyıya yaklaşık uzaklık (metre): içeride eksi, denizde artı.
        public float SignedDistance(Vector2 point)
        {
            Vector2 d = point - center;
            Vector2 q = new Vector2(d.x / radius.x, d.y / radius.y);
            float angle = Mathf.Atan2(q.y, q.x);
            float along = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y).magnitude;
            return (q.magnitude - CoastFactor(angle)) * along;
        }

        // Nokta karada mı (kıyıdan en az margin metre içeride)?
        public bool Contains(Vector2 point, float margin)
        {
            return SignedDistance(point) <= -margin;
        }

        // Nokta denize taştıysa, kıyıdan margin metre içeriye çeker.
        public Vector2 ClampInside(Vector2 point, float margin)
        {
            Vector2 d = point - center;
            Vector2 q = new Vector2(d.x / radius.x, d.y / radius.y);
            float angle = Mathf.Atan2(q.y, q.x);
            float along = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y).magnitude;
            float limit = CoastFactor(angle) - margin / along;
            if (q.magnitude <= limit || q.magnitude < 0.0001f) return point;

            q = q.normalized * limit;
            return center + new Vector2(q.x * radius.x, q.y * radius.y);
        }
    }
}
