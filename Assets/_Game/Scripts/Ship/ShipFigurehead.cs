// ShipFigurehead.cs
// Geminin burnundaki altın figür: tapınaktaki 3 altın figür parçası bulununca (QuestManager) ve gövde yapılmışsa
// parlayarak belirir. Finalde görünür; kaçış ekranında "EFSANEVİ GEMİ!" rozeti ve ek kristal verir (EscapePanel).

using UnityEngine;

namespace KayipAda
{
    public class ShipFigurehead : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject figure;
        [Tooltip("Figür bu aşama bitince (gövde) takılabilir.")]
        [SerializeField] private int afterStage = 2;

        private bool shown;

        private void Start()
        {
            figure.SetActive(false);
            if (QuestManager.Instance != null) QuestManager.Instance.FigureChanged += Refresh;
            if (ShipBuilder.Instance != null) ShipBuilder.Instance.StageCompleted += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null) QuestManager.Instance.FigureChanged -= Refresh;
            if (ShipBuilder.Instance != null) ShipBuilder.Instance.StageCompleted -= Refresh;
        }

        private void Refresh()
        {
            bool legendary = QuestManager.Instance != null && QuestManager.Instance.HasLegendaryFigure;
            bool hull = ShipBuilder.Instance == null || ShipBuilder.Instance.StageIndex >= afterStage;
            bool show = legendary && hull;
            if (show == shown) return;
            shown = show;
            figure.SetActive(show);
            if (!show || Time.timeSinceLevelLoad < 1f) return;
            Tween.PopIn(figure.transform, figure.transform.localScale, config.PopInDuration);
            if (Effects.Instance != null) Effects.Instance.GoldBurst(figure.transform.position);
        }
    }
}
