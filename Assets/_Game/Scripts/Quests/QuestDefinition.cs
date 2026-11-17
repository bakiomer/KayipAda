// QuestDefinition.cs
// İsteğe bağlı yan görevin ayar dosyası: tür (topla, sat, üret, onar, sandık bul, bölge keşfet, altın figür),
// hedef (kaynak / yapı / bölge), miktar, ödül (para ve/veya kristal) ve hangi bölge açılınca çıkacağı.
// Dosyalar: Assets/_Game/Data/Quests (sırası önemlidir: QuestManager listeyi baştan sona dener).

using UnityEngine;

namespace KayipAda
{
    public enum QuestKind { Collect, Sell, Produce, Repair, FindChest, Explore, FigurePart }

    [CreateAssetMenu(fileName = "NewQuest", menuName = "Kayıp Ada/Yan Görev")]
    public class QuestDefinition : ScriptableObject
    {
        [field: Tooltip("Kayıt kimliği (küçük harf).")]
        [field: SerializeField] public string Id { get; private set; } = "";

        [field: Tooltip("Kartta görünen başlık. Örn: Kuyuyu Onar")]
        [field: SerializeField] public string Title { get; private set; } = "Görev";

        [field: SerializeField] public QuestKind Kind { get; private set; } = QuestKind.Collect;

        [field: Tooltip("Topla/Sat/Üret: hangi kaynak (Sat'ta boşsa her şey sayılır).")]
        [field: SerializeField] public ResourceType Resource { get; private set; }

        [field: Tooltip("Onar: yapının kimliği (RepairSite).")]
        [field: SerializeField] public string Target { get; private set; } = "";

        [field: Tooltip("Keşfet: açılması gereken bölge.")]
        [field: SerializeField] public AreaDefinition ExploreArea { get; private set; }

        [field: SerializeField, Min(1)] public int Amount { get; private set; } = 10;

        [field: Header("Ödül")]
        [field: SerializeField, Min(0)] public int RewardCoins { get; private set; }
        [field: SerializeField, Min(0)] public int RewardCrystals { get; private set; }

        [field: Tooltip("Bu bölge/bina açılmadan görev çıkmaz (boşsa baştan).")]
        [field: SerializeField] public AreaDefinition RequiredArea { get; private set; }

#if UNITY_EDITOR
        public void EditorSetup(string id, string title, QuestKind kind, ResourceType resource, string target, AreaDefinition explore,
                                int amount, int coins, int crystals, AreaDefinition required)
        {
            Id = id;
            Title = title;
            Kind = kind;
            Resource = resource;
            Target = target;
            ExploreArea = explore;
            Amount = amount;
            RewardCoins = coins;
            RewardCrystals = crystals;
            RequiredArea = required;
        }
#endif
    }
}
