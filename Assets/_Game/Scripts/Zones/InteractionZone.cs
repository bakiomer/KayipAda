// InteractionZone.cs
// Üzerine gelen karakterle etkileşen tüm alanların ortak temeli (kaynak noktası, satış alanı,
// ileride açma kareleri ve bina girişleri). İçeride CarryStack taşıyan biri varsa Visitor'da tutar.
// Alanın bir trigger Collider2D'si olmalıdır.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class InteractionZone : MonoBehaviour
    {
        [SerializeField] protected GameConfig config;

        // Şu an alanın içinde olan karakterin yığını (yoksa null).
        protected CarryStack Visitor { get; private set; }

        // İçerideki karakter oyuncu mu? (İleride işçiler de alanlara girecek; para ödemek gibi işleri sadece oyuncu yapar.)
        protected bool VisitorIsPlayer { get; private set; }

        // Biri alana girdiğinde çağrılır; alt sınıflar isterse kullanır.
        protected virtual void OnVisitorEnter() { }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out CarryStack stack))
            {
                Visitor = stack;
                VisitorIsPlayer = other.TryGetComponent(out PlayerController _);
                OnVisitorEnter();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out CarryStack stack) && stack == Visitor)
            {
                Visitor = null;
                VisitorIsPlayer = false;
            }
        }
    }
}
