// PlayerCostume.cs
// Oyuncunun giydiği karakter kostümünü uygular: kostümün modeli (animasyonlu) varsayılan modelin yerine geçer,
// renk tonu (ör. altın kaptan) tüm parçalara uygulanır. Kostüm değişince (ProfileManager) hemen güncellenir.

using UnityEngine;

namespace KayipAda
{
    public class PlayerCostume : MonoBehaviour
    {
        [Tooltip("Modelin altında durduğu, yürüme yönüne dönen gövde (Body).")]
        [SerializeField] private Transform body;
        [SerializeField] private CharacterVisual visual;
        [Tooltip("Varsayılan model (Body/Model).")]
        [SerializeField] private GameObject defaultModel;

        private GameObject current;
        private string appliedId;

        public string AppliedId => appliedId;

        private void Start()
        {
            if (ProfileManager.Instance == null) return;
            ProfileManager.Instance.CostumesChanged += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (ProfileManager.Instance != null) ProfileManager.Instance.CostumesChanged -= Apply;
        }

        private void Apply()
        {
            CostumeDefinition costume = ProfileManager.Instance.Equipped(CostumeSlot.Character);
            string id = costume != null ? costume.Id : "";
            if (id == appliedId) return;
            appliedId = id;

            if (current != null) Destroy(current);
            current = null;
            GameObject model = defaultModel;
            if (costume != null && costume.CharacterModel != null)
            {
                current = Instantiate(costume.CharacterModel, body, false);
                current.name = "CostumeModel";
                model = current;
            }
            defaultModel.SetActive(model == defaultModel);
            Faded.Clear(defaultModel);
            if (costume != null && costume.Tint != Color.white) Faded.Apply(model, costume.Tint);
            Animator animator = model.GetComponentInChildren<Animator>();
            if (visual != null && animator != null) visual.SetAnimator(animator);
            if (isActiveAndEnabled && Effects.Instance != null) Effects.Instance.Sparkle(transform.position + Vector3.up);
        }
    }
}
