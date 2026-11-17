// Collectible.cs
// Yerde süzülüp dönen, parlayan özel eşya. İki tür:
// - Koleksiyon (eski pusula, şişede mektup, altın kafatası, kaptan dürbünü, hazine haritası parçası): alınınca ekranda
//   dönen 3D görünüşü ve "Koleksiyon 2/5" çıkar; profilde kalır (yeniden oynayınca soluk görünür, alınamaz).
//   5/5 olunca bir kez ek kristal.
// - Altın figür parçası (tapınak): bu adadaki ilerleme; 3'ü bulununca gemiye altın figür takılır (QuestManager).

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class Collectible : MonoBehaviour
    {
        public enum Kind { Collection, FigurePart }

        [SerializeField] private GameConfig config;
        [SerializeField] private Kind kind;
        [Tooltip("Kayıt kimliği (küçük harf).")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "Eşya";
        [Tooltip("Dönen/süzülen görünüş.")]
        [SerializeField] private Transform visual;
        [Tooltip("Koleksiyon ekranında dönen model (prefab).")]
        [SerializeField] private GameObject showcaseModel;
        [SerializeField] private GameObject glow;

        private static readonly List<Collectible> all = new List<Collectible>();
        public static IReadOnlyList<Collectible> All => all;
        public static event Action<Collectible> PickedUp;

        private Transform player;
        private Vector3 basePos;
        private bool taken;

        public string Id => id;
        public string DisplayName => displayName;
        public Kind Type => kind;
        public GameObject ShowcaseModel => showcaseModel;
        public bool IsTaken => taken;

        private void Awake()
        {
            all.Add(this);
            if (visual != null) basePos = visual.localPosition;
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            if (kind == Kind.Collection && ProfileManager.Instance != null && ProfileManager.Instance.HasCollectible(id))
            {
                taken = true; // daha önce bulundu: soluk, alınamaz
                if (glow != null) glow.SetActive(false);
                Faded.Apply(visual.gameObject, config.FoundItemTint);
            }
            else if (kind == Kind.FigurePart && QuestManager.Instance != null && QuestManager.Instance.HasFigurePart(id))
            {
                taken = true;
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (visual != null)
            {
                float t = Time.time;
                visual.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 2f) * 0.12f);
                visual.localRotation = Quaternion.Euler(0f, t * 70f, 0f);
            }
            if (taken || player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < config.CollectibleDistance * config.CollectibleDistance) Take();
        }

        public void Take()
        {
            if (taken) return;
            taken = true;
            AudioManager.Play(SoundId.UnlockJingle);
            Haptics.Vibrate();
            if (Effects.Instance != null) Effects.Instance.Sparkle(transform.position + Vector3.up);
            if (kind == Kind.Collection)
            {
                ProfileManager profile = ProfileManager.Instance;
                if (profile != null && profile.MarkCollectible(id))
                {
                    int total = CountOfKind(Kind.Collection);
                    if (CollectionPopup.Instance != null) CollectionPopup.Instance.Show(this, profile.CollectionCount, total);
                    if (profile.CollectionCount >= total && !profile.CollectionRewarded)
                    {
                        profile.CollectionRewarded = true;
                        profile.AddCrystals(config.CollectionBonusCrystals);
                    }
                }
                Faded.Apply(visual.gameObject, config.FoundItemTint);
                if (glow != null) glow.SetActive(false);
            }
            else
            {
                if (QuestManager.Instance != null) QuestManager.Instance.AddFigurePart(id);
                Tween.PopOut(transform, 0.3f);
            }
            PickedUp?.Invoke(this);
        }

        public static int CountOfKind(Kind k)
        {
            int n = 0;
            foreach (Collectible c in all) if (c != null && c.kind == k) n++;
            return n;
        }
    }
}
