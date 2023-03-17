using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Gooru;

namespace Arbelos
{
    public class QuestLogItem : MonoBehaviour
    {
        public Quest quest;
        public QuestManager questManager;
        public QuestLog questLog;
        public GameObject titleTextObject;
        public GameObject objectiveTextObject;
        public int objectiveIndex;
        private TMP_Text titleText;
        private TMP_Text objectiveText;

        private void Start()
        {
            titleText = titleTextObject.GetComponent<TMP_Text>();
            objectiveText = objectiveTextObject.GetComponent<TMP_Text>();
            SetTitleText(quest.title);
            SetObjectiveText(quest.objectives[objectiveIndex].title);
        }

        public void SetTitleText(string text)
        {
            titleText.text = text;
        }
        public void SetObjectiveText(string text)
        {
            objectiveText.text = text;
        }
    }
}