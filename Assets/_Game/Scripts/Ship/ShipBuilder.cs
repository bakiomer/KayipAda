// ShipBuilder.cs
// Kızaktaki gemi inşası (5 aşama: Omurga, Gövde, Direk, Yelken, Erzak; ShipConfig'te).
// Her aşamanın gözle görülen parçaları vardır ve malzeme geldikçe tek tek gelir:
// - Omurga: omurga kirişi ve kaburgalar yukarıdan düşüp yerine oturur.
// - Gövde: gövde tahtaları aşağıdan yukarıya kapanır (BuildReveal shader).
// - Direk: direkler tabanından yükselerek dikilir.  - Yelken: yelkenler tepeden aşağı açılır.
// - Erzak: fıçılar, sandıklar ve bayrak zıplayarak belirir.
// Her parça gelirken toz/kıvılcım ve "tak" sesi. Aşama bitince: oyun kısa süre yavaşlar, kamera gemiye yaklaşır,
// parçalar yerine oturur, altın ışık patlaması, üstten "OMURGA TAMAMLANDI!" bandı, müzik, titreşim; sonra kamera
// oyuncuya döner ve bir sonraki hedef açılır (Stage.UnlockOnComplete). Gemi bitince suya indirme töreni başlar.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace KayipAda
{
    public class ShipBuilder : MonoBehaviour
    {
        public enum PieceStyle { Drop, Rise, Unfurl, Pop }

        public static ShipBuilder Instance { get; private set; }

        public event Action StageCompleted;
        public event Action ShipCompleted;

        [SerializeField] private ShipConfig ship;
        [SerializeField] private GameConfig config;
        [Tooltip("Geminin tüm görüntüsü (törende kayan kök).")]
        [SerializeField] private Transform shipModel;
        [Tooltip("Gövde: aşağıdan yukarı belirir (BuildReveal malzemeli).")]
        [SerializeField] private Renderer[] hullRenderers = new Renderer[0];
        [Tooltip("Gövdenin belirdiği aşamanın sırası (0'dan).")]
        [SerializeField] private int hullStage = 1;
        [Tooltip("Her aşamanın parçaları: kökün çocukları sırayla belirir. Gövde aşamasında boş olabilir.")]
        [SerializeField] private Transform[] stagePieces = new Transform[0];
        [SerializeField] private PieceStyle[] stageStyles = new PieceStyle[0];
        [Tooltip("İnşa iskelesi (gemi bitince kaybolur).")]
        [SerializeField] private GameObject scaffolding;
        [SerializeField] private ShipProgressView progressView;
        [SerializeField] private ProgressRing progressRing;
        [Tooltip("Malzemelerin uçtuğu nokta (gemi gövdesi).")]
        [SerializeField] private Transform deliveryTarget;
        [Tooltip("Teslim alanı (oyuncu ve Taşıyıcı işçiler buraya gelir).")]
        [SerializeField] private ShipDelivery delivery;

        private readonly Dictionary<string, int> delivered = new Dictionary<string, int>();
        private readonly List<Material> revealMaterials = new List<Material>();
        private readonly Dictionary<Transform, Vector3> baseScale = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> basePosition = new Dictionary<Transform, Vector3>();
        private float reveal;
        private float shownReveal;
        private float minY, maxY;
        private int[] shownPieces;
        private Coroutine celebration;
        private float slowedFrom = -1f;

        public int StageIndex { get; private set; }
        public bool IsComplete => StageIndex >= ship.Stages.Length;

        // Oyuncu gemiye ilk malzemeyi getirdi mi? (Taşıyıcı işçiler ancak bundan sonra gemiye yardım eder;
        // öncesinde ürünleri Pazar'da satıp para kazandırırlar.)
        public bool IsStarted => StageIndex > 0 || delivered.Count > 0;
        public ShipConfig Config => ship;
        public ShipDelivery Delivery => delivery;
        public Transform DeliveryTarget => deliveryTarget;
        public Transform Model => shipModel;
        public bool IsCelebrating => celebration != null;

        // Bu aşamada şu an görünen parça sayısı (testler için).
        public int ShownPieces(int stage) => shownPieces != null && stage < shownPieces.Length ? shownPieces[stage] : 0;
        public int PieceCount(int stage) => stage < stagePieces.Length && stagePieces[stage] != null ? stagePieces[stage].childCount : 0;

        private void Awake()
        {
            Instance = this;
            // Gövdenin her malzemesine kendi kopyası: belirme oranı sadece bu gemiyi etkilesin.
            Bounds b = new Bounds(shipModel.position, Vector3.zero);
            bool first = true;
            foreach (Renderer r in hullRenderers)
            {
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = new Material(mats[i]);
                    revealMaterials.Add(mats[i]);
                }
                r.sharedMaterials = mats;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            minY = b.min.y;
            maxY = b.max.y;

            shownPieces = new int[stagePieces.Length];
            foreach (Transform root in stagePieces)
            {
                if (root == null) continue;
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform piece = root.GetChild(i);
                    baseScale[piece] = piece.localScale;
                    basePosition[piece] = piece.localPosition;
                    piece.gameObject.SetActive(false);
                }
            }
        }

        private void Start()
        {
            reveal = TargetReveal();
            shownReveal = reveal;
            ApplyReveal();
            SyncPieces(false);
            RefreshLabel();
            if (IsComplete && scaffolding != null) scaffolding.SetActive(false);
        }

        private void OnDestroy()
        {
            if (slowedFrom >= 0f) Time.timeScale = slowedFrom;
        }

        // Bu kaynak şu anki aşamada hâlâ gerekli mi?
        public bool Needs(ResourceType type)
        {
            if (IsComplete || type == null) return false;
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                if (req.Resource == type) return Delivered(type) < Required(req);
            }
            return false;
        }

        public int Delivered(ResourceType type) => delivered.TryGetValue(type.Id, out int n) ? n : 0;
        private int Required(ShipConfig.Requirement req) => config.ScaleAmount(req.Amount);

        // Teslim alanı bir parça teslim edince çağırır.
        public void Deliver(ResourceItem item)
        {
            ResourceType type = item.Type;
            if (!Needs(type))
            {
                Destroy(item.gameObject);
                return;
            }
            delivered[type.Id] = Delivered(type) + 1;
            item.FlyToAndDestroy(deliveryTarget.position, config.ItemFlyHeight * 1.5f, config.ItemFlyDuration * 1.4f, () =>
            {
                AudioManager.Play(SoundId.Build, 0.6f);
                if (Effects.Instance != null && UnityEngine.Random.value < 0.35f) Effects.Instance.Sparkle(deliveryTarget.position);
            });

            if (StageDone()) CompleteStage();
            AfterProgress();
        }

        // Test Menüsü: şu anki aşamayı malzeme beklemeden bitirir (kutlama ve kilit açma dahil).
        public void ForceCompleteStage()
        {
            if (IsComplete) return;
            CompleteStage();
            AfterProgress();
        }

        private void AfterProgress()
        {
            reveal = TargetReveal();
            SyncPieces(true);
            RefreshLabel();
        }

        private bool StageDone()
        {
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                if (Delivered(req.Resource) < Required(req)) return false;
            }
            return true;
        }

        private void CompleteStage()
        {
            ShipConfig.Stage finished = ship.Stages[StageIndex];
            int finishedIndex = StageIndex;
            StageIndex++;
            delivered.Clear();
            if (finished.UnlockOnComplete != null) AreaManager.Instance.Unlock(finished.UnlockOnComplete);

            if (Effects.Instance != null) Effects.Instance.Confetti(deliveryTarget.position + Vector3.up);
            string title = finished.Name.ToUpper(new CultureInfo("tr-TR")) + " TAMAMLANDI!";
            if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show(title, finished.UnlockMessage, ship.CelebrationDuration);
            AudioManager.Play(SoundId.StageComplete);
            AudioManager.Duck(config.MusicDuckTime);
            Haptics.Vibrate();
            StageCompleted?.Invoke();

            if (celebration != null) StopCelebration();
            if (!IsComplete) celebration = StartCoroutine(Celebrate(finishedIndex));

            if (IsComplete)
            {
                if (scaffolding != null) StartCoroutine(Dismantle());
                ShipCompleted?.Invoke();
            }
        }

        // Aşama kutlaması: kısa yavaşlama, kamera gemiye yaklaşır, parçalar yerine oturur, altın ışık, sonra geri dönüş.
        private IEnumerator Celebrate(int finishedIndex)
        {
            CameraFollow cam = CameraFollow.Main;
            Vector3 focus = shipModel.position + Vector3.up * ((maxY - minY) * 0.35f);
            if (cam != null)
            {
                Quaternion rot = Quaternion.Euler(ship.StageShotPitch, config.CameraYaw + ship.StageShotYaw, 0f);
                cam.SetShot(focus - rot * Vector3.forward * ship.StageShotDistance, rot, ship.StageShotBlend);
            }

            slowedFrom = Time.timeScale;
            Time.timeScale = Mathf.Min(Time.timeScale, ship.StageSlowScale);
            yield return new WaitForSecondsRealtime(ship.StageSlowTime);
            Time.timeScale = slowedFrom;
            slowedFrom = -1f;

            // Parçalar yerine oturur + altın ışık patlaması.
            if (finishedIndex < stagePieces.Length && stagePieces[finishedIndex] != null)
            {
                foreach (Transform piece in stagePieces[finishedIndex]) Tween.PunchScale(piece, 0.12f, 0.35f);
            }
            if (Effects.Instance != null) Effects.Instance.GoldBurst(focus);
            AudioManager.Play(SoundId.PartPlace);

            yield return new WaitForSecondsRealtime(ship.StageShotHold);
            if (cam != null) cam.ClearShot();
            celebration = null;
        }

        private void StopCelebration()
        {
            StopCoroutine(celebration);
            celebration = null;
            if (slowedFrom >= 0f) Time.timeScale = slowedFrom;
            slowedFrom = -1f;
            if (CameraFollow.Main != null) CameraFollow.Main.ClearShot();
        }

        // İnşa iskelesi parça parça sökülür: her parça küçülerek kaybolur, aralarında kısa bekleme.
        private IEnumerator Dismantle()
        {
            Transform root = scaffolding.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform piece = root.GetChild(i);
                Tween.PopOut(piece, ship.PieceAppearDuration * 0.6f, () => piece.gameObject.SetActive(false));
                if (i % 3 == 0) AudioManager.Play(SoundId.PartPlace, 0.4f);
                yield return new WaitForSeconds(ship.ScaffoldRemoveInterval);
            }
            yield return new WaitForSeconds(ship.PieceAppearDuration);
            scaffolding.SetActive(false);
        }

        // Kayıt sistemi için: aşama ve teslim edilenler.
        public IReadOnlyDictionary<string, int> DeliveredCounts => delivered;

        public void Restore(int stage, IDictionary<string, int> counts)
        {
            StageIndex = Mathf.Clamp(stage, 0, ship.Stages.Length);
            delivered.Clear();
            if (counts != null) foreach (KeyValuePair<string, int> p in counts) delivered[p.Key] = p.Value;
            reveal = TargetReveal();
            shownReveal = reveal;
            ApplyReveal();
            SyncPieces(false);
            if (scaffolding != null) scaffolding.SetActive(!IsComplete);
            RefreshLabel();
        }

        // Gövdenin görünmesi gereken oranı: gövde aşamasından önce 0, sonra 1, sırasında ilerleme kadar.
        private float TargetReveal()
        {
            if (StageIndex > hullStage) return 1f;
            if (StageIndex < hullStage) return 0f;
            return StageProgress();
        }

        // Şu anki aşamanın ilerlemesi (0..1): tüm malzemelerin toplamı üzerinden.
        public float StageProgress()
        {
            if (IsComplete) return 1f;
            int have = 0, need = 0;
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                int required = Required(req);
                need += required;
                have += Mathf.Min(Delivered(req.Resource), required);
            }
            return need == 0 ? 1f : (float)have / need;
        }

        // Her aşamanın görünmesi gereken parça sayısı: biten aşamalar tam, şu anki ilerleme kadar, sonrakiler hiç.
        private void SyncPieces(bool animate)
        {
            for (int s = 0; s < stagePieces.Length; s++)
            {
                Transform root = stagePieces[s];
                if (root == null) continue;
                int count = root.childCount;
                int want = s < StageIndex ? count : s > StageIndex ? 0 : Mathf.CeilToInt(StageProgress() * count - 0.0001f);
                if (s < hullStage && StageIndex > hullStage) want = 0; // omurga/kaburgalar gövde tahtalarıyla kapandı
                for (int i = 0; i < count; i++)
                {
                    Transform piece = root.GetChild(i);
                    bool show = i < want;
                    if (piece.gameObject.activeSelf == show) continue;
                    piece.gameObject.SetActive(show);
                    if (!show) continue;
                    piece.localScale = baseScale[piece];
                    piece.localPosition = basePosition[piece];
                    if (animate) Appear(piece, s < stageStyles.Length ? stageStyles[s] : PieceStyle.Pop);
                }
                shownPieces[s] = want;
            }
        }

        // Parça gelir: stile göre düşer / yükselir / açılır / zıplar; gelince toz, kıvılcım ve "tak" sesi.
        private void Appear(Transform piece, PieceStyle style)
        {
            Vector3 scale = baseScale[piece];
            Vector3 position = basePosition[piece];
            float duration = ship.PieceAppearDuration;
            Action landed = () =>
            {
                if (Effects.Instance != null)
                {
                    Effects.Instance.Dust(piece.position);
                    Effects.Instance.Sparkle(piece.position + Vector3.up * 0.5f);
                }
                AudioManager.Play(SoundId.PartPlace, 0.7f);
            };
            switch (style)
            {
                case PieceStyle.Drop:
                    Tween.Value(piece, duration, t =>
                    {
                        float k = t * t; // hızlanarak düşer
                        piece.localPosition = position + Vector3.up * (ship.PieceDropHeight * (1f - k));
                        piece.localScale = scale;
                    }, () => { piece.localPosition = position; Tween.PunchScale(piece, 0.15f, 0.25f); landed(); });
                    break;
                case PieceStyle.Rise:
                case PieceStyle.Unfurl:
                    Tween.Value(piece, duration, t =>
                    {
                        piece.localScale = new Vector3(scale.x, scale.y * Tween.EaseOutBack(t), scale.z);
                    }, () => { piece.localScale = scale; landed(); });
                    break;
                default:
                    Tween.PopIn(piece, scale, duration);
                    landed();
                    break;
            }
        }

        private void Update()
        {
            if (Mathf.Abs(shownReveal - reveal) > 0.0005f)
            {
                shownReveal = Mathf.MoveTowards(shownReveal, reveal, Time.deltaTime * ship.HullRevealSpeed);
                ApplyReveal();
            }
        }

        private void ApplyReveal()
        {
            foreach (Material m in revealMaterials)
            {
                m.SetFloat("_Reveal", shownReveal);
                m.SetFloat("_MinY", minY);
                m.SetFloat("_MaxY", maxY);
            }
        }

        private void RefreshLabel()
        {
            if (progressRing != null) progressRing.SetProgress(StageProgress());
            if (progressView == null) return;
            if (IsComplete)
            {
                progressView.ShowDone("GEMİ HAZIR!");
                return;
            }

            ShipConfig.Stage stage = ship.Stages[StageIndex];
            int n = stage.Requirements.Length;
            ResourceType[] types = new ResourceType[n];
            int[] have = new int[n], need = new int[n];
            for (int i = 0; i < n; i++)
            {
                types[i] = stage.Requirements[i].Resource;
                need[i] = Required(stage.Requirements[i]);
                have[i] = Mathf.Min(Delivered(types[i]), need[i]);
            }
            string title = stage.Name.ToUpper(new CultureInfo("tr-TR")) + "  " + (StageIndex + 1) + "/" + ship.Stages.Length;
            progressView.Show(title, types, have, need);
        }
    }
}
