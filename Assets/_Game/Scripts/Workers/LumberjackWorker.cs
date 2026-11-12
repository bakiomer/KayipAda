// LumberjackWorker.cs
// Oduncu işçi: ağaçlardan odun toplar, Bıçkıhane'nin girişine götürüp bırakır, tekrarlar.
// Durumlar: ağaç seç → ağaca git → topla → binaya git → bırak → (baştan).
// Bütün ağaçlar tükenmişse elindekini teslim eder ya da bekler; giriş doluysa yer açılana kadar girişte bekler.

using UnityEngine;

namespace KayipAda
{
    public class LumberjackWorker : Worker
    {
        private enum State { FindTree, GoToTree, Gather, GoToBuilding, Deliver }

        [Tooltip("Odun toplayabileceği ağaçlar.")]
        [SerializeField] private ResourceNode[] trees;
        [Tooltip("Odunu götürdüğü bina girişi (Bıçkıhane).")]
        [SerializeField] private BuildingInput buildingInput;

        private State state = State.FindTree;
        private ResourceNode tree;

        // Sırtındaki, binanın kabul ettiği türden kaynak sayısı.
        private int CarriedInput => Stack.CountOf(buildingInput.Building.Type.Input);

        protected override void Awake()
        {
            base.Awake();
            if (trees == null || trees.Length == 0 || buildingInput == null)
            {
                Debug.LogError(name + ": Ağaçlar veya bina girişi atanmamış!", this);
                enabled = false;
            }
        }

        protected override void Think()
        {
            switch (state)
            {
                case State.FindTree:
                    if (Stack.IsFull) { StartDelivery(); break; }

                    tree = FindTree();
                    if (tree != null)
                    {
                        GoTo(tree);
                        state = State.GoToTree;
                    }
                    else if (CarriedInput > 0) StartDelivery(); // ağaç kalmadı: elindekini götür
                    else Wait();                                // ağaçlar yeniden dolana kadar bekle
                    break;

                case State.GoToTree:
                    if (Stack.IsFull) StartDelivery();
                    else if (tree.IsDepleted) state = State.FindTree; // yolda başkası bitirdi
                    else if (HasArrived) state = State.Gather;
                    break;

                case State.Gather:
                    // Ağaç alanı odunu kendisi verir; işçi sadece dolmayı ya da ağacın bitmesini bekler.
                    if (Stack.IsFull) StartDelivery();
                    else if (tree.IsDepleted) state = State.FindTree;
                    break;

                case State.GoToBuilding:
                    if (HasArrived) state = State.Deliver;
                    break;

                case State.Deliver:
                    // Giriş alanı odunu kendisi alır; giriş doluysa işçi burada bekler.
                    if (CarriedInput == 0) state = State.FindTree;
                    break;
            }
        }

        private void StartDelivery()
        {
            GoTo(buildingInput);
            state = State.GoToBuilding;
        }

        // Tükenmemiş en yakın ağaç (yoksa null).
        private ResourceNode FindTree()
        {
            ResourceNode best = null;
            float bestDistance = float.MaxValue;
            foreach (ResourceNode candidate in trees)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.IsDepleted) continue;

                float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
