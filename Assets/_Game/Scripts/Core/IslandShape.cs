// IslandShape.cs
// Karanın organik kıyı şeklini tek yerde tanımlar: ana ada (elips + birkaç dalga) ve küçük adacıklar (aynı yöntemle,
// kendi merkez/yarıçapları). Köprüler tamamlanınca (OpenBridge) köprünün üstü de yürünebilir kara sayılır.
// Sahne kurma komutu zemin modellerini, kum/çimen dokularını ve suyun kıyı köpüğünü bu şekilden üretir;
// oyunda ise karakterlerin karada kalması (ClampInside) ve işçi yollarının kontrolü (Contains) bunu kullanır.

using System.Collections.Generic;
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
        [Tooltip("Adacıklar: her biri (merkez X, merkez Z, yarıçap X, yarıçap Z). Kıyı dalgaları ana adayla aynı (fazı kaydırılmış).")]
        [SerializeField] private Vector4[] islets = new Vector4[0];

        // Açık (tamamlanmış) köprüler: iki uç (XZ) ve yarı genişlik.
        private struct Bridge { public Vector2 A, B; public float HalfWidth; }
        private readonly List<Bridge> bridges = new List<Bridge>();

        public Vector2 Center => center;
        public Vector2 Radius => radius;
        public int IsletCount => islets.Length;
        public Vector4 Islet(int i) => islets[i];

        private void Awake()
        {
            Instance = this;
        }

#if UNITY_EDITOR
        // Sadece sahne kurma komutu için: şekli ayarlar.
        public void EditorSetup(Vector2 newCenter, Vector2 newRadius, Vector4[] newIslets)
        {
            center = newCenter;
            radius = newRadius;
            islets = newIslets ?? new Vector4[0];
            Instance = this;
        }
#endif

        // ---------------------------------------------------------------- Tek parça (ana ada = -1, adacık = 0..n)

        private void Lobe(int index, out Vector2 c, out Vector2 r, out float phase)
        {
            if (index < 0) { c = center; r = radius; phase = 0f; return; }
            Vector4 v = islets[index];
            c = new Vector2(v.x, v.y);
            r = new Vector2(v.z, v.w);
            phase = 1.3f + index * 2.1f;
        }

        // Verilen açıda (radyan) kıyının, elips yarıçapına oranı (~1).
        public float CoastFactor(float angle) => CoastFactor(angle, 0f);

        private float CoastFactor(float angle, float phase)
        {
            float f = 1f;
            foreach (Vector3 w in waves) f += w.y * Mathf.Cos(w.x * angle + w.z + phase * w.x);
            return f;
        }

        // Ana adanın verilen açıdaki kıyı noktası (XZ).
        public Vector2 CoastPoint(float angle) => CoastPoint(-1, angle);

        // Parçanın (ana ada = -1) verilen açıdaki kıyı noktası.
        public Vector2 CoastPoint(int lobe, float angle)
        {
            Lobe(lobe, out Vector2 c, out Vector2 r, out float phase);
            float f = CoastFactor(angle, phase);
            return c + new Vector2(Mathf.Cos(angle) * r.x, Mathf.Sin(angle) * r.y) * f;
        }

        public Vector2 LobeCenter(int lobe)
        {
            Lobe(lobe, out Vector2 c, out _, out _);
            return c;
        }

        private float LobeDistance(int lobe, Vector2 point)
        {
            Lobe(lobe, out Vector2 c, out Vector2 r, out float phase);
            Vector2 d = point - c;
            Vector2 q = new Vector2(d.x / r.x, d.y / r.y);
            float angle = Mathf.Atan2(q.y, q.x);
            float along = new Vector2(Mathf.Cos(angle) * r.x, Mathf.Sin(angle) * r.y).magnitude;
            return (q.magnitude - CoastFactor(angle, phase)) * along;
        }

        private Vector2 LobeClamp(int lobe, Vector2 point, float margin)
        {
            Lobe(lobe, out Vector2 c, out Vector2 r, out float phase);
            Vector2 d = point - c;
            Vector2 q = new Vector2(d.x / r.x, d.y / r.y);
            float angle = Mathf.Atan2(q.y, q.x);
            float along = new Vector2(Mathf.Cos(angle) * r.x, Mathf.Sin(angle) * r.y).magnitude;
            float limit = CoastFactor(angle, phase) - margin / along;
            if (q.magnitude <= limit || q.magnitude < 0.0001f) return point;
            q = q.normalized * limit;
            return c + new Vector2(q.x * r.x, q.y * r.y);
        }

        // ---------------------------------------------------------------- Bütün kara

        // Kıyıya yaklaşık uzaklık (metre): içeride eksi, denizde artı. Ana ada ve adacıkların birleşimi (köprüler hariç).
        public float SignedDistance(Vector2 point)
        {
            float d = LobeDistance(-1, point);
            for (int i = 0; i < islets.Length; i++) d = Mathf.Min(d, LobeDistance(i, point));
            return d;
        }

        // Hangi parçanın içinde (ana ada = -1); hiçbiri değilse en yakını.
        public int NearestLobe(Vector2 point)
        {
            int best = -1;
            float bestD = LobeDistance(-1, point);
            for (int i = 0; i < islets.Length; i++)
            {
                float d = LobeDistance(i, point);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // Nokta karada mı (kıyıdan en az margin metre içeride) ya da açık bir köprünün üstünde mi?
        public bool Contains(Vector2 point, float margin)
        {
            return SignedDistance(point) <= -margin || OnBridge(point, 0f);
        }

        // Nokta açık bir köprünün üstünde mi (kenardan margin içeride)?
        public bool OnBridge(Vector2 point, float margin)
        {
            foreach (Bridge b in bridges)
            {
                if (BridgeDistance(b, point) <= b.HalfWidth - margin) return true;
            }
            return false;
        }

        private static float BridgeDistance(Bridge b, Vector2 p)
        {
            Vector2 ab = b.B - b.A;
            float t = Mathf.Clamp01(Vector2.Dot(p - b.A, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (p - (b.A + ab * t)).magnitude;
        }

        // Nokta denize taştıysa en yakın karaya (ya da açık köprüye) margin metre içeriye çeker.
        public Vector2 ClampInside(Vector2 point, float margin)
        {
            if (SignedDistance(point) <= -margin) return point;
            foreach (Bridge b in bridges) if (BridgeDistance(b, point) <= b.HalfWidth - BridgeMargin) return point;

            Vector2 best = LobeClamp(-1, point, margin);
            float bestD = (best - point).sqrMagnitude;
            for (int i = 0; i < islets.Length; i++)
            {
                Vector2 c = LobeClamp(i, point, margin);
                float d = (c - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            foreach (Bridge b in bridges)
            {
                Vector2 ab = b.B - b.A;
                float t = Mathf.Clamp01(Vector2.Dot(point - b.A, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                Vector2 axis = b.A + ab * t;
                Vector2 off = point - axis;
                float limit = Mathf.Max(0.05f, b.HalfWidth - BridgeMargin);
                Vector2 c = off.magnitude > limit ? axis + off.normalized * limit : point;
                float d = (c - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        // Köprü kenarında karakterin kalacağı pay (köprü korkuluğu).
        private const float BridgeMargin = 0.35f;

        // Köprü tamamlandı: üstü yürünebilir olur (uçlar iki kıyının içine uzanır).
        public void OpenBridge(Vector2 a, Vector2 b, float halfWidth)
        {
            bridges.Add(new Bridge { A = a, B = b, HalfWidth = halfWidth });
        }
    }
}
