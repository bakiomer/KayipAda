// Enemy.cs
// Son gecenin iskelet korsanı: sahilden gemiye yürür, yanına varınca saldırır (gemi canını azaltır).
// Gülle isabet edince can kaybeder; ölünce ölme animasyonu oynar, yere gömülüp kaybolur.
// Binalara çarpmamak için NavObstacle'lardan itilir; adadan çıkmaz.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class Enemy : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        // Sahnede yaşayan tüm düşmanlar (toplar hedef seçerken kullanır).
        public static readonly List<Enemy> Alive = new List<Enemy>();

        [SerializeField] private Animator animator;
        [Tooltip("Yürüme yönüne dönen model kökü.")]
        [SerializeField] private Transform body;

        private ShipConfig ship;
        private NightDefense defense;
        private Vector3 target;
        private int health;
        private bool dead;

        public bool IsDead => dead;

        public void Setup(ShipConfig config, NightDefense owner, Vector3 attackPoint)
        {
            ship = config;
            defense = owner;
            target = attackPoint;
            health = config.EnemyHealth;
        }

        private void OnEnable() => Alive.Add(this);
        private void OnDisable() => Alive.Remove(this);

        private void Update()
        {
            if (dead || ship == null) return;

            Vector3 pos = transform.position;
            Vector3 to = target - pos;
            to.y = 0f;
            bool attacking = to.magnitude <= ship.EnemyAttackRange;

            if (!attacking)
            {
                Vector3 step = to.normalized * (ship.EnemySpeed * Time.deltaTime);
                Vector2 flat = new Vector2(pos.x + step.x, pos.z + step.z);
                flat = NavObstacle.PushOut(flat, 0.35f);
                if (IslandShape.Instance != null) flat = IslandShape.Instance.ClampInside(flat, 0.8f);
                transform.position = new Vector3(flat.x, 0f, flat.y);
            }
            else
            {
                defense.DamageShip(ship.EnemyDps * Time.deltaTime);
            }

            if (to.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(to.normalized);
                body.rotation = Quaternion.Slerp(body.rotation, look, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, attacking ? 0f : ship.EnemySpeed / 6f + 0.3f);
                animator.SetBool(AttackHash, attacking);
            }
        }

        public void Hit(int damage)
        {
            if (dead) return;
            health -= damage;
            Tween.PunchScale(body, 0.15f, 0.2f);
            if (health > 0) return;

            dead = true;
            Alive.Remove(this);
            AudioManager.Play(SoundId.EnemyDie, 0.7f);
            if (animator != null) animator.SetTrigger(DieHash);
            defense.EnemyDied(this);

            // Ölme animasyonundan sonra yere gömülüp kaybolur.
            Vector3 start = transform.position;
            Tween.Value(this, 1f, t => transform.position = start + Vector3.down * (t * 1.2f),
                        () => Destroy(gameObject), 1.4f);
        }
    }
}
