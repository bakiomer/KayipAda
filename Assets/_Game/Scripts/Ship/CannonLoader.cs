// CannonLoader.cs
// Topun yanındaki doldurma alanı: oyuncunun yığınındaki taşları tek tek topa uçurur, her taş bir gülle olur.
// Top doluysa almaz.

using UnityEngine;

namespace KayipAda
{
    public class CannonLoader : InteractionZone
    {
        [SerializeField] private ShipConfig ship;
        [SerializeField] private Cannon cannon;

        private float timer;

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }

            for (int i = 0; i < Visitors.Count; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor) || cannon.IsFull) continue;
                ResourceItem stone = visitor.RemoveTop(ship.AmmoResource);
                if (stone == null) continue;

                stone.FlyToAndDestroy(cannon.transform.position + Vector3.up, config.ItemFlyHeight, config.ItemFlyDuration,
                                      () => cannon.AddAmmo(1));
                AudioManager.Play(SoundId.Drop, 0.5f);
                timer = ship.CannonLoadInterval;
                return;
            }
        }
    }
}
