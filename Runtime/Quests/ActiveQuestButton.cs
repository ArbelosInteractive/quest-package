using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActiveQuestButton : MonoBehaviour
{
    [SerializeField] public TMP_Text questTitle;
    [SerializeField] public TMP_Text objectiveTitle;
    private const string defaultQuestTitle = "No active quest";
    private const string defaultObjectiveTitle = "Tap here to open the quest menu and choose an active quest.";
    public static ActiveQuestButton Instance { get; private set; }

    public void ShowQuestList()
    {
        //open window to new screens
    }

    public void SetActiveQuestViewText(string objectiveTitleText, string questTitleText = null)
    {
        if (questTitleText != null)
        {
            questTitle.text = questTitleText;
        }
        objectiveTitle.text = objectiveTitleText;
    }

    public void SetActiveQuestViewDefaults()
    {
        questTitle.text = defaultQuestTitle;
        objectiveTitle.text = defaultObjectiveTitle;
    }
}
