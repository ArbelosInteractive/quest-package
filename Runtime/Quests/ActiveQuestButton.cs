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
    private const string defaultQuestTitle = "No active quest";
    private const string defaultObjectiveTitle = "Tap here to open the quest menu and choose an active quest.";
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
        Debug.Log($"<color={debugColor}>Show Quest Window!</color>");
    }

    public void SetActiveQuestButtonText(string objectiveTitleText, string questTitleText = null)
    {
        if (questTitleText != null)
        {
            questTitle.text = questTitleText;
        }
        objectiveTitle.text = objectiveTitleText;
    }

    public void SetActiveQuestButtonDefaults()
    {
        questTitle.text = defaultQuestTitle;
        objectiveTitle.text = defaultObjectiveTitle;
    }
}
