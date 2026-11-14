// NightDefense.cs
// Son gece: gemi bitince akşam olur, sahilden 3 dalga halinde iskelet korsanlar gemiye yürür.
// Geminin canı sıfırlanırsa "Tekrar dene" paneli açılır (ilerleme silinmez, sadece gece baştan başlar).
// Üç dalga bitince gün doğar, gemi yelken açıp uzaklaşır, kamera onu takip eder ve kaçış ekranı gelir.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class NightDefense : MonoBehaviour
    {
        public static NightDefense Instance { get; private set; }

        public enum Phase { Waiting, Night, Lost, Won }

        [SerializeField] private ShipConfig ship;
        [SerializeField] private GameConfig config;
        [SerializeField] private ShipBuilder builder;
        [SerializeField] private Enemy enemyPrefab;
        [Tooltip("Düşmanların çıktığı sahil noktaları.")]
        [SerializeField] private Transform[] spawnPoints = new Transform[0];
        [Tooltip("Düşmanların saldırdığı nokta (geminin kıyıdaki ucu).")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private CameraFollow cameraFollow;
        [Header("Arayüz")]
        [SerializeField] private GameObject healthBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMPro.TMP_Text bannerText;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private EscapePanel escapePanel;

        private readonly List<Enemy> spawned = new List<Enemy>();
        private float shipHealth;
        private int alive;
        private Coroutine routine;

        public Phase State { get; private set; } = Phase.Waiting;
        public int Wave { get; private set; }
        public float ShipHealth01 => shipHealth / ship.ShipHealth;

        private void Awake()
        {
            Instance = this;
            if (healthBar != null) healthBar.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (bannerText != null) bannerText.gameObject.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
        }

        private void Start()
        {
            builder.ShipCompleted += OnShipCompleted;
            // Kayıttan gemi bitmiş geldiyse gece hemen başlasın.
            if (builder.IsComplete) OnShipCompleted();
        }

        private void OnDestroy()
        {
            if (builder != null) builder.ShipCompleted -= OnShipCompleted;
        }

        private void OnShipCompleted()
        {
            if (State != Phase.Waiting) return;
            BeginNight();
        }

        public void BeginNight()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(NightRoutine());
        }

        private IEnumerator NightRoutine()
        {
            State = Phase.Night;
            shipHealth = ship.ShipHealth;
            UpdateBar();
            Banner("Gemi hazır! Ama gece yaklaşıyor...");
            yield return new WaitForSeconds(ship.NightStartDelay * 0.5f);
            if (DayNightController.Instance != null) DayNightController.Instance.ToNight();
            AudioManager.Play(SoundId.NightStart);
            Banner("SON GECE\nGemiyi koru!");
            if (healthBar != null) healthBar.SetActive(true);
            yield return new WaitForSeconds(ship.NightStartDelay * 0.5f);

            for (Wave = 1; Wave <= ship.WaveSizes.Length; Wave++)
            {
                Banner("Dalga " + Wave + "/" + ship.WaveSizes.Length);
                int count = config.ScaleAmount(ship.WaveSizes[Wave - 1]);
                for (int i = 0; i < count; i++)
                {
                    if (State != Phase.Night) yield break;
                    Spawn(i);
                    yield return new WaitForSeconds(ship.SpawnInterval);
                }
                while (alive > 0 && State == Phase.Night) yield return null;
                if (State != Phase.Night) yield break;
                if (Wave < ship.WaveSizes.Length) yield return new WaitForSeconds(ship.TimeBetweenWaves);
            }

            yield return Win();
        }

        private void Spawn(int index)
        {
            if (spawnPoints.Length == 0 || enemyPrefab == null) return;
            Transform point = spawnPoints[index % spawnPoints.Length];
            Vector3 jitter = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            Enemy enemy = Instantiate(enemyPrefab, point.position + jitter, Quaternion.identity);
            enemy.Setup(ship, this, attackPoint.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-1f, 1f)));
            Tween.PopIn(enemy.transform, Vector3.one, 0.35f);
            spawned.Add(enemy);
            alive++;
        }

        public void EnemyDied(Enemy enemy)
        {
            alive = Mathf.Max(0, alive - 1);
            spawned.Remove(enemy);
        }

        public void DamageShip(float amount)
        {
            if (State != Phase.Night) return;
            float before = shipHealth;
            shipHealth = Mathf.Max(0f, shipHealth - amount);
            if (Mathf.FloorToInt(before / 10f) != Mathf.FloorToInt(shipHealth / 10f)) AudioManager.Play(SoundId.ShipHit, 0.5f);
            UpdateBar();
            if (shipHealth <= 0f) Lose();
        }

        private void UpdateBar()
        {
            if (healthFill != null) healthFill.fillAmount = ShipHealth01;
        }

        private void Lose()
        {
            State = Phase.Lost;
            if (routine != null) StopCoroutine(routine);
            ClearEnemies();
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                Tween.PopIn(defeatPanel.transform, Vector3.one, 0.35f);
            }
            PlayerController.InputBlocked = true;
        }

        // "Tekrar dene": son gece baştan başlar, ilerleme silinmez.
        public void Retry()
        {
            AudioManager.Play(SoundId.Button);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            PlayerController.InputBlocked = false;
            State = Phase.Waiting;
            BeginNight();
        }

        private IEnumerator Win()
        {
            State = Phase.Won;
            if (healthBar != null) healthBar.SetActive(false);
            Banner("Gün doğuyor... KAÇIŞ ZAMANI!");
            if (DayNightController.Instance != null) DayNightController.Instance.ToDay();
            AudioManager.Play(SoundId.Victory);
            yield return new WaitForSeconds(ship.DayNightTransition);

            PlayerController.InputBlocked = true;
            if (cameraFollow != null) cameraFollow.SetTarget(builder.Model);
            builder.Sail(Vector3.forward);
            yield return new WaitForSeconds(ship.SailDuration);
            if (escapePanel != null) escapePanel.Show();
        }

        private void ClearEnemies()
        {
            foreach (Enemy e in spawned) if (e != null) Destroy(e.gameObject);
            spawned.Clear();
            alive = 0;
        }

        private void Banner(string text)
        {
            if (bannerText == null) return;
            bannerText.text = text;
            bannerText.gameObject.SetActive(true);
            Tween.PopIn(bannerText.transform, Vector3.one, 0.4f);
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), 2.6f);
        }

        private void HideBanner()
        {
            if (bannerText != null) bannerText.gameObject.SetActive(false);
        }
    }
}
