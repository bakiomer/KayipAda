// EscapePanel.cs
// Kaçış ekranı: "KAÇTIN!", geçen süre, kazanılan kaptanlık XP'si ve seviyesi, "Yeni adalar yakında", "Tekrar oyna".
// "Tekrar oyna" adadaki ilerlemeyi sıfırlar ama kaptanlık seviyesi kalır.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class EscapePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private Button replayButton;

        public bool IsShown => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
        }

        public void Show()
        {
            PlayerController.InputBlocked = true;
            SaveManager save = SaveManager.Instance;
            float seconds = save != null ? save.PlayTime : Time.timeSinceLevelLoad;
            int xp = save != null ? save.RegisterEscape() : 0;
            int level = save != null ? save.CaptainLevel : 0;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            timeLabel.text = "Süre: " + minutes + " dk " + Mathf.FloorToInt(seconds % 60f) + " sn";
            xpLabel.text = "+" + xp + " Kaptanlık XP\nKaptanlık seviyesi: " + level;

            root.SetActive(true);
            Tween.PopIn(root.transform, Vector3.one, 0.5f);
            if (Effects.Instance != null && Camera.main != null)
            {
                Effects.Instance.Confetti(Camera.main.transform.position + Camera.main.transform.forward * 12f);
            }
        }

        private void Replay()
        {
            AudioManager.Play(SoundId.Button);
            if (SaveManager.Instance != null) SaveManager.Instance.RestartRun();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
