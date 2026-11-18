// QuestBoardPanel.cs
// Kamptaki ahşap görev panosu: oyuncu panonun önünde durunca alttan panel açılır (yürümeyi engellemez),
// hareket edince / uzaklaşınca / X / geri tuşuyla kapanır.
// Üstte aktif yan görevler (başlık + "12/20"), altta tamamlananlar (✓ başlık, soluk). Ekrandaki görev kartları da kalır.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class QuestBoardPanel : MonoBehaviour
    {
        public static QuestBoardPanel Instance { get; private set; }

        [SerializeField] private RectTransform sheet;
        [Tooltip("Sağ üstteki X butonu.")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text activeText;
        [SerializeField] private TMP_Text doneText;
        [SerializeField] private TMP_Text title;

        private float shownY, hiddenY;
        private Transform board;

        public bool IsOpen { get; private set; }
        public string ActiveText => activeText != null ? activeText.text : "";
        public string DoneText => doneText != null ? doneText.text : "";

        private void Awake()
        {
            Instance = this;
            shownY = sheet.anchoredPosition.y;
            hiddenY = shownY - sheet.rect.height - 80f;
            sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, hiddenY);
            sheet.gameObject.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(() => Close(null));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(Transform from)
        {
            board = from;
            Refresh();
            if (IsOpen) return;
            IsOpen = true;
            sheet.gameObject.SetActive(true);
            AudioManager.Play(SoundId.Button);
            Slide(hiddenY, shownY);
        }

        public void Close(Transform from)
        {
            if (!IsOpen || (from != null && from != board)) return;
            IsOpen = false;
            Slide(sheet.anchoredPosition.y, hiddenY, () => { if (!IsOpen) sheet.gameObject.SetActive(false); });
        }

        private void Update()
        {
            if (IsOpen && (PlayerController.InputBlocked || PanelZone.BackPressed())) Close(null);
        }

        public void Refresh()
        {
            QuestManager q = QuestManager.Instance;
            if (q == null) return;
            System.Text.StringBuilder a = new System.Text.StringBuilder(), d = new System.Text.StringBuilder();
            foreach (QuestDefinition def in q.Active)
                a.Append("• ").Append(def.Title).Append("  <color=#FFD54A>").Append(Mathf.Min(q.Progress(def), def.Amount)).Append('/').Append(def.Amount).Append("</color>\n");
            int done = 0;
            foreach (QuestDefinition def in q.All)
            {
                if (!q.IsCompleted(def)) continue;
                d.Append("✓ ").Append(def.Title).Append('\n');
                done++;
            }
            activeText.text = a.Length > 0 ? a.ToString().TrimEnd() : "Şu an görev yok";
            doneText.text = done > 0 ? d.ToString().TrimEnd() : "Henüz tamamlanan görev yok";
            if (title != null) title.text = "GÖREV PANOSU  " + done + "/" + q.All.Count;
        }

        private void Slide(float from, float to, System.Action finished = null)
        {
            Tween.Value(this, 0.25f, t =>
            {
                float y = Mathf.LerpUnclamped(from, to, Tween.EaseOutBack(t));
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, y);
            }, finished);
        }
    }
}
