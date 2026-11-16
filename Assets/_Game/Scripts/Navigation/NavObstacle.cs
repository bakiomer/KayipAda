// NavObstacle.cs
// Karakterlerin içinden geçmediği engel (bina gövdesi, çadır, ağaç gövdesi, gemi), yerde (XZ düzlemi).
// Dikdörtgendir; istenirse nesnenin Y dönüşüyle döner (oriented) ve köşeleri yuvarlatılır (cornerRadius):
// oyuncu yuvarlak köşelerden takılmadan kayar. İşçiler etrafından NavMesh ile dolaşır, oyuncu kenarına çarpıp kayar.
// Açık (aktif) olan tüm engeller tek bir listede tutulur; değişince Version artar ve işçiler yollarını yeniler.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class NavObstacle : MonoBehaviour
    {
        [Tooltip("Engelin genişliği (X) ve derinliği (Z), metre. Nesnenin konumu ortadadır.")]
        [SerializeField] private Vector2 size = Vector2.one;
        [Tooltip("Açıksa engel nesnenin Y dönüşüyle birlikte döner (eğik duran gemi, kızak).")]
        [SerializeField] private bool oriented;
        [Tooltip("Köşe yuvarlaklığı (metre). Oyuncu köşelerde takılmadan kayar.")]
        [SerializeField] private float cornerRadius;

        private static readonly List<NavObstacle> active = new List<NavObstacle>();

        public static IReadOnlyList<NavObstacle> Active => active;

        // Engel listesi her değiştiğinde artar.
        public static int Version { get; private set; }

        public Vector2 Size => size;
        public float Yaw => oriented ? transform.eulerAngles.y : 0f;
        public Vector2 Center => new Vector2(transform.position.x, transform.position.z);

        // Engelin XZ'de kapladığı eksene hizalı dikdörtgen (dönükse onu saran kutu), "margin" kadar büyütülmüş.
        public Rect GetRect(float margin)
        {
            Vector2 half = size * 0.5f;
            if (oriented)
            {
                float a = Yaw * Mathf.Deg2Rad, c = Mathf.Abs(Mathf.Cos(a)), s = Mathf.Abs(Mathf.Sin(a));
                half = new Vector2(half.x * c + half.y * s, half.x * s + half.y * c);
            }
            half += Vector2.one * margin;
            return new Rect(Center - half, half * 2f);
        }

        // Nokta engelin içinde mi (margin kadar büyütülmüş olarak)?
        public bool Contains(Vector2 point, float margin)
        {
            Vector2 local = ToLocal(point);
            Vector2 inner = InnerHalf;
            Vector2 closest = new Vector2(Mathf.Clamp(local.x, -inner.x, inner.x), Mathf.Clamp(local.y, -inner.y, inner.y));
            return (local - closest).magnitude < Rounding + margin;
        }

        private float Rounding => Mathf.Min(cornerRadius, Mathf.Min(size.x, size.y) * 0.5f);
        private Vector2 InnerHalf => size * 0.5f - Vector2.one * Rounding;

        private Vector2 ToLocal(Vector2 p)
        {
            Vector2 d = p - Center;
            if (!oriented) return d;
            float a = Yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            // Unity'de Y dönüşü XZ'de saat yönünde: yerel x = (c, -s), yerel z = (s, c).
            return new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }

        private Vector2 ToWorld(Vector2 local)
        {
            if (!oriented) return Center + local;
            float a = Yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return Center + new Vector2(local.x * c + local.y * s, -local.x * s + local.y * c);
        }

        // Tek engelden dışarı itme (yuvarlatılmış kutu).
        private Vector2 Push(Vector2 point, float radius)
        {
            Vector2 local = ToLocal(point);
            Vector2 inner = InnerHalf;
            float reach = radius + Rounding;
            Vector2 closest = new Vector2(Mathf.Clamp(local.x, -inner.x, inner.x), Mathf.Clamp(local.y, -inner.y, inner.y));
            Vector2 away = local - closest;
            float distance = away.magnitude;

            if (distance > 0.0001f)
            {
                if (distance >= reach) return point;
                return ToWorld(closest + away / distance * reach);
            }

            // Merkez iç kutunun içinde: en yakın kenardan dışarı çıkar.
            float left = local.x + inner.x, right = inner.x - local.x, down = local.y + inner.y, up = inner.y - local.y;
            float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
            if (min == left) local.x = -inner.x - reach;
            else if (min == right) local.x = inner.x + reach;
            else if (min == down) local.y = -inner.y - reach;
            else local.y = inner.y + reach;
            return ToWorld(local);
        }

        // Yarıçapı radius olan bir karakteri (XZ noktası) engellerin dışına iter. Komşu engeller birbirine itmesin
        // diye iki tur yapılır.
        public static Vector2 PushOut(Vector2 point, float radius)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < active.Count; i++) point = active[i].Push(point, radius);
            }
            return point;
        }

        private void OnEnable()
        {
            active.Add(this);
            Version++;
        }

        private void OnDisable()
        {
            active.Remove(this);
            Version++;
        }
    }
}
