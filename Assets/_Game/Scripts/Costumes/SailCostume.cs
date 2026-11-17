// SailCostume.cs
// Geminin yelken ve bayrak rengini giyilen yelken/bayrak kostümüne göre boyar (klasik, kırmızı, siyah, altın).

using UnityEngine;

namespace KayipAda
{
    public class SailCostume : MonoBehaviour
    {
        [Tooltip("Boyanan parçalar (yelkenler, bayrak).")]
        [SerializeField] private GameObject[] targets = new GameObject[0];

        public string AppliedId { get; private set; } = "";

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
            CostumeDefinition costume = ProfileManager.Instance.Equipped(CostumeSlot.Sail);
            AppliedId = costume != null ? costume.Id : "";
            foreach (GameObject t in targets)
            {
                Faded.Clear(t);
                if (costume != null && costume.Tint != Color.white) Faded.Apply(t, costume.Tint);
            }
        }
    }
}
