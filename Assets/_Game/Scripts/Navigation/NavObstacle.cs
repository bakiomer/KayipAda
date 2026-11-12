// NavObstacle.cs
// İşçilerin içinden geçmeyip etrafından dolaştığı dikdörtgen engel (bina gövdesi, kamp çadırı).
// Açık (aktif) olan tüm engeller tek bir listede tutulur; Pathfinder yol ararken bu listeye bakar.
// Bir engel açılıp kapanınca Version artar, yürüyen işçiler yollarını yeniden hesaplar.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class NavObstacle : MonoBehaviour
    {
        [Tooltip("Engelin genişliği ve yüksekliği (dünya birimi), nesnenin konumu ortadadır.")]
        [SerializeField] private Vector2 size = Vector2.one;

        private static readonly List<NavObstacle> active = new List<NavObstacle>();

        public static IReadOnlyList<NavObstacle> Active => active;

        // Engel listesi her değiştiğinde artar.
        public static int Version { get; private set; }

        // Engelin dikdörtgeni, her yandan "margin" kadar büyütülmüş olarak.
        public Rect GetRect(float margin)
        {
            Vector2 grown = size + Vector2.one * (margin * 2f);
            return new Rect((Vector2)transform.position - grown / 2f, grown);
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
