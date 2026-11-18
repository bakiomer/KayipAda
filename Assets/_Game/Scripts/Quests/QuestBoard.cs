// QuestBoard.cs
// Kamptaki ahşap görev panosu (üstünde kâğıtlar): oyuncu önündeki alana girince görev panosu paneli açılır.

using UnityEngine;

namespace KayipAda
{
    public class QuestBoard : InteractionZone
    {
        protected override void OnVisitorEnter(CarryStack visitor)
        {
            if (visitor.IsPlayer && QuestBoardPanel.Instance != null) QuestBoardPanel.Instance.Open(transform);
        }

        protected override void OnVisitorExit(CarryStack visitor)
        {
            if (visitor.IsPlayer && QuestBoardPanel.Instance != null) QuestBoardPanel.Instance.Close(transform);
        }
    }
}
