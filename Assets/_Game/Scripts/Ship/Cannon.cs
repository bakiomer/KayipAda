// Cannon.cs
// Savunma topu: gecede menzildeki en yakın iskelete döner ve taş gülleyi kavisli atar; gülle düşünce patlar
// (patlama efekti + ses + yarıçap içindekilere hasar). Gülle stoğu topun üstündeki göstergede yazar.
// Gülleler oyuncunun getirdiği taşlardan doldurulur (CannonLoader).

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class Cannon : MonoBehaviour
    {
        [SerializeField] private ShipConfig ship;
        [Tooltip("Hedefe dönen namlu kökü.")]
        [SerializeField] private Transform barrel;
        [Tooltip("Güllenin çıktığı nokta.")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private TMP_Text ammoLabel;

        private float timer;
        private bool ammoSet;

        public int Ammo { get; private set; }
        public bool IsFull => Ammo >= ship.CannonAmmoCapacity;

        private void Awake()
        {
            if (!ammoSet) Ammo = Mathf.Min(ship.CannonStartAmmo, ship.CannonAmmoCapacity); // kayıttan geldiyse dokunma
            ammoSet = true;
            RefreshLabel();
        }

        public void AddAmmo(int amount)
        {
            Ammo = Mathf.Min(ship.CannonAmmoCapacity, Ammo + amount);
            RefreshLabel();
            Tween.PunchScale(ammoLabel.transform, 0.2f, 0.2f);
        }

        // Kayıt sistemi için.
        public void SetAmmo(int amount)
        {
            Ammo = Mathf.Clamp(amount, 0, ship.CannonAmmoCapacity);
            ammoSet = true;
            RefreshLabel();
        }

        private void Update()
        {
            if (timer > 0f) timer -= Time.deltaTime;

            Enemy target = FindTarget();
            if (target == null) return;

            Vector3 dir = target.transform.position - barrel.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized);
                barrel.rotation = Quaternion.Slerp(barrel.rotation, look, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            if (timer > 0f || Ammo <= 0) return;
            timer = ship.CannonFireInterval;
            Fire(target);
        }

        private Enemy FindTarget()
        {
            Enemy best = null;
            float bestDistance = ship.CannonRange * ship.CannonRange;
            foreach (Enemy e in Enemy.Alive)
            {
                if (e == null || e.IsDead) continue;
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    best = e;
                    bestDistance = d;
                }
            }
            return best;
        }

        private void Fire(Enemy target)
        {
            Ammo--;
            RefreshLabel();
            AudioManager.Play(SoundId.CannonFire, 0.8f);
            Tween.PunchScale(barrel, 0.12f, 0.25f);

            Vector3 from = muzzle != null ? muzzle.position : barrel.position + Vector3.up;
            // Hedefin varış anındaki yaklaşık yeri: şu anki konum (iskeletler yavaş).
            Vector3 landing = target.transform.position;
            GameObject ball = ballPrefab != null ? Instantiate(ballPrefab, from, Quaternion.identity) : null;
            if (ball == null)
            {
                Explode(landing);
                return;
            }
            Tween.MoveArc(ball.transform, from, () => landing, ship.CannonBallArc, ship.CannonBallFlightTime, () =>
            {
                Explode(landing);
                Destroy(ball);
            });
        }

        private void Explode(Vector3 position)
        {
            if (Effects.Instance != null) Effects.Instance.Explosion(position + Vector3.up * 0.3f);
            AudioManager.Play(SoundId.Explosion, 0.8f);
            float r2 = ship.ExplosionRadius * ship.ExplosionRadius;
            for (int i = Enemy.Alive.Count - 1; i >= 0; i--)
            {
                Enemy e = Enemy.Alive[i];
                if (e != null && (e.transform.position - position).sqrMagnitude <= r2) e.Hit(ship.CannonDamage);
            }
        }

        private void RefreshLabel()
        {
            if (ammoLabel != null) ammoLabel.text = "Gülle " + Ammo + "/" + ship.CannonAmmoCapacity;
        }
    }
}
