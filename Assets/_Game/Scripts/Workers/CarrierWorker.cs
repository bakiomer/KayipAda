// CarrierWorker.cs
// Taşıyıcı işçi: bina çıkışlarından ürün (kalas, tuğla) alır; gemi o ürünü bekliyorsa gemiye, yoksa Pazar'a götürür.
// Durumlar: çıkış seç → çıkışa git → al → Pazar'a git → sat → (baştan).
// Bina çıkışlarında ürün yoksa Ambar'a bakar: gemiye ya da bir bina girişine lazım olan malzeme ambarda varsa
// ambarın alma alanından ihtiyaç kadar alıp oraya götürür.
// Çıkış boşalınca: başka çıkışta ürün varsa oraya gider, bina hâlâ çalışıyorsa yeni ürünü bekler,
// yoksa elindekini satmaya gider. Hiç ürün yoksa olduğu yerde bekler.

using UnityEngine;

namespace KayipAda
{
    public class CarrierWorker : Worker
    {
        private enum State { FindSource, GoToSource, Collect, GoToMarket, Sell, GoToStorage, TakeFromStorage, Deliver }

        [Tooltip("Ürün alabileceği bina çıkışları (sadece açılmış binalar kullanılır).")]
        [SerializeField] private BuildingOutput[] sources;
        [Tooltip("Ürünleri sattığı Pazar.")]
        [SerializeField] private Market market;

        private State state = State.FindSource;
        private BuildingOutput source;
        private InteractionZone dropZone; // Pazar ya da gemi teslim alanı
        private StoragePickZone storagePick;
        private BuildingInput[] inputs = new BuildingInput[0];
        private ResourceType wantType;
        private int wantAmount;
        private InteractionZone deliverTo;
        private float takeTimer;

        // Ambardan taşıma (test ve görünüm için).
        public bool IsStorageRun => state == State.GoToStorage || state == State.TakeFromStorage || state == State.Deliver;

        private void Start()
        {
            storagePick = FindAnyObjectByType<StoragePickZone>(FindObjectsInactive.Include);
            inputs = FindObjectsByType<BuildingInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

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
                    else if (PlanStorageRun())               // ambarda gemiye/binaya lazım olan var
                    {
                        GoTo(storagePick);
                        state = State.GoToStorage;
                    }
                    else Wait();                             // hiç ürün yok: bekle
                    break;

                case State.GoToStorage:
                    if (HasArrived) { state = State.TakeFromStorage; takeTimer = 0f; }
                    break;

                case State.TakeFromStorage:
                    if (takeTimer > 0f) { takeTimer -= Time.deltaTime; break; }
                    if (wantAmount > 0 && storagePick.GiveTo(Stack, wantType))
                    {
                        wantAmount--;
                        takeTimer = (Storage.Instance != null ? Storage.Instance.TransferInterval : 0.1f) * 2f;
                        break;
                    }
                    if (Stack.IsEmpty) { state = State.FindSource; break; }
                    GoTo(deliverTo);
                    state = State.Deliver;
                    break;

                case State.Deliver:
                    // Teslim alanı / bina girişi sırttakileri kendisi alır.
                    if (!HasArrived) break;
                    if (Stack.IsEmpty) state = State.FindSource;
                    else if (Stack.CountOf(wantType) == 0 || !StillNeeded()) StartSelling(); // kalanı sat
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
                    // Pazar ya da gemi teslim alanı sırttakileri kendisi alır.
                    if (Stack.IsEmpty) state = State.FindSource;
                    else if (dropZone != market && !CarriesShipNeed()) StartSelling(); // gemi artık istemiyor: kalanı sat
                    break;
            }
        }

        // Ambarda gemiye ya da bir bina girişine lazım olan malzeme var mı? Varsa hedefi ve miktarı seçer.
        private bool PlanStorageRun()
        {
            Storage storage = Storage.Instance;
            if (storage == null || !storage.IsOpen || storagePick == null || !storagePick.isActiveAndEnabled || storage.Total == 0) return false;
            ShipBuilder ship = ShipBuilder.Instance;
            if (ship != null && !ship.IsComplete && ship.Delivery != null && ship.Delivery.isActiveAndEnabled)
            {
                foreach (ResourceType t in storage.Types)
                {
                    int need = Mathf.Min(ship.Missing(t), storage.CountOf(t));
                    if (need <= 0) continue;
                    wantType = t;
                    wantAmount = Mathf.Min(need, Stack.Capacity);
                    deliverTo = ship.Delivery;
                    return true;
                }
            }
            foreach (BuildingInput input in inputs)
            {
                if (input == null || !input.isActiveAndEnabled || input.Building == null) continue;
                ResourceType t = input.Building.Type.Input;
                ItemPile pile = input.Building.InputPile;
                int space = pile.Capacity - pile.Count;
                int need = Mathf.Min(space, storage.CountOf(t));
                if (need <= 0) continue;
                wantType = t;
                wantAmount = Mathf.Min(need, Stack.Capacity);
                deliverTo = input;
                return true;
            }
            return false;
        }

        private bool StillNeeded()
        {
            if (deliverTo is BuildingInput input) return input.Building != null && !input.Building.InputPile.IsFull;
            ShipBuilder ship = ShipBuilder.Instance;
            return ship != null && ship.Needs(wantType);
        }

        private void StartSelling()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            dropZone = ship != null && ship.Delivery != null && ship.Delivery.isActiveAndEnabled && CarriesShipNeed()
                ? ship.Delivery
                : (InteractionZone)market;
            GoTo(dropZone);
            state = State.GoToMarket;
        }

        // Sırtındakilerden gemiye lazım olan var mı?
        private bool CarriesShipNeed()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            if (ship == null || ship.IsComplete) return false;
            if (wantType != null && ship.Needs(wantType) && Stack.CountOf(wantType) > 0) return true; // ambardan getirdiği
            if (!ship.IsStarted) return false;
            foreach (BuildingOutput s in sources)
            {
                if (s != null && ship.Needs(s.Building.Type.Output) && Stack.CountOf(s.Building.Type.Output) > 0) return true;
            }
            if (wantType != null && ship.Needs(wantType) && Stack.CountOf(wantType) > 0) return true;
            return false;
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
