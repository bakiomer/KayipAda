// StoragePickZone.cs
// Ambarın alma alanı: oyuncu içinde durunca alttan Ambar paneli açılır (her kaynak ikon + miktar, seçilen kaynaktan
// "Al" ile kapasite kadar alınır); çıkınca kapanır. Taşıyıcı işçi bu alanı hedef alıp ambardan malzeme çeker.

using UnityEngine;

namespace KayipAda
{
    public class StoragePickZone : InteractionZone
    {
        [SerializeField] private Storage storage;

        public Storage Storage => storage;

        protected override void OnVisitorEnter(CarryStack visitor)
        {
            if (visitor.IsPlayer && storage != null && storage.IsOpen && StoragePanel.Instance != null) StoragePanel.Instance.Open(this, visitor);
        }

        protected override void OnVisitorExit(CarryStack visitor)
        {
            if (visitor.IsPlayer && StoragePanel.Instance != null) StoragePanel.Instance.Close(this);
        }

        // İşçi (ya da panel) bir parça ister: ambardan çıkıp sırta uçar. Sırt doluysa / ambarda yoksa false.
        public bool GiveTo(CarryStack stack, ResourceType type)
        {
            if (storage == null || stack == null || stack.IsFull || storage.CountOf(type) <= 0) return false;
            ResourceItem item = storage.Take(type);
            if (item == null) return false;
            if (!stack.TryAdd(item))
            {
                Destroy(item.gameObject);
                storage.Add(type, 1);
                return false;
            }
            return true;
        }
    }
}
