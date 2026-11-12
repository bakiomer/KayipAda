// InteractionZone.cs
// Üzerine gelen karakterlerle etkileşen tüm alanların ortak temeli (kaynak noktası, Pazar, açma kareleri,
// bina giriş/çıkışları). İçerideki tüm CarryStack sahiplerini (oyuncu ve işçiler) Visitors listesinde tutar.
// Oyuncuya her alan hizmet eder; işçiye sadece o an hedeflediği alan (CanServe). Alanın trigger Collider2D'si olmalıdır.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class InteractionZone : MonoBehaviour
    {
        [SerializeField] protected GameConfig config;

        private readonly List<CarryStack> visitors = new List<CarryStack>();

        // Şu an alanın içinde olan karakterlerin yığınları.
        protected IReadOnlyList<CarryStack> Visitors => visitors;

        // Biri alana girdiğinde çağrılır; alt sınıflar isterse kullanır.
        protected virtual void OnVisitorEnter(CarryStack visitor) { }

        // Bu karaktere hizmet edilsin mi? İşçi yoldan geçerken başka alanlara takılmasın diye
        // (örn. odun taşırken Pazar'dan geçince odunu satılmasın) işçiye sadece hedefi olan alan dokunur.
        protected bool CanServe(CarryStack visitor)
        {
            return visitor != null && (visitor.IsPlayer || visitor.WorkTarget == this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out CarryStack stack) && !visitors.Contains(stack))
            {
                visitors.Add(stack);
                OnVisitorEnter(stack);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out CarryStack stack))
            {
                visitors.Remove(stack);
            }
        }

        // Alan gizlenince içeridekiler unutulur (yeniden açılınca Unity giriş olayını tekrar gönderir).
        protected virtual void OnDisable()
        {
            visitors.Clear();
        }
    }
}
