// QuestCard.cs
// Sol üstteki yan görev kartı: başlık, ilerleme ("12/20" + ince çubuk) ve ödül (para / kristal ikonları).
// Görev gelince soldan kayarak girer; bitince yeşile döner, zıplar ve sola kayıp çıkar.

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
        [SerializeField] private Color normal = new Color(0.1f, 0.13f, 0.19f, 0.88f);
        [SerializeField] private Color done = new Color(0.25f, 0.6f, 0.25f, 0.95f);

        private QuestDefinition quest;
        private float shownX;
        private float animTime = -1f;
        private bool leaving;

        public bool IsPlaying => animTime >= 0f;
        public QuestDefinition Quest => quest;

        private void Awake()
        {
            shownX = body.anchoredPosition.x;
            body.gameObject.SetActive(false);
        }

        public void Bind(QuestDefinition q, int progress)
        {
            bool isNew = q != quest || !body.gameObject.activeSelf;
            quest = q;
            title.text = q.Title;
            coinIcon.SetActive(q.RewardCoins > 0);
            coinText.text = q.RewardCoins > 0 ? q.RewardCoins.ToString() : "";
            crystalIcon.SetActive(q.RewardCrystals > 0);
            crystalText.text = q.RewardCrystals > 0 ? q.RewardCrystals.ToString() : "";
            background.color = normal;
            Refresh(progress);
            body.gameObject.SetActive(true);
            if (!isNew) return;
            leaving = false;
            animTime = 0f; // soldan kayarak gelir
        }

        public void Refresh(int progress)
        {
            if (quest == null) return;
            count.text = Mathf.Min(progress, quest.Amount) + "/" + quest.Amount;
            fill.fillAmount = Mathf.Clamp01(progress / (float)quest.Amount);
        }

        public void Clear()
        {
            if (IsPlaying && leaving) return;
            quest = null;
            body.gameObject.SetActive(false);
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
            if (leaving) { leaving = false; quest = null; body.gameObject.SetActive(false); }
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
