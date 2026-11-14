// InteractionZone.cs
// Üzerine gelen karakterlerle etkileşen tüm alanların ortak temeli (kaynak noktası, Pazar, açma kareleri,
// bina giriş/çıkışları). İçerideki tüm CarryStack sahiplerini (oyuncu ve işçiler) Visitors listesinde tutar.
// Oyuncuya her alan hizmet eder; işçiye sadece o an hedeflediği alan (CanServe).
// Alanın trigger Collider'ı (3D, isTrigger) olmalıdır.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Collider))]
    public abstract class InteractionZone : MonoBehaviour
    {
        [SerializeField] protected GameConfig config;

        private readonly List<CarryStack> visitors = new List<CarryStack>();

        // Şu an alanın içinde olan karakterlerin yığınları.
        protected IReadOnlyList<CarryStack> Visitors => visitors;

        // Biri alana girdiğinde çağrılır; alt sınıflar isterse kullanır.
        protected virtual void OnVisitorEnter(CarryStack visitor) { }

        // Biri alandan çıktığında çağrılır.
        protected virtual void OnVisitorExit(CarryStack visitor) { }

        // İşçinin bu alanla iş yapmak için yürüyeceği nokta (varsayılan: alanın ortası).
        public virtual Vector3 GetStandPoint(Vector3 from)
        {
            return transform.position;
        }

        // Bu karaktere hizmet edilsin mi? İşçi yoldan geçerken başka alanlara takılmasın diye
        // (örn. odun taşırken Pazar'dan geçince odunu satılmasın) işçiye sadece hedefi olan alan dokunur.
        protected bool CanServe(CarryStack visitor)
        {
            return visitor != null && (visitor.IsPlayer || visitor.WorkTarget == this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CarryStack stack) && !visitors.Contains(stack))
            {
                visitors.Add(stack);
                OnVisitorEnter(stack);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out CarryStack stack) && visitors.Remove(stack))
            {
                OnVisitorExit(stack);
            }
        }

        // Alan gizlenince içeridekiler unutulur (yeniden açılınca Unity giriş olayını tekrar gönderir).
        protected virtual void OnDisable()
        {
            visitors.Clear();
        }
    }
}
