// RegionCloud.cs
// Kapalı bölgenin üstündeki yumuşak, yavaşça kıpırdayan alçak poligonlu bulut/sis (sadece kenarlar ve tabela görünür).
// Bölge açılınca: kamera 1.5-2 sn bölgeye odaklanır, bulut parçaları dışarı doğru süzülüp küçülerek dağılır,
// keşif sesi + parıltı, sonra kamera oyuncuya döner. Açık bölgede (kayıttan) bulut hiç görünmez.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class RegionCloud : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Açılınca bulutun dağıldığı bölge.")]
        [SerializeField] private AreaDefinition area;
        [Tooltip("Bulut parçaları.")]
        [SerializeField] private Transform[] puffs = new Transform[0];

        private static readonly List<RegionCloud> all = new List<RegionCloud>();
        public static IReadOnlyList<RegionCloud> All => all;

        private Vector3[] basePos;
        private Vector3[] baseScale;
        private bool dispersed;

        public AreaDefinition Area => area;
        public bool IsDispersed => dispersed;
        public bool IsRevealing { get; private set; }

        private void Awake()
        {
            all.Add(this);
            basePos = new Vector3[puffs.Length];
            baseScale = new Vector3[puffs.Length];
            for (int i = 0; i < puffs.Length; i++)
            {
                basePos[i] = puffs[i].localPosition;
                baseScale[i] = puffs[i].localScale;
            }
        }

        // Dağılırken gizlenirse (Test Menüsü) kamera odak çekiminde kalmasın.
        private void OnDisable()
        {
            if (!IsRevealing) return;
            IsRevealing = false;
            if (CameraFollow.Main != null) CameraFollow.Main.ClearShot();
        }

        private void OnDestroy()
        {
            all.Remove(this);
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked -= OnUnlocked;
        }

        private void Start()
        {
            if (AreaManager.Instance == null) return;
            if (area != null && AreaManager.Instance.IsUnlocked(area))
            {
                dispersed = true;
                gameObject.SetActive(false);
                return;
            }
            AreaManager.Instance.AreaUnlocked += OnUnlocked;
        }

        private void OnUnlocked(AreaDefinition unlocked)
        {
            if (unlocked != area || dispersed) return;
            dispersed = true;
            if (isActiveAndEnabled) StartCoroutine(Disperse());
            else gameObject.SetActive(false);
        }

        // Yavaş kıpırdama.
        private void Update()
        {
            if (IsRevealing) return;
            float t = Time.time * config.CloudDriftSpeed;
            for (int i = 0; i < puffs.Length; i++)
            {
                float s = i * 1.7f;
                puffs[i].localPosition = basePos[i] + new Vector3(Mathf.Sin(t + s) * 0.35f, Mathf.Sin(t * 1.3f + s) * 0.12f, Mathf.Cos(t * 0.8f + s) * 0.35f);
            }
        }

        private IEnumerator Disperse()
        {
            IsRevealing = true;
            CameraFollow follow = CameraFollow.Main;
            Vector3 center = transform.position;
            if (follow != null && !follow.HasShot)
            {
                Vector3 pos = center + new Vector3(0f, config.CloudShotHeight, -config.CloudShotDistance);
                follow.SetShot(pos, Quaternion.LookRotation(center - pos), 3f);
            }
            yield return new WaitForSeconds(0.45f);
            AudioManager.Play(SoundId.Discovery);
            Haptics.Vibrate();
            for (int i = 0; i < puffs.Length; i += 3)
                if (Effects.Instance != null) Effects.Instance.Sparkle(puffs[i].position + Vector3.up);

            Vector3[] from = new Vector3[puffs.Length];
            for (int i = 0; i < puffs.Length; i++) from[i] = puffs[i].localPosition;
            float duration = config.CloudDisperseTime;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                for (int i = 0; i < puffs.Length; i++)
                {
                    Vector3 outward = new Vector3(from[i].x, 0f, from[i].z).normalized;
                    puffs[i].localPosition = from[i] + outward * (k * 4f) + Vector3.up * (k * 2.5f);
                    puffs[i].localScale = baseScale[i] * (1f - k);
                }
                yield return null;
            }
            yield return new WaitForSeconds(Mathf.Max(0f, config.CloudFocusTime - duration - 0.45f));
            if (follow != null) follow.ClearShot();
            IsRevealing = false;
            gameObject.SetActive(false);
        }

        // Test Menüsü: bulutları kaldır / geri getir (kapalı bölgeler için).
        public static void SetAllHidden(bool hidden)
        {
            foreach (RegionCloud c in all)
            {
                if (c == null) continue;
                bool locked = c.area == null || AreaManager.Instance == null || !AreaManager.Instance.IsUnlocked(c.area);
                c.gameObject.SetActive(!hidden && locked);
                if (!hidden && locked)
                {
                    c.dispersed = false;
                    c.IsRevealing = false;
                    for (int i = 0; i < c.puffs.Length; i++) c.puffs[i].localScale = c.baseScale[i];
                }
            }
        }
    }
}
