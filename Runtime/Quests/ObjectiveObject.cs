using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gooru;

namespace Arbelos
{
    public class ObjectiveObject : MonoBehaviour
    {
        //Inspector properties
        public string npcId;
        public string npcDisplayName;
        public int objectiveId;

        //fields
        private int questId;
        private Objective objective;

        [SerializeField] private List<string> attributes;

        private void Start()
        {
            QuestManager.Instance.AddToObjectiveObjectsList(this.gameObject);
        }

        public void SetInfo(int newQuestId, Objective newObjective)
        {
            questId = newQuestId;
            objective = newObjective;
            objectiveId = objective.user_objective.id;

            Debug.Log($"ObjectName: {gameObject.name} | ObjectiveId = {objectiveId}", gameObject);
        }

        public int GetQuestId()
        {
            return questId;
        }

        public Objective GetObjective()
        {
            return objective;
        }

        //for testing purposes - delete later
        private async void OnTriggerStay(Collider other)
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                if (other.tag == "Player")
                {
                    await QuestManager.Instance.CompleteObjective(questId, objective.user_objective.id);
                }
            }
        }
    }
}
