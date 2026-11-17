// StoragePanel.cs
// Ambar paneli: oyuncu Ambar'ın alma alanında durunca alttan kayarak açılır (yürümeyi engellemez), çıkınca kapanır.
// Ambardaki her kaynak bir kutu: ikon + miktar (boş olanlar soluk). Kutuya dokununca seçilir; "Al" ile seçilen
// kaynaktan oyuncunun sırtında yer kalana kadar alınır. Üstte "AMBAR 37/100".

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class StoragePanel : MonoBehaviour
    {
        public static StoragePanel Instance { get; private set; }

        [System.Serializable]
        public class Slot
        {
            public Button Button;
            public Image Icon;
            public TMP_Text Count;
            public Image Frame;
        }

        [SerializeField] private GameConfig config;
        [SerializeField] private RectTransform sheet;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Slot[] slots = new Slot[0];
        [SerializeField] private Button takeButton;
        [SerializeField] private TMP_Text takeText;
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.25f);
        [SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 0.12f);

        private StoragePickZone zone;
        private CarryStack stack;
        private int selected = -1;
        private float shownY, hiddenY;
        private bool taking;
        private float takeTimer;

        public bool IsOpen { get; private set; }
        public ResourceType Selected => zone != null && selected >= 0 && selected < zone.Storage.Types.Count ? zone.Storage.Types[selected] : null;

        private void Awake()
        {
            Instance = this;
            shownY = sheet.anchoredPosition.y;
            hiddenY = shownY - sheet.rect.height - 80f;
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i].Button.onClick.AddListener(() => Select(index));
            }
            takeButton.onClick.AddListener(Take);
            sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, hiddenY);
            sheet.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (zone != null && zone.Storage != null) zone.Storage.Changed -= Refresh;
        }

        public void Open(StoragePickZone from, CarryStack player)
        {
            if (zone != null && zone.Storage != null) zone.Storage.Changed -= Refresh;
            zone = from;
            stack = player;
            zone.Storage.Changed += Refresh;
            if (Selected == null || zone.Storage.CountOf(Selected) == 0) selected = FirstAvailable();
            Refresh();
            if (IsOpen) return;
            IsOpen = true;
            sheet.gameObject.SetActive(true);
            AudioManager.Play(SoundId.Button);
            Slide(hiddenY, shownY);
        }

        public void Close(StoragePickZone from)
        {
            if (!IsOpen || (from != null && from != zone)) return;
            IsOpen = false;
            taking = false;
            if (zone != null && zone.Storage != null) zone.Storage.Changed -= Refresh;
            Slide(sheet.anchoredPosition.y, hiddenY, () => { if (!IsOpen) sheet.gameObject.SetActive(false); });
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (zone == null || !zone.isActiveAndEnabled || PlayerController.InputBlocked) { Close(zone); return; }
            // "Al": seçilen kaynaktan sırt dolana / ambarda bitene kadar tek tek uçar.
            if (!taking) return;
            if (takeTimer > 0f) { takeTimer -= Time.deltaTime; return; }
            if (Selected == null || !zone.GiveTo(stack, Selected)) { taking = false; Refresh(); return; }
            takeTimer = config.StorageTransferInterval;
        }

        public void Select(int index)
        {
            if (zone == null || index < 0 || index >= zone.Storage.Types.Count) return;
            selected = index;
            AudioManager.Play(SoundId.Button, 0.5f);
            if (index < slots.Length) Tween.PunchScale(slots[index].Button.transform, 0.1f, 0.2f);
            Refresh();
        }

        public void Take()
        {
            if (Selected == null || zone.Storage.CountOf(Selected) == 0 || stack == null || stack.IsFull)
            {
                Tween.Shake(takeButton.transform, 5f, 0.25f);
                return;
            }
            taking = true;
            takeTimer = 0f;
        }

        // Test için: seçili kaynaktan anında alınabilecek kadar al (sırt dolana kadar).
        public int TakeAllNow()
        {
            int n = 0;
            while (Selected != null && zone.GiveTo(stack, Selected)) n++;
            Refresh();
            return n;
        }

        private int FirstAvailable()
        {
            for (int i = 0; i < zone.Storage.Types.Count; i++) if (zone.Storage.CountOf(zone.Storage.Types[i]) > 0) return i;
            return -1;
        }

        public void Refresh()
        {
            if (zone == null) return;
            Storage s = zone.Storage;
            title.text = "AMBAR  " + s.Total + "/" + s.Capacity;
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < s.Types.Count && s.Types[i] != null;
                slots[i].Button.gameObject.SetActive(used);
                if (!used) continue;
                int count = s.CountOf(s.Types[i]);
                slots[i].Icon.sprite = s.Types[i].Icon;
                slots[i].Icon.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                slots[i].Count.text = count.ToString();
                slots[i].Frame.color = i == selected ? selectedColor : frameColor;
            }
            ResourceType sel = Selected;
            int free = stack != null ? stack.Capacity - stack.Count : 0;
            int can = sel != null ? Mathf.Min(free, s.CountOf(sel)) : 0;
            takeText.text = sel != null ? "Al  " + can + " " + sel.DisplayName : "Kaynak seç";
            ((Graphic)takeButton.targetGraphic).color = can > 0 ? new Color(0.36f, 0.75f, 0.3f) : new Color(0.45f, 0.45f, 0.45f);
        }

        private void Slide(float from, float to, System.Action done = null)
        {
            Tween.Value(this, 0.25f, t =>
            {
                float y = Mathf.LerpUnclamped(from, to, Tween.EaseOutBack(t));
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, y);
            }, done);
        }
    }
}
