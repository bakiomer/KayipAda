// BridgeBuilder.cs
// Adacığa giden aşamalı köprü: köprü başındaki teslim alanına getirilen her kalas köprüde, köprü başından karşıya
// doğru sıradaki tahta olarak yerine oturur (ikincisinde halatlar korkuluk olur). Kalın direkler iki yakaya baştan
// bağlıdır. Köprü başına gelince (inşa bitmeden) kamera kısa süre karşı kıyıyı gösterir (batık, Demirci iskeleti,
// hurda yığınları). Bitince: kamera köprü boyunca karşıya kayar, karşıdaki nesneler zıplayarak belirir,
// "BATIK KOYU AÇILDI!" bandı çıkar, kamera oyuncuya döner; köprünün üstü yürünebilir olur (IslandShape + NavMesh).

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
        [Tooltip("Köprü iskeleti bölümleri (kiriş + direk), kıyıdan karşıya sırayla; tahtalar ilerledikçe belirir.")]
        [SerializeField] private Transform[] frames = new Transform[0];
        [Tooltip("Köprünün iki ucu (kıyıların içinde) ve yarı genişliği: bitince bu şerit yürünebilir olur.")]
        [SerializeField] private Transform endA;
        [SerializeField] private Transform endB;
        [SerializeField] private float halfWidth = 1.2f;
        [Tooltip("Karşı kıyıdaki içerik (köprü bitince çocukları sırayla zıplayarak belirir).")]
        [SerializeField] private Transform revealRoot;
        [Tooltip("Karşı kıyının ortası (kamera buraya bakar).")]
        [SerializeField] private Transform farShore;

        private float previewCooldown;
        private Transform player;
        public bool IsPreviewing { get; private set; }
        public bool IsRevealing { get; private set; }
        public Vector3 FarShore => farShore != null ? farShore.position : EndB;

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
            foreach (Transform t in frames) t.gameObject.SetActive(false);
        }

        // Kaç iskelet bölümü görünür (testler için).
        public int VisibleFrames
        {
            get { int n = 0; foreach (Transform t in frames) if (t != null && t.gameObject.activeSelf) n++; return n; }
        }
        public int FrameCount => frames.Length;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            open.Remove(this);
        }

        protected override void Start()
        {
            base.Start();
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
        }

        // Köprü başına gelen oyuncuya (inşa bitmeden) karşı kıyıyı kısa süre göster.
        private void LateUpdate()
        {
            if (previewCooldown > 0f) previewCooldown -= Time.deltaTime;
            if (IsCompleted || !IsAvailable || player == null || config.BridgePreviewTime <= 0f || previewCooldown > 0f) return;
            if (IsPreviewing || PlayerController.InputBlocked) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 2.2f * 2.2f) return;
            CameraFollow cam = CameraFollow.Main;
            if (cam == null || cam.HasShot) return;
            previewCooldown = config.BridgePreviewCooldown;
            StartCoroutine(Preview(cam));
        }

        private System.Collections.IEnumerator Preview(CameraFollow cam)
        {
            IsPreviewing = true;
            Vector3 look = Vector3.Lerp(EndB, FarShore, 0.6f);
            // Önizleme biraz daha geniş: karşı kıyının tamamı (batık, Demirci iskeleti, hurda) kadraja girsin.
            Vector3 pos = look + new Vector3(0f, config.BridgeShotHeight * 1.45f, -config.BridgeShotDistance * 1.45f);
            cam.SetShot(pos, Quaternion.LookRotation(look - pos), 2.6f);
            yield return new WaitForSeconds(config.BridgePreviewTime);
            if (cam.HasShot && !IsRevealing) cam.ClearShot();
            IsPreviewing = false;
        }

        // Bitiş çekimi: kamera köprü boyunca karşıya kayar, karşıdaki nesneler zıplayarak belirir, bant çıkar.
        private System.Collections.IEnumerator Reveal()
        {
            IsRevealing = true;
            CameraFollow cam = CameraFollow.Main;
            Vector3 offset = new Vector3(0f, config.BridgeShotHeight, -config.BridgeShotDistance);
            Vector3 start = EndA, end = Vector3.Lerp(EndB, FarShore, 0.6f);
            float glide = config.BridgeRevealGlide;
            for (float t = 0f; t < glide; t += Time.deltaTime)
            {
                Vector3 look = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / glide));
                if (cam != null) cam.SetShot(look + offset, Quaternion.LookRotation(-offset), 6f);
                yield return null;
            }
            // Karşı kıyı canlanır: nesneler sırayla zıplayarak gelir.
            if (revealRoot != null)
            {
                int k = 0;
                foreach (Transform child in revealRoot)
                {
                    if (!child.gameObject.activeSelf) continue;
                    Vector3 scale = child.localScale;
                    child.localScale = Vector3.zero;
                    Tween.PopIn(child, scale, config.PopInDuration, 0.08f * k);
                    if (k % 2 == 0 && Effects.Instance != null) Effects.Instance.Dust(child.position);
                    k++;
                }
            }
            if (islandArea != null && CelebrationBanner.Instance != null)
                CelebrationBanner.Instance.Show(TextUtil.UpperTr(islandArea.DisplayName) + " AÇILDI!", "", config.BridgeRevealHold + 0.8f);
            if (Effects.Instance != null) Effects.Instance.Confetti(FarShore + Vector3.up);
            Haptics.Vibrate();
            yield return new WaitForSeconds(config.BridgeRevealHold);
            if (cam != null) cam.ClearShot();
            IsRevealing = false;
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
            // İskelet bölümleri tahtaların ulaştığı yere kadar (bir bölüm önden) görünür.
            if (list == planks && frames.Length > 0)
            {
                int show = Mathf.Min(frames.Length, Mathf.CeilToInt((upTo + 1f) / planks.Length * frames.Length) + 1);
                for (int i = 0; i < show; i++)
                {
                    if (frames[i] == null || frames[i].gameObject.activeSelf) continue;
                    frames[i].gameObject.SetActive(true);
                    if (animate) Tween.PopIn(frames[i], frames[i].localScale, config.PopInDuration);
                }
            }
        }

        protected override void OnCompleted(bool animate)
        {
            foreach (Transform t in planks) t.gameObject.SetActive(true);
            foreach (Transform t in ropes) t.gameObject.SetActive(true);
            foreach (Transform t in frames) t.gameObject.SetActive(true);
            if (!open.Contains(this)) open.Add(this);
            if (IslandShape.Instance != null)
                IslandShape.Instance.OpenBridge(new Vector2(endA.position.x, endA.position.z), new Vector2(endB.position.x, endB.position.z), halfWidth);
            NavObstacle.Touch(); // NavMesh köprüyü eklesin
            if (islandArea != null && AreaManager.Instance != null && !AreaManager.Instance.IsUnlocked(islandArea)) AreaManager.Instance.Unlock(islandArea);
            if (!animate) return;
            AudioManager.Celebrate(CelebrationTier.Big); // adacık açılışı
            if (isActiveAndEnabled) StartCoroutine(Reveal());
        }
    }
}
