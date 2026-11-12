// CarrierWorker.cs
// Taşıyıcı işçi: bina çıkışlarından ürün (kalas, tuğla) alır, Pazar'a götürüp satar, tekrarlar.
// Durumlar: çıkış seç → çıkışa git → al → Pazar'a git → sat → (baştan).
// Çıkış boşalınca: başka çıkışta ürün varsa oraya gider, bina hâlâ çalışıyorsa yeni ürünü bekler,
// yoksa elindekini satmaya gider. Hiç ürün yoksa olduğu yerde bekler.

using UnityEngine;

namespace KayipAda
{
    public class CarrierWorker : Worker
    {
        private enum State { FindSource, GoToSource, Collect, GoToMarket, Sell }

        [Tooltip("Ürün alabileceği bina çıkışları (sadece açılmış binalar kullanılır).")]
        [SerializeField] private BuildingOutput[] sources;
        [Tooltip("Ürünleri sattığı Pazar.")]
        [SerializeField] private Market market;

        private State state = State.FindSource;
        private BuildingOutput source;

        protected override void Awake()
        {
            base.Awake();
            if (sources == null || sources.Length == 0 || market == null)
            {
                Debug.LogError(name + ": Bina çıkışları veya Pazar atanmamış!", this);
                enabled = false;
            }
        }

        protected override void Think()
        {
            switch (state)
            {
                case State.FindSource:
                    if (Stack.IsFull) { StartSelling(); break; }

                    source = FindSource(null);
                    if (source != null)
                    {
                        GoTo(source);
                        state = State.GoToSource;
                    }
                    else if (!Stack.IsEmpty) StartSelling(); // başka ürün yok: elindekini sat
                    else Wait();                             // hiç ürün yok: bekle
                    break;

                case State.GoToSource:
                    if (Stack.IsFull) StartSelling();
                    else if (HasArrived) state = State.Collect;
                    break;

                case State.Collect:
                    // Çıkış alanı ürünü kendisi verir; işçi dolmayı ya da çıkışın boşalmasını bekler.
                    if (Stack.IsFull) { StartSelling(); break; }
                    if (!source.Pile.IsEmpty) break;

                    BuildingOutput other = FindSource(source);
                    if (other != null)
                    {
                        source = other;
                        GoTo(other);
                        state = State.GoToSource;
                    }
                    else if (!source.Building.HasEnoughInput && !Stack.IsEmpty)
                    {
                        StartSelling(); // bina durdu, yeni ürün gelmeyecek
                    }
                    // Aksi halde: bina çalışıyor (ya da eli boş) → burada yeni ürünü bekle.
                    break;

                case State.GoToMarket:
                    if (HasArrived) state = State.Sell;
                    break;

                case State.Sell:
                    // Pazar sırttakileri kendisi satar.
                    if (Stack.IsEmpty) state = State.FindSource;
                    break;
            }
        }

        private void StartSelling()
        {
            GoTo(market);
            state = State.GoToMarket;
        }

        // Ürünü olan, açık binaların çıkışlarından en doluyu seçer ("except" hariç). Yoksa null.
        private BuildingOutput FindSource(BuildingOutput except)
        {
            BuildingOutput best = null;
            foreach (BuildingOutput candidate in sources)
            {
                if (candidate == null || candidate == except || !candidate.isActiveAndEnabled || candidate.Pile.IsEmpty) continue;
                if (best == null || candidate.Pile.Count > best.Pile.Count) best = candidate;
            }
            return best;
        }
    }
}
