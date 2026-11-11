// VirtualJoystick.cs
// "Yüzen" sanal joystick: ekranın neresine dokunulursa (veya fareyle tıklanırsa) orada belirir,
// sürükledikçe Direction (-1..1 arası yön) güncellenir. Dokunmatik ve fare aynı şekilde çalışır
// çünkü ikisi de EventSystem (InputSystemUIInputModule) üzerinden gelir.
// Bu script ekranı kaplayan görünmez bir Image üzerine eklenir. Ayarlar GameConfig'ten okunur.

using UnityEngine;
using UnityEngine.EventSystems;

namespace KayipAda
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private RectTransform background; // Dış halka
        [SerializeField] private RectTransform handle;     // İçteki top

        public Vector2 Direction { get; private set; }

        private RectTransform area;

        private void Awake()
        {
            area = (RectTransform)transform;
            background.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Joystick'i dokunulan noktaya taşı ve göster.
            background.anchoredPosition = ScreenToLocal(eventData.position);
            handle.anchoredPosition = Vector2.zero;
            background.gameObject.SetActive(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            float range = config.JoystickRange;
            Vector2 offset = ScreenToLocal(eventData.position) - background.anchoredPosition;
            offset = Vector2.ClampMagnitude(offset, range);
            handle.anchoredPosition = offset;

            Vector2 dir = offset / range;
            Direction = dir.magnitude < config.JoystickDeadZone ? Vector2.zero : dir;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Direction = Vector2.zero;
            background.gameObject.SetActive(false);
        }

        // Ekran koordinatını (piksel) bu alanın yerel UI koordinatına çevirir.
        private Vector2 ScreenToLocal(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, null, out Vector2 local);
            return local;
        }
    }
}
