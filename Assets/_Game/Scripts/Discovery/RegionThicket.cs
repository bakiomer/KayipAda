// RegionThicket.cs
// Kapalı ana ada bölgesini kaplayan sık çalılık ve sarmaşık: içinden geçilmez (NavObstacle), aralarından içerisi
// kısmen görünür, rüzgârda hafifçe sallanır. Bölge açılınca kamera kısa süre bölgeye odaklanır, çalılar kapıdan
// başlayarak tek tek kesilir: parçalar dönerek savrulur ve küçülür, yapraklar saçılır, yol açılır; kamera geri döner.
// Açık bölgede (kayıttan) çalılık hiç görünmez. Sayılar GameConfig > "Kapalı bölge çalılığı".

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class RegionThicket : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Açılınca çalılığın kesildiği bölge.")]
        [SerializeField] private AreaDefinition area;
        [Tooltip("Çalılar ve sarmaşıklar (kesilme sırası: girişe yakın olan önce).")]
        [SerializeField] private Transform[] bushes = new Transform[0];
        [Tooltip("Kapalıyken bölgeye girişi engelleyen engel(ler).")]
        [SerializeField] private NavObstacle[] blockers = new NavObstacle[0];
        [Tooltip("Kesilince saçılan yapraklar.")]
        [SerializeField] private ParticleSystem leaves;
        [Tooltip("Kesilmenin başladığı yer (bölge girişi).")]
        [SerializeField] private Transform entrance;

        private static readonly List<RegionThicket> all = new List<RegionThicket>();
        public static IReadOnlyList<RegionThicket> All => all;

        private Quaternion[] baseRot;
        private Vector3[] baseScale;
        private Vector3[] basePos;
        private bool cleared;

        public AreaDefinition Area => area;
        public bool IsCleared => cleared;
        public bool IsRevealing { get; private set; }
        public int BushCount => bushes.Length;

        private void Awake()
        {
            all.Add(this);
            baseRot = new Quaternion[bushes.Length];
            baseScale = new Vector3[bushes.Length];
            basePos = new Vector3[bushes.Length];
            for (int i = 0; i < bushes.Length; i++)
            {
                baseRot[i] = bushes[i].localRotation;
                baseScale[i] = bushes[i].localScale;
                basePos[i] = bushes[i].localPosition;
            }
        }

        // Kesilirken gizlenirse (Test Menüsü) kamera odak çekiminde kalmasın.
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
                cleared = true;
                SetBlocking(false);
                gameObject.SetActive(false);
                return;
            }
            AreaManager.Instance.AreaUnlocked += OnUnlocked;
        }

        private void OnUnlocked(AreaDefinition unlocked)
        {
            if (unlocked != area || cleared) return;
            cleared = true;
            SetBlocking(false); // yol hemen açılır
            if (isActiveAndEnabled) StartCoroutine(Clear());
            else gameObject.SetActive(false);
        }

        private void SetBlocking(bool on)
        {
            foreach (NavObstacle b in blockers) if (b != null && b.enabled != on) b.enabled = on;
        }

        // Rüzgârda hafif sallanma (her çalı farklı zamanda).
        private void Update()
        {
            if (IsRevealing) return;
            float t = Time.time * config.ThicketSwaySpeed;
            for (int i = 0; i < bushes.Length; i++)
            {
                float s = i * 1.37f;
                bushes[i].localRotation = baseRot[i] * Quaternion.Euler(Mathf.Sin(t + s) * config.ThicketSwayAngle, 0f,
                                                                       Mathf.Cos(t * 0.8f + s) * config.ThicketSwayAngle);
            }
        }

        private IEnumerator Clear()
        {
            IsRevealing = true;
            CameraFollow follow = CameraFollow.Main;
            Vector3 center = transform.position;
            bool tookShot = false;
            if (follow != null && !follow.HasShot)
            {
                Vector3 pos = center + new Vector3(0f, config.ThicketShotHeight, -config.ThicketShotDistance);
                follow.SetShot(pos, Quaternion.LookRotation(center - pos), 3f);
                tookShot = true;
            }
            yield return new WaitForSeconds(0.4f);
            AudioManager.Play(SoundId.Discovery);
            Haptics.Vibrate();

            // Girişe yakın olan önce kesilir.
            Vector3 from = entrance != null ? entrance.position : center;
            List<int> order = new List<int>();
            for (int i = 0; i < bushes.Length; i++) order.Add(i);
            order.Sort((a, b) => (bushes[a].position - from).sqrMagnitude.CompareTo((bushes[b].position - from).sqrMagnitude));
            float step = config.ThicketCutTime / Mathf.Max(1, bushes.Length);
            for (int k = 0; k < order.Count; k++)
            {
                Cut(order[k], center);
                if (k % 3 == 0) AudioManager.Play(SoundId.Rustle, 0.5f);
                if (k % 4 == 0) AudioManager.Play(SoundId.Chop, 0.3f);
                yield return new WaitForSeconds(step);
            }
            yield return new WaitForSeconds(0.6f);
            float rest = config.ThicketFocusTime - config.ThicketCutTime - 1f;
            if (rest > 0f) yield return new WaitForSeconds(rest);
            if (tookShot && follow != null) follow.ClearShot();
            IsRevealing = false;
            gameObject.SetActive(false);
        }

        // Tek çalı: yaprak saçar, dönerek dışarı savrulur ve küçülerek kaybolur.
        private void Cut(int i, Vector3 center)
        {
            Transform bush = bushes[i];
            Vector3 start = bush.position;
            if (leaves != null && config.ThicketLeaves > 0)
            {
                ParticleSystem.EmitParams p = new ParticleSystem.EmitParams { position = start + Vector3.up * 0.6f, applyShapeToPosition = true };
                leaves.Emit(p, config.ThicketLeaves);
            }
            Vector3 outward = start - center;
            outward.y = 0f;
            outward = outward.sqrMagnitude < 0.01f ? Random.insideUnitSphere : outward.normalized;
            outward.y = 0f;
            Vector3 end = start + outward * Random.Range(1.6f, 2.8f) + Vector3.up * 0.2f;
            Vector3 spinAxis = Vector3.Cross(Vector3.up, outward).normalized;
            Quaternion rot0 = bush.rotation;
            Vector3 scale0 = bush.localScale;
            float spin = Random.Range(160f, 320f);
            Tween.Value(bush, 0.55f, t =>
            {
                bush.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.4f);
                bush.rotation = Quaternion.AngleAxis(spin * t, spinAxis) * rot0;
                bush.localScale = scale0 * (1f - t * t);
            }, () => bush.gameObject.SetActive(false));
        }

        // Test Menüsü: çalılıkları kaldır / geri getir (kapalı bölgeler için).
        public static void SetAllHidden(bool hidden)
        {
            foreach (RegionThicket c in all)
            {
                if (c == null) continue;
                bool locked = c.area == null || AreaManager.Instance == null || !AreaManager.Instance.IsUnlocked(c.area);
                c.gameObject.SetActive(!hidden && locked);
                c.SetBlocking(!hidden && locked);
                if (hidden || !locked) continue;
                c.cleared = false;
                c.IsRevealing = false;
                for (int i = 0; i < c.bushes.Length; i++)
                {
                    c.bushes[i].gameObject.SetActive(true);
                    c.bushes[i].localPosition = c.basePos[i];
                    c.bushes[i].localRotation = c.baseRot[i];
                    c.bushes[i].localScale = c.baseScale[i];
                }
            }
        }
    }
}
