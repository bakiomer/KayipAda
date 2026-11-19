// CampSite.cs
// Adanın merkezindeki kamp: oyuncu burada başlar; ortada kamp ateşi (titreyen alev, kıvılcım, hafif duman, çıtırtı).
// Kamp ilerledikçe büyür: başta enkazdan çadır, gemi 2. aşamada ahşap kulübe, 4. aşamada küçük ev (bayrak, fener,
// sandıklar); her büyümede zıplama, konfeti ve kutlama. Boşta kalan işçiler kısa süre ateşin başına gelip dinlenir
// (oturma yerleri), sonra işe döner. Kamp seviyesi kayda yazılır (SaveData.CampLevel).

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    public class CampSite : MonoBehaviour
    {
        public static CampSite Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Seviye görünüşleri: 0 = enkaz çadır, 1 = ahşap kulübe, 2 = küçük ev.")]
        [SerializeField] private GameObject[] levels = new GameObject[0];
        [Tooltip("Kamp bu gemi aşamasına gelince büyür (seviye 1, seviye 2).")]
        [SerializeField] private int[] stageForLevel = { 2, 4 };
        [Tooltip("Ateşin başındaki dinlenme yerleri.")]
        [SerializeField] private Transform[] seats = new Transform[0];
        [SerializeField] private Transform fire;
        [SerializeField] private Transform flame;
        [SerializeField] private AudioSource crackle;

        private readonly Worker[] seatUsers = new Worker[8];
        private Vector3 flameScale;

        public int Level { get; private set; }
        public float RestAfter => config.CampRestAfter;
        public float RestTime => config.CampRestTime;
        public Vector3 FirePosition => fire != null ? fire.position : transform.position;

        private void Awake()
        {
            Instance = this;
            if (flame != null) flameScale = flame.localScale;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ShipBuilder.Instance != null) ShipBuilder.Instance.StageCompleted -= OnStage;
        }

        private void Start()
        {
            if (ShipBuilder.Instance != null)
            {
                ShipBuilder.Instance.StageCompleted += OnStage;
                Level = Mathf.Max(Level, LevelForStage(ShipBuilder.Instance.StageIndex));
            }
            ShowLevel(false);
            if (crackle != null)
            {
                crackle.clip = MakeCrackle();
                crackle.loop = true;
                crackle.Play();
            }
        }

        private int LevelForStage(int stage)
        {
            int level = 0;
            for (int i = 0; i < stageForLevel.Length; i++) if (stage >= stageForLevel[i]) level = i + 1;
            return Mathf.Min(level, levels.Length - 1);
        }

        private void OnStage()
        {
            int want = LevelForStage(ShipBuilder.Instance.StageIndex);
            if (want <= Level) return;
            Level = want;
            ShowLevel(true);
        }

        // Kayıt.
        public void Restore(int level)
        {
            Level = Mathf.Clamp(Mathf.Max(level, Level), 0, Mathf.Max(0, levels.Length - 1));
            ShowLevel(false);
        }

        // Test Menüsü / testler: kampı bir seviye büyüt.
        public void ForceGrow()
        {
            if (Level >= levels.Length - 1) return;
            Level++;
            ShowLevel(true);
        }

        private void ShowLevel(bool celebrate)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == null) continue;
                bool on = i == Level;
                if (levels[i].activeSelf == on) continue;
                levels[i].SetActive(on);
                if (on && celebrate) Tween.PopIn(levels[i].transform, Vector3.one, 0.5f);
            }
            if (!celebrate) return;
            Vector3 p = levels.Length > Level && levels[Level] != null ? levels[Level].transform.position : transform.position;
            if (Effects.Instance != null) { Effects.Instance.Confetti(p + Vector3.up * 1.5f); Effects.Instance.Dust(p); }
            if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show("KAMP BÜYÜDÜ!", Level == 1 ? "Ahşap kulübe" : "Küçük ev", 2f);
            AudioManager.Celebrate(CelebrationTier.Medium);
            Haptics.Vibrate();
        }

        private void Update()
        {
            if (flame == null) return;
            float t = Time.time;
            float f = 1f + Mathf.Sin(t * 13f) * 0.08f + Mathf.Sin(t * 7.3f) * 0.06f;
            flame.localScale = new Vector3(flameScale.x * (2f - f), flameScale.y * f, flameScale.z * (2f - f));
        }

        // Boşta kalan işçi için ateşin başında boş bir yer (yoksa null).
        public Transform ClaimSeat(Worker worker)
        {
            for (int i = 0; i < seats.Length && i < seatUsers.Length; i++)
                if (seatUsers[i] == null || seatUsers[i] == worker) { seatUsers[i] = worker; return seats[i]; }
            return null;
        }

        public void ReleaseSeat(Worker worker)
        {
            for (int i = 0; i < seatUsers.Length; i++) if (seatUsers[i] == worker) seatUsers[i] = null;
        }

        // Çıtırtı: kodla üretilen kısa, rastgele "çat" sesleri (döngü).
        private static AudioClip MakeCrackle()
        {
            const int rate = 22050;
            int length = rate * 3;
            float[] data = new float[length];
            System.Random r = new System.Random(3);
            float brown = 0f;
            for (int i = 0; i < length; i++)
            {
                brown = Mathf.Clamp(brown + ((float)r.NextDouble() * 2f - 1f) * 0.02f, -0.3f, 0.3f);
                data[i] = brown * 0.25f; // alçak uğultu
            }
            for (int k = 0; k < 40; k++)
            {
                int at = r.Next(length - 400);
                float amp = 0.25f + (float)r.NextDouble() * 0.5f;
                for (int j = 0; j < 300; j++) data[at + j] += ((float)r.NextDouble() * 2f - 1f) * amp * Mathf.Exp(-j / 40f);
            }
            AudioClip clip = AudioClip.Create("Crackle", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
