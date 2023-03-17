using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gooru;

namespace Arbelos
{
    public class QuestLog : MonoBehaviour
    {
        public GameObject questLogItemObject;
        public GameObject questLogViewport;
        public GameObject questManagerObject;
        private QuestManager questManager;
        private List<Quest> questList;
        private List<GameObject> questLogItems = new List<GameObject>();


        private void Awake()
        {
            questManager = questManagerObject.GetComponent<QuestManager>();
        }

        public void AddQuestToLog(Quest quest, int objectiveIndex)
        {
            GameObject item = Instantiate(questLogItemObject, questLogViewport.transform);
            item.GetComponent<QuestLogItem>().quest = quest;
            item.GetComponent<QuestLogItem>().questManager = questManager;
            item.GetComponent<QuestLogItem>().objectiveIndex = objectiveIndex;
            item.GetComponent<QuestLogItem>().questLog = this;
            questLogItems.Add(item);
        }

        public void RemoveQuestFromLog(int questId)
        {
            foreach (GameObject questLogItem in questLogItems)
            {
                if (questLogItem.GetComponent<QuestLogItem>().quest.id == questId)
                {
                    questLogItem.SetActive(false);
                    return;
                }
            }
            Debug.Log("Quest: " + questId + " not found in QuestLog. RemoveQuestFromLog failed.");
        }

        public void UpdateQuestInLog(int questId, int objectiveId)
        {
            foreach (GameObject questLogItem in questLogItems)
            {
                if (questLogItem.GetComponent<QuestLogItem>().quest.id == questId)
                {
                    QuestLogItem questLogItemFound = questLogItem.GetComponent<QuestLogItem>();
                    foreach (Objective objective in questLogItemFound.quest.objectives)
                    {
                        if (objective.user_objective.id == objectiveId)
                        {
                            questLogItemFound.SetObjectiveText(objective.title);
                            Debug.Log("QuestLogItem found. Updating to objective: " + objectiveId);
                            return;
                        }
                    }
                }
            }
            Debug.Log("Quest: " + questId + " not found in QuestLog. UpdateQuestInLog failed.");
        }
    }
}