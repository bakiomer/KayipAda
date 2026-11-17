// StorageDropZone.cs
// Ambarın bırakma alanı: oyuncu içindeyken sırtındaki kaynaklar tek tek ambara akar (Pazar'a satmadan saklama).
// Ambar dolunca bırakma durur ve "Ambar dolu!" uyarısı çıkar. İşçiler buraya bırakmaz.

using UnityEngine;

namespace KayipAda
{
    public class StorageDropZone : InteractionZone
    {
        [SerializeField] private Storage storage;

        private float timer;

        private void Update()
        {
            if (storage == null || !storage.IsOpen) return;
            if (timer > 0f) { timer -= Time.deltaTime; return; }
            for (int i = 0; i < Visitors.Count; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!visitor.IsPlayer || visitor.IsEmpty) continue;
                if (storage.IsFull)
                {
                    storage.WarnFull(transform.position);
                    return;
                }
                ResourceItem item = visitor.RemoveTop();
                if (item == null) continue;
                storage.TryStore(item);
                timer = config.StorageTransferInterval;
                return;
            }
        }
    }
}
