// TempleRiddle.cs
// Kayıp Tapınak bulmacası: yerde üç sembollü taş levha. Oyuncu bir levhanın üstüne basınca levha yanar (ışık + ses).
// Üçü de yanınca ortadaki taş çember çalışır (döner, parlar, toz kalkar), kamera kısa odaklanır ve önündeki topraktan
// özel sandık yükselir. Sandık açılınca içinden bu adanın koleksiyon eşyası (altın kafatası) fırlar, kristal verir.
// Durum kayda girer (SaveData.TempleSolved); yeni oyunda sıfırlanır. Sayılar GameConfig > "Kayıp Tapınak".

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    public class TempleRiddle : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Üç taş levha (basılınca yanar).")]
        [SerializeField] private Transform[] plates = new Transform[0];
        [Tooltip("Her levhanın yanınca açılan ışığı (parlak sembol + hale).")]
        [SerializeField] private GameObject[] plateLights = new GameObject[0];
        [Tooltip("Ortadaki dönen taş çember ve parıltısı.")]
        [SerializeField] private Transform ring;
        [SerializeField] private GameObject ringGlow;
        [Tooltip("Yerden yükselen sandık (içinde TreasureChest).")]
        [SerializeField] private Transform chestHolder;
        [Tooltip("Sandıktan fırlayan koleksiyon eşyası (Collectible; başta gizli ve kapalı).")]
        [SerializeField] private Collectible reward;

        private const float HiddenDepth = 1.4f; // çözülmemişken sandık bu kadar toprağın altında (harita, sayı değil his)

        private bool[] lit;
        private bool solved;
        private bool rewardShown;
        private float ringSpeed;
        private Transform player;
        private TreasureChest chest;

        public bool Solved => solved;
        public int LitCount
        {
            get { int n = 0; if (lit != null) foreach (bool b in lit) if (b) n++; return n; }
        }
        public Transform[] Plates => plates;
        public Transform ChestHolder => chestHolder;
        public Collectible Reward => reward;

        private void Awake()
        {
            if (lit == null || lit.Length != plates.Length) lit = new bool[plates.Length];
            chest = chestHolder != null ? chestHolder.GetComponentInChildren<TreasureChest>(true) : null;
            ApplyState();
        }

        private void OnEnable()
        {
            TreasureChest.Opened += OnChestOpened;
        }

        private void OnDisable()
        {
            TreasureChest.Opened -= OnChestOpened;
        }

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
        }

        // Kayıttan: çözülmüşse levhalar yanık, çember parlak, sandık yerinde (açılmışsa eşya da çıkmış).
        public void Restore(bool isSolved)
        {
            solved = isSolved;
            if (lit == null || lit.Length != plates.Length) lit = new bool[plates.Length];
            for (int i = 0; i < lit.Length; i++) lit[i] = isSolved;
            ApplyState();
        }

        private void ApplyState()
        {
            for (int i = 0; i < plateLights.Length; i++)
                if (plateLights[i] != null) plateLights[i].SetActive(lit != null && i < lit.Length && lit[i]);
            if (ringGlow != null) ringGlow.SetActive(solved);
            // Sandık hep etkin (sayımda yer alsın) ama çözülene kadar toprağın altında ve açılamaz.
            if (chestHolder != null)
                chestHolder.localPosition = new Vector3(chestHolder.localPosition.x, solved ? 0f : -HiddenDepth, chestHolder.localPosition.z);
            if (chest != null) chest.enabled = solved;
            if (reward != null && !rewardShown)
            {
                reward.enabled = false;
                reward.transform.localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            if (ring != null)
            {
                ringSpeed = Mathf.MoveTowards(ringSpeed, solved ? config.TempleRingSpin : 0f, config.TempleRingSpin * Time.deltaTime);
                if (ringSpeed > 0.01f) ring.Rotate(0f, ringSpeed * Time.deltaTime, 0f, Space.World);
            }
            if (solved && chest != null && chest.IsOpen && !rewardShown) ShowReward(false);
            if (solved || player == null) return;
            float r = config.TemplePlateRadius;
            for (int i = 0; i < plates.Length; i++)
            {
                if (lit[i] || plates[i] == null) continue;
                Vector3 d = player.position - plates[i].position;
                d.y = 0f;
                if (d.sqrMagnitude < r * r) Light(i);
            }
        }

        // Levha yanar; üçü yanınca bulmaca çözülür.
        public void Light(int index)
        {
            if (solved || index < 0 || index >= plates.Length || lit[index]) return;
            lit[index] = true;
            if (index < plateLights.Length && plateLights[index] != null)
            {
                plateLights[index].SetActive(true);
                Tween.PopIn(plateLights[index].transform, Vector3.one, 0.3f);
            }
            if (Effects.Instance != null) Effects.Instance.Sparkle(plates[index].position + Vector3.up * 0.3f);
            AudioManager.Play(SoundId.Pop);
            AudioManager.Play(SoundId.Discovery, 0.5f);
            Tween.PunchScale(plates[index], 0.15f, 0.3f);
            if (LitCount == plates.Length) StartCoroutine(Solve());
        }

        private IEnumerator Solve()
        {
            solved = true;
            Vector3 center = ring != null ? ring.position : transform.position;
            CameraFollow cam = CameraFollow.Main;
            bool shot = false;
            if (cam != null && !cam.HasShot)
            {
                Vector3 pos = center + new Vector3(0f, config.RevealShotHeight * 0.7f, -config.RevealShotDistance * 0.7f);
                cam.SetShot(pos, Quaternion.LookRotation(center - pos), 3f);
                shot = true;
            }
            if (ringGlow != null)
            {
                ringGlow.SetActive(true);
                Tween.PopIn(ringGlow.transform, ringGlow.transform.localScale, 0.5f);
            }
            AudioManager.Play(SoundId.Whoosh);
            AudioManager.Play(SoundId.Discovery);
            if (Effects.Instance != null)
                for (int i = 0; i < 6; i++)
                    Effects.Instance.Dust(center + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 1.4f);
            yield return new WaitForSeconds(config.TempleSolveDelay);

            // Sandık topraktan yükselir.
            if (chestHolder != null)
            {
                Vector3 baseLocal = new Vector3(chestHolder.localPosition.x, 0f, chestHolder.localPosition.z);
                chestHolder.localPosition = baseLocal + Vector3.down * HiddenDepth;
                if (Effects.Instance != null) Effects.Instance.Dust(chestHolder.position + Vector3.up * HiddenDepth);
                AudioManager.Play(SoundId.GateOpen, 0.7f);
                Tween.Value(chestHolder, config.TempleChestRiseTime, t =>
                {
                    chestHolder.localPosition = baseLocal + Vector3.down * (HiddenDepth * (1f - Tween.EaseOutBack(t)));
                });
                yield return new WaitForSeconds(config.TempleChestRiseTime);
                chestHolder.localPosition = baseLocal;
                if (chest != null) chest.enabled = true;
                if (Effects.Instance != null) Effects.Instance.GoldBurst(chestHolder.position + Vector3.up * 0.6f);
            }
            yield return new WaitForSeconds(0.8f);
            if (shot && cam != null) cam.ClearShot();
        }

        private void OnChestOpened(TreasureChest opened)
        {
            if (chest != null && opened == chest) ShowReward(true);
        }

        // Sandıktan koleksiyon eşyası fırlar (oyuncunun yanına düşer, yaklaşınca alınır).
        private void ShowReward(bool animate)
        {
            if (rewardShown || reward == null) return;
            rewardShown = true;
            reward.enabled = true;
            Transform t = reward.transform;
            Vector3 target = t.position;
            if (animate && chestHolder != null)
            {
                t.localScale = Vector3.zero;
                Tween.PopIn(t, Vector3.one, 0.35f);
                Tween.MoveArc(t, chestHolder.position + Vector3.up * 0.8f, () => target, 1.6f, 0.6f, null);
            }
            else t.localScale = Vector3.one;
        }
    }
}
