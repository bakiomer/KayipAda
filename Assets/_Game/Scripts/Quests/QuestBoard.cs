// QuestBoard.cs
// Kamptaki ahşap görev panosu (üstünde kâğıtlar): oyuncu önündeki alanda durunca (PanelZone) görev panosu paneli açılır.

using UnityEngine;

namespace KayipAda
{
    public class QuestBoard : PanelZone
    {
        protected override void OpenPanel(CarryStack visitor)
        {
            if (QuestBoardPanel.Instance != null) QuestBoardPanel.Instance.Open(transform);
        }

        protected override void ClosePanel()
        {
            if (QuestBoardPanel.Instance != null) QuestBoardPanel.Instance.Close(transform);
        }
    }
}
