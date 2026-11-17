// QuestCard.cs
// Sol üstteki yan görev kartı: solda görev türünün net ikonu, büyük başlık, ilerleme ("12/20" + ince çubuk) ve ödül
// (para / kristal ikonları). Dokununca küçülüp tek satıra iner (ikon + başlık + "12/20"); tekrar dokununca açılır.
// Kartlar alt alta dizilir (küçülen kartın altındaki yukarı kayar). Görev gelince soldan kayarak girer; bitince yeşile
// döner, zıplar ve sola kayıp çıkar.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class QuestCard : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text count;
        [SerializeField] private Image fill;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private GameObject crystalIcon;
        [SerializeField] private TMP_Text crystalText;
        [SerializeField] private Image icon;
        [Tooltip("Açıkken görünen kısım (çubuk, sayı, ödüller).")]
        [SerializeField] private GameObject details;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private float expandedHeight = 132f;
        [SerializeField] private float collapsedHeight = 66f;
        [Header("Görev türü ikonları (kaynağı olmayan görevler)")]
        [SerializeField] private Sprite sellIcon;
        [SerializeField] private Sprite repairIcon;
        [SerializeField] private Sprite chestIcon;
        [SerializeField] private Sprite exploreIcon;
        [SerializeField] private Sprite figureIcon;
        [SerializeField] private Color normal = new Color(0.1f, 0.13f, 0.19f, 0.88f);
        [SerializeField] private Color done = new Color(0.25f, 0.6f, 0.25f, 0.95f);

        private QuestDefinition quest;
        private float shownX;
        private float animTime = -1f;
        private bool leaving;
        private bool collapsed;
        private int lastProgress;

        public bool IsCollapsed => collapsed;
        public string TitleText => title != null ? title.text : "";

        public bool IsPlaying => animTime >= 0f;
        public QuestDefinition Quest => quest;

        private void Awake()
        {
            shownX = body.anchoredPosition.x;
            body.gameObject.SetActive(false);
            Button button = body.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Toggle);
            ApplyLayout();
        }

        // Dokununca küçül / büyü.
        public void Toggle()
        {
            collapsed = !collapsed;
            AudioManager.Play(SoundId.Button, 0.5f);
            ApplyLayout();
            Tween.PunchScale(body, 0.05f, 0.2f);
        }

        private void ApplyLayout()
        {
            bool visible = body.gameObject.activeSelf;
            if (layout != null) layout.preferredHeight = !visible ? 0f : collapsed ? collapsedHeight : expandedHeight;
            if (details != null) details.SetActive(!collapsed);
            if (icon != null) icon.rectTransform.sizeDelta = Vector2.one * (collapsed ? 50f : 76f);
            if (quest == null || title == null) return;
            int shown = Mathf.Min(lastProgress, quest.Amount);
            title.text = collapsed ? quest.Title + "  <color=#FFD54A>" + shown + "/" + quest.Amount + "</color>" : quest.Title;
        }

        private Sprite IconFor(QuestDefinition q)
        {
            if (q.Resource != null && q.Resource.Icon != null && (q.Kind == QuestKind.Collect || q.Kind == QuestKind.Produce)) return q.Resource.Icon;
            switch (q.Kind)
            {
                case QuestKind.Sell: return sellIcon;
                case QuestKind.Repair: return repairIcon;
                case QuestKind.FindChest: return chestIcon;
                case QuestKind.Explore: return exploreIcon;
                case QuestKind.FigurePart: return figureIcon;
                default: return q.Resource != null ? q.Resource.Icon : sellIcon;
            }
        }

        public void Bind(QuestDefinition q, int progress)
        {
            bool isNew = q != quest || !body.gameObject.activeSelf;
            quest = q;
            title.text = q.Title;
            if (icon != null) icon.sprite = IconFor(q);
            coinIcon.SetActive(q.RewardCoins > 0);
            coinText.text = q.RewardCoins > 0 ? q.RewardCoins.ToString() : "";
            crystalIcon.SetActive(q.RewardCrystals > 0);
            crystalText.text = q.RewardCrystals > 0 ? q.RewardCrystals.ToString() : "";
            background.color = normal;
            Refresh(progress);
            body.gameObject.SetActive(true);
            ApplyLayout();
            if (!isNew) return;
            leaving = false;
            animTime = 0f; // soldan kayarak gelir
        }

        public void Refresh(int progress)
        {
            if (quest == null) return;
            lastProgress = progress;
            count.text = Mathf.Min(progress, quest.Amount) + "/" + quest.Amount;
            fill.fillAmount = Mathf.Clamp01(progress / (float)quest.Amount);
            if (collapsed) ApplyLayout();
        }

        public void Clear()
        {
            if (IsPlaying && leaving) return;
            quest = null;
            body.gameObject.SetActive(false);
            ApplyLayout();
        }

        // Bitti: yeşil + zıplama, sonra sola kayıp çıkar.
        public void PlayComplete()
        {
            background.color = done;
            fill.fillAmount = 1f;
            Tween.PunchScale(body, 0.12f, 0.35f);
            leaving = true;
            animTime = 0f;
        }

        public void StopAnimation()
        {
            animTime = -1f;
            if (leaving) { leaving = false; quest = null; body.gameObject.SetActive(false); ApplyLayout(); }
            body.anchoredPosition = new Vector2(shownX, body.anchoredPosition.y);
        }

        private void Update()
        {
            if (animTime < 0f) return;
            animTime += Time.unscaledDeltaTime;
            float width = body.rect.width + 60f;
            if (!leaving)
            {
                float k = Mathf.Clamp01(animTime / 0.35f);
                body.anchoredPosition = new Vector2(shownX - width * (1f - Tween.EaseOutBack(k)), body.anchoredPosition.y);
                if (k >= 1f) animTime = -1f;
                return;
            }
            // Bitiş: 0.9 sn yeşil bekler, sonra kayar.
            float t = Mathf.Clamp01((animTime - 0.9f) / 0.3f);
            body.anchoredPosition = new Vector2(shownX - width * t * t, body.anchoredPosition.y);
            if (t >= 1f) StopAnimation();
        }
    }
}
