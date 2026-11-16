// Crab.cs
// Sahildeki yengeç: yan yan yürür, ara sıra durur. Oyuncu yaklaşınca kuma gömülüp kaybolur, kısa süre sonra
// sahilin başka bir yerinde kumdan çıkar. Çarpışması yoktur, oyunu etkilemez. Sayılar GameConfig'te (Canlılar).

using UnityEngine;

namespace KayipAda
{
    public class Crab : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Gövde (yürürken hafifçe zıplar).")]
        [SerializeField] private Transform body;

        private IslandLife life;
        private Transform player;
        private float timer;
        private float dir = 1f;
        private bool walking;
        private float hiddenUntil = -1f;
        private float sink;          // 0 = yüzeyde, 1 = kuma gömülü
        private Vector3 bodyBase;

        public void Setup(IslandLife owner, Transform target)
        {
            life = owner;
            player = target;
            bodyBase = body.localPosition;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // Gömülüyken: süre dolunca başka yerde çık.
            if (hiddenUntil > 0f)
            {
                sink = Mathf.MoveTowards(sink, 1f, dt * 3f);
                if (Time.time >= hiddenUntil)
                {
                    hiddenUntil = -1f;
                    transform.position = life.RandomBeachPoint();
                    if (Effects.Instance != null) Effects.Instance.Dust(transform.position);
                }
                ApplySink();
                return;
            }
            sink = Mathf.MoveTowards(sink, 0f, dt * 2f);

            // Oyuncu yaklaştı: kuma gömül.
            if (player != null && (player.position - transform.position).sqrMagnitude < config.CrabFleeDistance * config.CrabFleeDistance)
            {
                hiddenUntil = Time.time + config.CrabHideTime;
                if (Effects.Instance != null) Effects.Instance.Dust(transform.position);
                ApplySink();
                return;
            }

            // Yürü / dur.
            timer -= dt;
            if (timer <= 0f)
            {
                walking = Random.value < 0.65f;
                dir = Random.value < 0.5f ? -1f : 1f;
                timer = Random.Range(0.8f, 2.4f);
                if (Random.value < 0.3f) transform.rotation *= Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f);
            }
            if (walking)
            {
                Vector3 next = transform.position + transform.right * (dir * config.CrabSpeed * dt);
                if (life.IsBeach(next)) transform.position = next;
                else dir = -dir; // sahilden çıkma
            }
            ApplySink();
        }

        private void ApplySink()
        {
            float bob = walking && hiddenUntil < 0f ? Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.03f : 0f; // yürürken tıpış tıpış
            body.localPosition = bodyBase + Vector3.up * (bob - sink * 0.35f);
            bool visible = sink < 0.99f;
            if (body.gameObject.activeSelf != visible) body.gameObject.SetActive(visible);
        }
    }
}
