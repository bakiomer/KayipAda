// Pier.cs
// Denize uzanan uzun ahşap iskele: üstü baştan sona yürünebilir (oyuncu ucuna kadar gider). Oyun başında
// IslandShape'e yürünebilir şerit olarak eklenir (köprülerle aynı yol).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class Pier : MonoBehaviour
    {
        [SerializeField] private Transform endA;
        [SerializeField] private Transform endB;
        [SerializeField] private float halfWidth = 1f;
        [Tooltip("Güvertenin yüksekliği (görsel; oyuncu zeminde yürür).")]
        [SerializeField] private float deckHeight = 0.1f;

        private static readonly List<Pier> all = new List<Pier>();
        public static IReadOnlyList<Pier> All => all;

        public Vector3 EndA => endA.position;
        public Vector3 EndB => endB.position;
        public float HalfWidth => halfWidth;

        private void Awake() => all.Add(this);
        private void OnDestroy() => all.Remove(this);

        private void Start()
        {
            if (IslandShape.Instance != null)
                IslandShape.Instance.OpenBridge(new Vector2(endA.position.x, endA.position.z), new Vector2(endB.position.x, endB.position.z), halfWidth);
        }
    }
}
