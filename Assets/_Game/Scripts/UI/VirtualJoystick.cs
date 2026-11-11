// VirtualJoystick.cs
// "Yüzen" sanal joystick: ekranın neresine dokunulursa joystick orada belirir,
// parmak sürüklendikçe Direction (-1..1 arası yön) değeri güncellenir.
// Bu script ekranı kaplayan görünmez bir Image üzerine eklenir.

using UnityEngine;
using UnityEngine.EventSystems;

namespace KayipAda
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background; // Dış halka
        [SerializeField] private RectTransform handle;     // İçteki top
        [Tooltip("Topun merkezden en fazla ne kadar uzaklaşabileceği (UI birimi).")]
        [SerializeField] private float handleRange = 110f;
        [Tooltip("Bu değerin altındaki küçük hareketler yok sayılır (0-1).")]
        [SerializeField] private float deadZone = 0.1f;

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
            Vector2 offset = ScreenToLocal(eventData.position) - background.anchoredPosition;
            offset = Vector2.ClampMagnitude(offset, handleRange);
            handle.anchoredPosition = offset;

            Vector2 dir = offset / handleRange;
            Direction = dir.magnitude < deadZone ? Vector2.zero : dir;
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
