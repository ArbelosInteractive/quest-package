using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arbelos
{
    public class QuestLogButton : MonoBehaviour
    {
        public GameObject questLogObject;
        private bool toggleBool = false;

        public void toggleQuestLog()
        {
            toggleBool = !toggleBool;
            questLogObject.SetActive(toggleBool);
        }
    }
}
