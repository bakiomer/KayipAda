// ShipProgressView.cs
// Gemi teslim alanının üstündeki aşama göstergesi (kutusuz): üstte aşamanın adı ("GÖVDE 2/5"), altında her
// malzeme için ikon + "12/30" + ince dolum çubuğu. Kameraya dönük; uzakta yumuşakça kaybolur (LabelFader).
// ShipBuilder değer değişince Show çağırır (her karede güncellemez).

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class ShipProgressView : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            public GameObject Root;
            public Image Icon;
            public TMP_Text Count;
            public Image Fill;
        }

        [SerializeField] private TMP_Text title;
        [SerializeField] private Row[] rows = new Row[0];

        public string TitleText => title != null ? title.text : "";

        public void Show(string stageTitle, ResourceType[] resources, int[] have, int[] need)
        {
            if (title != null) title.text = stageTitle;
            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                bool used = resources != null && i < resources.Length;
                row.Root.SetActive(used);
                if (!used) continue;
                row.Icon.sprite = resources[i].Icon;
                row.Count.text = have[i] + "/" + need[i];
                row.Fill.fillAmount = need[i] > 0 ? Mathf.Clamp01((float)have[i] / need[i]) : 1f;
            }
        }

        // Gemi bitti: sadece başlık.
        public void ShowDone(string text)
        {
            if (title != null) title.text = text;
            foreach (Row row in rows) row.Root.SetActive(false);
        }
    }
}
