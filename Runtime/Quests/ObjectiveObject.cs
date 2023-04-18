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
        [SerializeField] private List<string> attributes;

        //fields
        private int questId;
        private Objective objective;
        
        //constants
        private const string debugColor = "#e8d168";
        
        private void Start()
        {
            QuestManager.Instance.AddToObjectiveObjectsList(this.gameObject);
        }

        public void SetInfo(int newQuestId, Objective newObjective)
        {
            questId = newQuestId;
            objective = newObjective;
            objectiveId = objective.user_objective.id;

            Debug.Log($"</color={debugColor}>(Objective Object) ObjectName: {gameObject.name} | ObjectiveId = {objectiveId}</color>", gameObject);
        }

        public int GetQuestId()
        {
            return questId;
        }

        public Objective GetObjective()
        {
            return objective;
        }
    }
}
