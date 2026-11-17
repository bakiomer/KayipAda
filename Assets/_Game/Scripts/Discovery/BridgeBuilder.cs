// BridgeBuilder.cs
// Adacığa giden aşamalı köprü: köprü başındaki teslim alanına getirilen her kalas köprüde sıradaki tahta olarak
// yerine oturur (ikincisinde halatlar korkuluk olur). "Köprü: 18/40". Bitince adacık bölgesi açılır (bulut dağılır),
// köprünün üstü yürünebilir olur (IslandShape + NavMesh) ve kısa bir kutlama yapılır.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class BridgeBuilder : ConstructionSite
    {
        [Tooltip("Köprü bitince açılan bölge (adacık).")]
        [SerializeField] private AreaDefinition islandArea;
        [Tooltip("Köprünün tahtaları (sırayla belirir).")]
        [SerializeField] private Transform[] planks = new Transform[0];
        [Tooltip("Korkuluk halatları (halat teslim edildikçe belirir; boş olabilir).")]
        [SerializeField] private Transform[] ropes = new Transform[0];
        [Tooltip("Köprünün iki ucu (kıyıların içinde) ve yarı genişliği: bitince bu şerit yürünebilir olur.")]
        [SerializeField] private Transform endA;
        [SerializeField] private Transform endB;
        [SerializeField] private float halfWidth = 1.2f;

        private static readonly List<BridgeBuilder> open = new List<BridgeBuilder>();

        // Tamamlanmış köprüler (NavMesh bunları yürünebilir alan olarak ekler).
        public static IReadOnlyList<BridgeBuilder> Open => open;
        public Vector3 EndA => endA.position;
        public Vector3 EndB => endB.position;
        public float HalfWidth => halfWidth;
        public AreaDefinition IslandArea => islandArea;

        protected override void Awake()
        {
            base.Awake();
            foreach (Transform t in planks) t.gameObject.SetActive(false);
            foreach (Transform t in ropes) t.gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            open.Remove(this);
        }

        protected override Vector3 PiecePosition(ResourceType type, int index)
        {
            Transform[] list = IsRope(type) ? ropes : planks;
            if (list.Length == 0) return base.PiecePosition(type, index);
            return list[Mathf.Clamp(PieceIndex(type, index, list.Length), 0, list.Length - 1)].position;
        }

        private bool IsRope(ResourceType type) => ropes.Length > 0 && Requirements.Count > 1 && type == Requirements[1].Resource;

        // Teslim edilen sayıyı parça sayısına yayar (40 kalas → 20 tahta da olsa sırayla dolar).
        private int PieceIndex(ResourceType type, int index, int pieceCount)
        {
            int amount = 1;
            foreach (Requirement r in Requirements) if (r.Resource == type) amount = r.Amount;
            return Mathf.FloorToInt((index + 1f) / amount * pieceCount) - 1;
        }

        protected override void OnPieceAdded(ResourceType type, int index, bool animate)
        {
            Transform[] list = IsRope(type) ? ropes : planks;
            if (list.Length == 0) return;
            int upTo = PieceIndex(type, index, list.Length);
            for (int i = 0; i <= upTo && i < list.Length; i++)
            {
                if (list[i].gameObject.activeSelf) continue;
                list[i].gameObject.SetActive(true);
                if (animate) Tween.PopIn(list[i], list[i].localScale, config.PopInDuration);
            }
        }

        protected override void OnCompleted(bool animate)
        {
            foreach (Transform t in planks) t.gameObject.SetActive(true);
            foreach (Transform t in ropes) t.gameObject.SetActive(true);
            if (!open.Contains(this)) open.Add(this);
            if (IslandShape.Instance != null)
                IslandShape.Instance.OpenBridge(new Vector2(endA.position.x, endA.position.z), new Vector2(endB.position.x, endB.position.z), halfWidth);
            NavObstacle.Touch(); // NavMesh köprüyü eklesin
            if (islandArea != null && AreaManager.Instance != null && !AreaManager.Instance.IsUnlocked(islandArea)) AreaManager.Instance.Unlock(islandArea);
            if (!animate) return;
            Vector3 mid = (endA.position + endB.position) * 0.5f;
            if (Effects.Instance != null) Effects.Instance.Confetti(mid + Vector3.up);
            if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show(DisplayName + " tamamlandı!", islandArea != null ? islandArea.DisplayName + " açıldı" : "", 2.2f);
            AudioManager.Play(SoundId.StageComplete);
            Haptics.Vibrate();
        }
    }
}
