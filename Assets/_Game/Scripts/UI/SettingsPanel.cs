// SettingsPanel.cs
// Sağ üstteki küçük "AYAR" butonu: basınca altında küçük panel açılır. Titreşim ve ses aç/kapa
// (tercihler cihazda saklanır: Haptics.Enabled, AudioManager.SoundOn). Tekrar basınca kapanır.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button soundButton;

        private static readonly Color OnColor = new Color(0.36f, 0.78f, 0.36f);
        private static readonly Color OffColor = new Color(0.45f, 0.45f, 0.5f);

        private void Awake()
        {
            panel.SetActive(false);
            toggleButton.onClick.AddListener(() =>
            {
                AudioManager.Play(SoundId.Button);
                panel.SetActive(!panel.activeSelf);
                Refresh();
            });
            vibrationButton.onClick.AddListener(() =>
            {
                Haptics.Enabled = !Haptics.Enabled;
                AudioManager.Play(SoundId.Button);
                Haptics.Vibrate();
                Refresh();
            });
            soundButton.onClick.AddListener(() =>
            {
                AudioManager.SoundOn = !AudioManager.SoundOn;
                AudioManager.Play(SoundId.Button);
                Refresh();
            });
        }

        private void Refresh()
        {
            Set(vibrationButton, "Titreşim: ", Haptics.Enabled);
            Set(soundButton, "Ses: ", AudioManager.SoundOn);
        }

        private static void Set(Button button, string label, bool on)
        {
            button.GetComponentInChildren<TMP_Text>().text = label + (on ? "AÇIK" : "KAPALI");
            button.GetComponent<Image>().color = on ? OnColor : OffColor;
        }
    }
}
