// InfoSign.cs
// Kapalı bölgenin ya da köprünün girişindeki ahşap tanıtım tabelası. Oyuncu yaklaşınca tabelanın üstünde küçük bir
// kart açılır: bölge adı, içindeki kaynaklar ve binalar (ikonlarla, ör. Hurda Metal, Demirci → Çivi), gizli sandık
// sayısı ("? ?") ve açma bedeli. Bölge açılınca kart bir daha açılmaz (tabela bölge adıyla kalır).
// Sonraki hedef oku (ProgressionGuide) kapalı bölgeyi gösterirken bu tabelayı da gösterebilir (All).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class InfoSign : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Bu tabelanın tanıttığı bölge (açılınca kart kapanır).")]
        [SerializeField] private AreaDefinition area;
        [Tooltip("Açılan kart (dünyada, kameraya dönük).")]
        [SerializeField] private Transform card;

        private static readonly List<InfoSign> all = new List<InfoSign>();
        public static IReadOnlyList<InfoSign> All => all;

        private Transform player;
        private Vector3 cardScale;
        private bool shown;

        public AreaDefinition Area => area;
        public bool IsCardShown => shown;
        // Kart en az bir kez açıldı mı? (Rehber oku önce tabelayı, sonra açma karesini gösterir.)
        public bool HasBeenSeen { get; private set; }

        public static InfoSign For(AreaDefinition a)
        {
            if (a == null) return null;
            foreach (InfoSign s in all) if (s != null && s.area == a && s.isActiveAndEnabled) return s;
            return null;
        }
        public bool IsUnlocked => area != null && AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(area);

        private void Awake()
        {
            all.Add(this);
            if (card != null)
            {
                cardScale = card.localScale;
                card.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
        }

        private void Update()
        {
            if (card == null || player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            bool want = !IsUnlocked && !PlayerController.InputBlocked && d.sqrMagnitude < config.InfoCardDistance * config.InfoCardDistance;
            if (want == shown) return;
            shown = want;
            if (want)
            {
                card.gameObject.SetActive(true);
                HasBeenSeen = true;
                Tween.PopIn(card, cardScale, 0.3f);
                AudioManager.Play(SoundId.Button, 0.5f);
            }
            else
            {
                Tween.PopOut(card, 0.2f, () => { if (!shown) card.gameObject.SetActive(false); });
            }
        }
    }
}
