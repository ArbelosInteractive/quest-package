using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using Gooru;

namespace Arbelos
{
    public class QuestManager : MonoBehaviour
    {
        //fields
        private List<Quest> questList = new List<Quest>();
        public int currentSequence = 0;
        private string collectionId = "";
        public List<GameObject> objectiveObjects;

        //public objects
        public GameObject globalObject;
        public GameObject questLogObject;
        public GameObject npcCanvasObject;

        //component references
        private GooruManager gooruManager;
        private IDialogueManager dialogueManager;
        private QuestLog questLog;

        public static QuestManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            gooruManager = globalObject.GetComponent<GooruManager>();
            questLog = questLogObject.GetComponent<QuestLog>();

            if (dialogueManager == null)
            {
                IEnumerable<IDialogueManager> dialogueManagerList = FindObjectsOfType<MonoBehaviour>().OfType<IDialogueManager>();
                dialogueManager = dialogueManagerList.ElementAt(0); //realisticlly there should only be 1
            }

        }

        private async Task<List<Quest>> GetAllQuests()
        {
            return await gooruManager.GetAllQuests();
        }

        public async Task StartFirstQuest()
        {
            try
            {
                List<Quest> quests = new List<Quest>();
                quests = await GetAllQuests();
                foreach (Quest quest in quests)
                {
                    if (quest.user_quest.sequence == 1)
                    {
                        try
                        {
                            await StartQuest(quest.id);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"StartQuest failed in StartFirstQuest: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"StartFirstQuest failed: {e.Message}");
            }
        }

        private async Task<Quest> GetQuest(int questId)
        {
            Quest newQuest = new Quest();
            newQuest = await gooruManager.GetQuest(questId);
            return newQuest;
        }

        public Quest GetQuestFromQuestList(int questId)
        {
            foreach (Quest quest in questList)
            {
                if (quest.id == questId)
                {
                    return quest;
                }
            }

            Debug.Log("Quest: " + questId + " not found in GetQuestFromList. Null returned.");
            return null;
        }

        public async Task<int> GetNextQuestId(int questId)
        {
            NextQuestResponse nextQuest = new NextQuestResponse();
            nextQuest = await gooruManager.GetNextQuest(questId);
            return nextQuest.quest_id;
        }

        public string GetCollectionId()
        {
            return collectionId;
        }

        public Quest GetQuestFromQuestListByObjectiveId(int objectiveId)
        {
            foreach (Quest quest in questList)
            {
                foreach (Objective objective in quest.objectives)
                {
                    if (objective.id == objectiveId)
                    {
                        return quest;
                    }
                }
            }

            Debug.Log("Objective: " + objectiveId + " not found in GetQuestFromQuestListByObjectiveId. Null returned.");
            return null;
        }

        public async Task StartQuest(int questId)
        {
            if (GetQuestFromQuestList(questId) != null)
            {
                Debug.Log("(ERROR) Quest: " + questId + " is already in the QuestList!");
                return;
            }

            Quest newQuest = await GetQuest(questId);
            if (newQuest == null)
            {
                Debug.Log("(ERROR) Quest: " + questId + " returned null from the server!");
                return;
            }

            try
            {
                await gooruManager.StartQuest(newQuest.user_quest.id);
                questList.Add(newQuest);
                int index = newQuest.objectives.FindIndex(a => a.sequence == 1);
                await StartObjective(newQuest.id, newQuest.objectives[index].user_objective.id);
                questLog.AddQuestToLog(newQuest);
                Debug.Log("Quest Started: " + questId);
                return;
            }
            catch (Exception e)
            {
                Debug.Log("(ERROR) Failed to start quest: " + questId);
                throw e;
            }
        }

        public async Task StartObjective(int questId, int userObjectiveId)
        {
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log("Quest: " + questId + " is not in the questList. StartObjective failed.");
                return;
            }

            foreach (Quest quest in questList)
            {
                if (quest.id == questId)
                {
                    foreach (Objective objective in quest.objectives)
                    {
                        if (objective.user_objective.id == userObjectiveId)
                        {
                            try
                            {
                                await gooruManager.StartObjective(userObjectiveId);
                                foreach (GameObject objectiveObject in objectiveObjects)
                                {
                                    if (objectiveObject.GetComponent<ObjectiveObject>().npcId == objective.end_game_object_id)
                                    {
                                        objectiveObject.GetComponent<ObjectiveObject>().SetInfo(questId, objective);
                                        objectiveObject.GetComponentInChildren<NPCQuestMarker>(true).gameObject.SetActive(true);
                                        try
                                        {
                                            collectionId = objective.navigator_objective_detail.collection_id;
                                        }
                                        catch
                                        {
                                            Debug.Log("CollectionId is null.");
                                        }


                                        // List<string> dialogs = GetDialogueData(objective);
                                        // if (dialogs.Count > 0)
                                        // {
                                        //     DialogueManager.Instance.StartConversation(dialogs, objective.end_game_object.firstName, objective.end_game_object.lastName);
                                        // }

                                        Debug.Log("(SUCCESS) Objective Object set to ID: " + objectiveObject.GetComponent<ObjectiveObject>().npcId);
                                        Debug.Log($"Objective: ({objective.title}) started!");
                                        return;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"(ERROR) Failed to start objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                            }

                        }
                    }
                    Debug.Log("(ERROR) Objective: " + userObjectiveId + " not found.");
                    return;
                }
            }
        }

        public async Task CompleteObjective(int questId, int userObjectiveId)
        {
            QuestServiceObject questServiceObject = new QuestServiceObject();
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log("Quest: " + questId + " is not in the questList. Completing without quest.");

                questServiceObject.withQuest = "false";
                await gooruManager.CompleteObjective(userObjectiveId, questServiceObject);
                return;
            }

            foreach (Quest quest1 in questList)
            {
                Debug.Log($"QuestIdTest: {quest1.id}");
                Debug.Log($"UserQuestIdTest: {quest1.user_quest.id}");
            }

            foreach (Quest quest in questList)
            {
                if (quest.id == questId)
                {
                    Debug.Log($"Quest Id: {quest.id}");
                    foreach (Objective objective in quest.objectives)
                    {
                        if (objective.user_objective.id == userObjectiveId)
                        {
                            try
                            {
                                questServiceObject.withQuest = "true";
                                List<string> dialogs = GetDialogueData(objective);
                                if (dialogs.Count > 0)
                                {
                                    dialogueManager.StartConversation(dialogs, objective.end_game_object.firstName, objective.end_game_object.lastName);
                                }

                                await gooruManager.CompleteObjective(userObjectiveId, questServiceObject);
                                foreach (GameObject objectiveObject in objectiveObjects)
                                {
                                    if (objectiveObject.GetComponent<ObjectiveObject>().npcId == objective.end_game_object_id)
                                    {
                                        objectiveObject.GetComponentInChildren<NPCQuestMarker>(true).gameObject.SetActive(false);
                                        Debug.Log($"Objective {objective.title} Complete!");
                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"(ERROR) Failed to complete objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                                return;
                            }

                            if (objective.sequence == quest.objectives.Count)
                            {
                                questLog.RemoveQuestFromLog(quest.id);
                                dialogueManager.SetAtEndOfQuest(true);
                                Debug.Log("Final objective complete!");

                                int nextQuestId = 0;
                                try
                                {
                                    nextQuestId = await GetNextQuestId(quest.user_quest.id);
                                    Debug.Log($"NextQuestId: {nextQuestId} for Quest: {quest.user_quest.id}");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"GetNextQuestId failed with nextQuestId: {quest.user_quest.id}. \n Error: {e.Message}");
                                }

                                if (nextQuestId != 0)
                                {
                                    try
                                    {
                                        await StartQuest(nextQuestId);
                                        Debug.Log("Starting new quest: " + nextQuestId);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"StartQuest failed with nextQuestId: {nextQuestId}. \n Error: {e.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.Log($"Final Quest Complete???? | NextQuestId: {nextQuestId} | UserQuestId: {quest.user_quest.id}");
                                }
                                questList.Remove(quest);
                            }
                            else
                            {
                                currentSequence = quest.objectives.FindIndex(a => a.sequence == objective.sequence + 1);
                                await StartObjective(quest.id, quest.objectives[currentSequence].user_objective.id);
                                Debug.Log($"(UpdateQuestInLog) QuestId: {quest.id} | UserObjectiveId: {quest.objectives[currentSequence].user_objective.id}");
                                questLog.UpdateQuestInLog(quest.id, quest.objectives[currentSequence].user_objective.id);
                            }
                            return;
                        }
                    }
                    Debug.Log("(ERROR) Objective:" + userObjectiveId + " not found.");
                    return;
                }
            }
        }

        //NPC
        public void AddToObjectiveObjectsList(GameObject objectiveObject)
        {
            objectiveObjects.Add(objectiveObject);
            Debug.Log(objectiveObject.GetComponent<ObjectiveObject>().npcId + " added to ObjectiveObjects list!");
        }

        public List<string> GetDialogueData(Objective objective)
        {
            List<string> dialogueText = new List<string>();
            List<Message> messages = new List<Message>();

            if (objective.objective_dialog != null)
            {
                messages = objective.objective_dialog.messages;
                List<Message> sortedMessages = messages.OrderBy(message => message.sequence).ToList();
                foreach (var message in sortedMessages)
                {
                    dialogueText.Add(message.text);
                    Debug.Log("<color=purple>Message: </color>" + message.text);
                }
            }

            return dialogueText;
        }
    }
}
