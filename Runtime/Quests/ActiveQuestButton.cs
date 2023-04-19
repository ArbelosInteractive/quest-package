using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActiveQuestButton : MonoBehaviour
{
    //component references
    [SerializeField] public TMP_Text questTitle;
    [SerializeField] public TMP_Text objectiveTitle;

    //constants
    private const string defaultQuestTitle = "No Active Quest";
    private const string defaultObjectiveTitle = "Talk to DV to begin or resume.";
    private const string finishedAllQuestsTitle = "You have completed all quests! Congratulations!";
    private const string debugColor = "#e8d168";

    //singleton
    public static ActiveQuestButton Instance { get; private set; }

    private void Start()
    {
        Instance = this;
        SetActiveQuestButtonDefaults();
    }

    public void ShowQuestList()
    {
        //open window to new screens
        Debug.Log($"<color={debugColor}>(ActiveQuestButton) Show Quest Window here!</color>");
    }

    public void SetActiveQuestButtonText(string questTitleText, string objectiveTitleText)
    {
        if (!string.IsNullOrEmpty(questTitleText))
        {
            questTitle.text = questTitleText;
        }
        else
        {
            Debug.Log($"<color{debugColor}>(ActiveQuestButton) questTitleText is null or empty</color>");
        }

        if (!string.IsNullOrEmpty(objectiveTitleText))
        {
            objectiveTitle.text = objectiveTitleText;
        }
        else
        {
            Debug.Log($"<color{debugColor}>(ActiveQuestButton) objectiveTitleText is null or empty</color>");
        }
    }

    private void SetActiveQuestButtonDefaults()
    {
        questTitle.text = defaultQuestTitle;
        objectiveTitle.text = defaultObjectiveTitle;
    }

    public void SetFinishedAllQuestsButtonText()
    {
        questTitle.text = defaultQuestTitle;
        objectiveTitle.text = finishedAllQuestsTitle;
    }
}
