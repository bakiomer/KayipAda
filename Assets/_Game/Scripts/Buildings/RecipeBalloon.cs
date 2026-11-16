// RecipeBalloon.cs
// Binanın üstündeki büyük tarif balonu: "3 [lif ikonu] → 1 [halat ikonu]". Oyuncu binaya yaklaşınca zıplayarak
// açılır, uzaklaşınca küçülerek kapanır (GameConfig.RecipeShowDistance). Sinematik çekimde gizlenir.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class RecipeBalloon : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private ProcessingBuilding building;
        [Tooltip("Açılıp kapanan görsel kök.")]
        [SerializeField] private RectTransform visual;
        [SerializeField] private TMP_Text inputCount;
        [SerializeField] private Image inputIcon;
        [SerializeField] private TMP_Text outputCount;
        [SerializeField] private Image outputIcon;

        private Transform player;
        private float shown; // 0..1

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            Refresh();
            shown = 0f;
            visual.localScale = Vector3.zero;
        }

        // Tarif metnini binanın türünden yazar (ayar değişirse de doğru kalsın).
        public void Refresh()
        {
            if (building == null || building.Type == null) return;
            BuildingType type = building.Type;
            inputCount.text = type.InputAmount.ToString();
            outputCount.text = type.OutputAmount.ToString();
            if (type.Input != null) inputIcon.sprite = type.Input.Icon;
            if (type.Output != null) outputIcon.sprite = type.Output.Icon;
        }

        private void LateUpdate()
        {
            bool near = false;
            if (player != null && building != null && building.isActiveAndEnabled)
            {
                Vector3 d = player.position - transform.position;
                d.y = 0f;
                near = d.magnitude < config.RecipeShowDistance;
            }
            CameraFollow follow = CameraFollow.Main;
            if (follow != null && follow.HasShot) near = false;

            float target = near ? 1f : 0f;
            if (Mathf.Approximately(shown, target)) return;
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 4f);
            float s = near ? Tween.EaseOutBack(shown) : shown;
            visual.localScale = Vector3.one * s;
        }
    }
}
