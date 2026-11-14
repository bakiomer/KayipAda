// UnlockableArea.cs
// Açılabilir bir bölgenin/binanın sahnedeki kökü. Kapalıyken içeriği (kaynak noktaları, bina) gizlidir
// ve çit/silik zemin görünür. AreaManager bölgeyi açınca çit küçülerek kaybolur, içerideki nesneler
// sırayla zıplayarak belirir. Oyun başında zaten açık olanlar efektsiz gösterilir.

using UnityEngine;

namespace KayipAda
{
    public class UnlockableArea : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private AreaDefinition area;
        [Tooltip("Bölge açılınca görünecek nesneler (kaynak noktaları, bina vb.).")]
        [SerializeField] private GameObject content;
        [Tooltip("Bölge kapalıyken görünen nesneler (çit, yazı, silik zemin).")]
        [SerializeField] private GameObject lockedVisual;

        private void Awake()
        {
            if (area == null || content == null)
            {
                Debug.LogError(name + ": Area veya Content atanmamış!", this);
                return;
            }

            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            bool open = AreaManager.Instance.IsUnlocked(area);
            content.SetActive(open);
            if (lockedVisual != null) lockedVisual.SetActive(!open);
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
            if (unlocked != area) return;

            if (lockedVisual != null)
            {
                Tween.PopOut(lockedVisual.transform, 0.3f, () => lockedVisual.SetActive(false));
            }

            content.SetActive(true);
            // İçerideki nesneler sırayla zıplayarak belirir.
            Transform root = content.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Tween.PopIn(child, child.localScale, config.PopInDuration, 0.15f + i * config.PopInStagger);
            }
        }
    }
}
