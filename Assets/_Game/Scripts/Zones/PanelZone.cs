// PanelZone.cs
// Panel açan alanların ortak temeli (Tayfa Barakası, Geliştirme Tezgahı, Ambar "Al", Görev Panosu, Yükselt tabelası).
// Oyuncu alana girince alan belirginleşir ve küçük ikon çıkar; panel ancak oyuncu alanda DURURSA (joystick bırakılmış,
// GameConfig.PanelOpenDelay sn) açılır — durma süresi yerdeki halkada dolar. Hareket başladığı an panel anında kapanır;
// yanından geçerken açılmaz. X / geri tuşuyla kapatılınca oyuncu tekrar durana kadar açılmaz. Joystick asla kilitlenmez.

using UnityEngine;
using UnityEngine.InputSystem;

namespace KayipAda
{
    public abstract class PanelZone : InteractionZone
    {
        [Tooltip("Alan ipucu: oyuncu içerideyken belirginleşen görsel (yer halkası + ikon). Boş olabilir.")]
        [SerializeField] private GameObject hint;
        [Tooltip("Durma süresini gösteren halka (boş olabilir).")]
        [SerializeField] private ProgressRing dwellRing;

        private bool playerInside;
        private CarryStack player;
        private float still;
        private bool shownThisStop;

        public bool PlayerInside => playerInside;
        // Testler için: oyuncu durunca panel açılana kadar geçen süre.
        public float StillTime => still;

        // Android geri tuşu (Input System'de Escape) ya da klavyede Esc bu karede basıldı mı? Paneller kapanır.
        public static bool BackPressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        protected abstract void OpenPanel(CarryStack visitor);
        protected abstract void ClosePanel();
        // Panel açılabilir mi (örn. Ambar kapalıyken açılmaz).
        protected virtual bool CanOpen => true;

        protected virtual void Awake()
        {
            SetHint(false);
        }

        protected override void OnVisitorEnter(CarryStack visitor)
        {
            if (!visitor.IsPlayer) return;
            playerInside = true;
            player = visitor;
            still = 0f;
            shownThisStop = false;
            SetHint(true);
        }

        protected override void OnVisitorExit(CarryStack visitor)
        {
            if (!visitor.IsPlayer) return;
            playerInside = false;
            still = 0f;
            shownThisStop = false;
            SetHint(false);
            ClosePanel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (playerInside) ClosePanel();
            playerInside = false;
            SetHint(false);
        }

        protected virtual void Update()
        {
            if (!playerInside) return;
            if (PlayerController.MoveInputActive || PlayerController.InputBlocked)
            {
                still = 0f;
                if (shownThisStop) ClosePanel();
                shownThisStop = false;
            }
            else if (!shownThisStop)
            {
                still += Time.deltaTime;
                if (still >= config.PanelOpenDelay && CanOpen)
                {
                    shownThisStop = true;
                    OpenPanel(player);
                }
            }
            if (dwellRing != null) dwellRing.SetProgress(shownThisStop ? 1f : still / Mathf.Max(0.01f, config.PanelOpenDelay));
        }

        private void SetHint(bool on)
        {
            if (hint != null && hint.activeSelf != on)
            {
                hint.SetActive(on);
                if (on) Tween.PopIn(hint.transform, Vector3.one, 0.2f);
            }
            if (dwellRing != null) dwellRing.SetProgress(0f);
        }
    }
}
