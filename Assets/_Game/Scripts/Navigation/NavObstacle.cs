// NavObstacle.cs
// Karakterlerin içinden geçmediği dikdörtgen engel (bina gövdesi, çadır, ağaç gövdesi), yerde (XZ düzlemi).
// İşçiler etrafından dolaşır (Pathfinder), oyuncu ise kenarına çarpıp kayar (PushOut).
// Açık (aktif) olan tüm engeller tek bir listede tutulur; değişince Version artar ve yürüyen işçiler
// yollarını yeniden hesaplar. Not: Dönüş dikkate alınmaz (eksene hizalı dikdörtgen).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class NavObstacle : MonoBehaviour
    {
        [Tooltip("Engelin genişliği (X) ve derinliği (Z), metre. Nesnenin konumu ortadadır.")]
        [SerializeField] private Vector2 size = Vector2.one;

        private static readonly List<NavObstacle> active = new List<NavObstacle>();

        public static IReadOnlyList<NavObstacle> Active => active;

        // Engel listesi her değiştiğinde artar.
        public static int Version { get; private set; }

        // Engelin XZ dikdörtgeni, her yandan "margin" kadar büyütülmüş olarak.
        public Rect GetRect(float margin)
        {
            Vector2 grown = size + Vector2.one * (margin * 2f);
            Vector2 center = new Vector2(transform.position.x, transform.position.z);
            return new Rect(center - grown / 2f, grown);
        }

        // Yarıçapı radius olan bir karakteri (XZ noktası) engellerin dışına iter.
        public static Vector2 PushOut(Vector2 point, float radius)
        {
            for (int i = 0; i < active.Count; i++)
            {
                Rect r = active[i].GetRect(0f);
                Vector2 closest = new Vector2(Mathf.Clamp(point.x, r.xMin, r.xMax), Mathf.Clamp(point.y, r.yMin, r.yMax));
                Vector2 away = point - closest;
                float distance = away.magnitude;

                if (distance > 0.0001f)
                {
                    if (distance < radius) point = closest + away / distance * radius;
                    continue;
                }

                // Merkez dikdörtgenin içinde: en yakın kenardan dışarı çıkar.
                float left = point.x - r.xMin, right = r.xMax - point.x, down = point.y - r.yMin, up = r.yMax - point.y;
                float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
                if (min == left) point.x = r.xMin - radius;
                else if (min == right) point.x = r.xMax + radius;
                else if (min == down) point.y = r.yMin - radius;
                else point.y = r.yMax + radius;
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
