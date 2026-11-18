// InteractionVisibility.cs
// Yakınlığa göre etkileşim görünürlüğü: oyuncu binaya yaklaşınca giriş/çıkış halkaları ve ikonları tam görünür ve
// parlak, Yükselt tabelası açılır (tarif balonu kendi mesafesiyle açılır). Uzaktaki binanın halkaları ve ikonları
// soluk ve biraz küçük kalır (gizlenmez), Yükselt tabelası görünmez; sadece başlık levhası kalır. Geçişler yumuşak
// (saydamlık + ölçek). Ambar'ın Bırak/Al işaretleri uzakta soluk kalır. Mesafeler GameConfig > "Yakınlık görünürlüğü".

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class InteractionVisibility : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Halka ve ikon tuvalleri (soluklaşır ve küçülür).")]
        [SerializeField] private CanvasGroup[] groups = new CanvasGroup[0];
        [Tooltip("Dünyadaki yazılar (soluklaşır).")]
        [SerializeField] private TMP_Text[] texts = new TMP_Text[0];
        [Tooltip("Ölçeği küçülen yer işaretleri (halka, ok).")]
        [SerializeField] private Transform[] scaled = new Transform[0];
        [Tooltip("Yakındayken görünen Yükselt tabelası (boşsa çocuklarda aranır).")]
        [SerializeField] private Transform upgradeSign;

        private Transform player;
        private Vector3[] groupScale, scaledBase;
        private Vector3 signScale;
        private float shown = -1f, signShown = -1f;

        // 0 = uzak (soluk), 1 = yakın (tam).
        public float Nearness => shown < 0f ? 0f : shown;
        public bool SignVisible => upgradeSign != null && signShown > 0.5f;

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            if (upgradeSign == null)
            {
                BuildingUpgradeSign s = GetComponentInChildren<BuildingUpgradeSign>(true);
                if (s != null) upgradeSign = s.transform;
            }
            if (upgradeSign != null) signScale = upgradeSign.localScale;
            groupScale = new Vector3[groups.Length];
            for (int i = 0; i < groups.Length; i++) groupScale[i] = groups[i] != null ? groups[i].transform.localScale : Vector3.one;
            scaledBase = new Vector3[scaled.Length];
            for (int i = 0; i < scaled.Length; i++) scaledBase[i] = scaled[i] != null ? scaled[i].localScale : Vector3.one;
        }

        private void LateUpdate()
        {
            if (player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            float target = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(config.InteractNear, config.InteractFar, dist));
            float signTarget = dist < config.InteractNear ? 1f : 0f;
            if (CameraFollow.Main != null && CameraFollow.Main.HasShot) signTarget = 0f;
            bool first = shown < 0f;
            shown = first ? target : Mathf.MoveTowards(shown, target, Time.deltaTime * 3f);
            signShown = first ? signTarget : Mathf.MoveTowards(signShown, signTarget, Time.deltaTime * 4f);

            float alpha = Mathf.Lerp(config.InteractFarAlpha, 1f, shown);
            float scale = Mathf.Lerp(config.InteractFarScale, 1f, shown);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null) continue;
                groups[i].alpha = alpha;
                groups[i].transform.localScale = groupScale[i] * scale;
            }
            for (int i = 0; i < texts.Length; i++) if (texts[i] != null) texts[i].alpha = alpha;
            for (int i = 0; i < scaled.Length; i++) if (scaled[i] != null) scaled[i].localScale = scaledBase[i] * scale;
            if (upgradeSign != null)
            {
                float s = signShown <= 0f ? 0f : (signTarget > 0f ? Tween.EaseOutBack(signShown) : signShown);
                upgradeSign.localScale = signScale * Mathf.Max(0.001f, s);
            }
        }
    }
}
