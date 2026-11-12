// UnlockableArea.cs
// Açılabilir bir bölgenin sahnedeki kökü. Bölge kapalıyken içeriği (kaynak noktaları vb.) gizlidir
// ve çit görünür; AreaManager bölgeyi açınca içerik ortaya çıkar, çit kaybolur.

using UnityEngine;

namespace KayipAda
{
    public class UnlockableArea : MonoBehaviour
    {
        [SerializeField] private AreaDefinition area;
        [Tooltip("Bölge açılınca görünecek nesneler (kaynak noktaları vb.).")]
        [SerializeField] private GameObject content;
        [Tooltip("Bölge kapalıyken görünen nesneler (çit, kilit yazısı).")]
        [SerializeField] private GameObject lockedVisual;

        private void Awake()
        {
            if (area == null || content == null)
            {
                Debug.LogError(name + ": Area veya Content atanmamış!", this);
                return;
            }

            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            Refresh();
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
            }
        }

        private void OnAreaUnlocked(AreaDefinition unlocked)
        {
            if (unlocked == area) Refresh();
        }

        private void Refresh()
        {
            bool open = AreaManager.Instance.IsUnlocked(area);
            content.SetActive(open);
            if (lockedVisual != null) lockedVisual.SetActive(!open);
        }
    }
}
