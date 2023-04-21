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
        private Quest activeQuest = new Quest();
        private string collectionId = "";
        public List<GameObject> objectiveObjects;
        public int currentSequence = 0;

        //constants
        private const string debugColor = "#e8d168";

        //component references
        private IDialogueManager dialogueManager;
        private ActiveQuestButton activeQuestButton;

        //events
        public delegate void OnQuestStarted();
        public static event OnQuestStarted onQuestStarted;
        public delegate void OnQuestCompleted();
        public static event OnQuestCompleted onQuestComplete;
        public delegate void OnObjectiveStarted();
        public static event OnObjectiveStarted onObjectiveStarted;
        public delegate void OnObjectiveCompleted();
        public static event OnObjectiveCompleted onObjectiveCompleted;

        //Singleton
        public static QuestManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (dialogueManager == null)
            {
                IEnumerable<IDialogueManager> dialogueManagerList = FindObjectsOfType<MonoBehaviour>().OfType<IDialogueManager>();
                dialogueManager = dialogueManagerList.ElementAt(0); //realistically there should only be 1
            }

            if (activeQuestButton == null)
            {
                activeQuestButton = ActiveQuestButton.Instance;
            }
        }

        private async Task<List<Quest>> GetAllQuests()
        {
            return await GooruManager.Instance.GetAllQuests();
        }

        private async Task<Quest> GetQuest(int questId)
        {
            Quest newQuest = new Quest();
            newQuest = await GooruManager.Instance.GetQuest(questId);
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

            Debug.Log($"<color={debugColor}>(Quest Manager) Quest: " + questId + " not found in GetQuestFromList. Null returned.</color>");
            return null;
        }

        public async Task<int> GetNextQuestId(int questId)
        {
            NextQuestResponse nextQuest = new NextQuestResponse();
            nextQuest = await GooruManager.Instance.GetNextQuest(questId);
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

            Debug.Log($"<color={debugColor}>(Quest Manager) Objective: " + objectiveId + " not found in GetQuestFromQuestListByObjectiveId. Null returned.</color>");
            return null;
        }

        public async Task InitializeQuests()
        {
            try
            {
                List<Quest> quests = new List<Quest>();
                quests = await GetAllQuests();
                quests.Sort((x, y) => x.user_quest.sequence.CompareTo(y.user_quest.sequence));
                foreach (Quest quest in quests)
                {
                    if (quest.user_quest.sequence == 1 && quest.user_quest.progress == 0)
                    {
                        Debug.Log($"<color={debugColor}>(Quest Manager) First quest in sequence starting: {quest.id} | {quest.title}</color>");
                        await StartQuest(quest.id);
                        return;
                    }
                    else
                    {
                        if (quest.user_quest.progress < 100)
                        {
                            Debug.Log($"<color={debugColor}>(Quest Manager) Found quest with progress under 100: {quest.id} | {quest.title}</color>");
                            await ResumeQuest(quest);
                            return;
                        }
                    }
                }
                Debug.Log($"<color={debugColor}>(Quest Manager) No quest found with less than 100 progress. All Quests finished!</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"(Quest Manager) InitializeQuests failed: {e.Message}");
            }
        }

        private async Task ResumeQuest(Quest quest)
        {
            quest.objectives.Sort((x, y) => x.sequence.CompareTo(y.sequence));
            foreach (Objective objective in quest.objectives)
            {
                if (objective.user_objective.progress < 100)
                {
                    questList.Add(quest);
                    currentSequence = quest.objectives.FindIndex(a => a.user_objective.id == objective.user_objective.id);

                    Debug.Log($"<color={debugColor}>(Quest Manager) Current Sequence: {currentSequence}</color>");

                    await StartObjective(quest.id, quest.objectives[currentSequence].user_objective.id);
                    ActiveQuestButton.Instance.SetActiveQuestButtonText(quest.title, quest.objectives[currentSequence].title);
                    Debug.Log($"<color={debugColor}>(Quest Manager) Resuming quest: {quest.id} at user-objective: {objective.user_objective.id}</color>");
                    return;
                }
            }
        }

        public async Task StartQuest(int questId)
        {
            if (GetQuestFromQuestList(questId) != null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: " + questId + " is already in the QuestList!</color>");
                return;
            }

            Quest newQuest = await GetQuest(questId);
            if (newQuest == null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: " + questId + " returned null from the server!</color>");
                return;
            }

            try
            {
                await GooruManager.Instance.StartQuest(newQuest.user_quest.id);
                questList.Add(newQuest);
                int index = newQuest.objectives.FindIndex(a => a.sequence == 1);
                await StartObjective(newQuest.id, newQuest.objectives[index].user_objective.id);
                ActiveQuestButton.Instance.SetActiveQuestButtonText(newQuest.title, newQuest.objectives[0].title);
                if (onQuestStarted != null)
                {
                    onQuestStarted();
                }
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest Started: {questId}</color>");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"(Quest Manager) Failed to start quest: questId");
                throw e;
            }
        }

        public async Task StartObjective(int questId, int userObjectiveId)
        {
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: {questId} is not in the questList. StartObjective: {userObjectiveId} failed.</color>");
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
                                await GooruManager.Instance.StartObjective(userObjectiveId);
                                if (onObjectiveStarted != null)
                                {
                                    onObjectiveStarted();
                                }
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
                                            Debug.Log($"(Quest Manager) <color={debugColor}>CollectionId is null for user_objective: {objective.user_objective.id}</color>");
                                        }

                                        Debug.Log($"(Quest Manager) <color={debugColor}>Objective: ({objective.title}) started!</color>");
                                        return;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"(Quest Manager) Failed to start objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                            }
                        }
                    }
                    Debug.Log($"(Quest Manager) <color={debugColor}>Objective: {userObjectiveId} not found.</color>");
                    return;
                }
            }
        }


        public async Task CompleteObjective(int questId, int userObjectiveId)
        {
            QuestServiceObject questServiceObject = new QuestServiceObject();

            //an assessment is completed without the appropriate quest in the list
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: {questId} is not in the questList. Completing without quest.</color>");
                questServiceObject.withQuest = "false";
                await GooruManager.Instance.CompleteObjective(userObjectiveId, questServiceObject);
                return;
            }

            //complete objective with quest in the list
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
                                questServiceObject.withQuest = "true";

                                //display dialog at the end of an objective
                                List<string> dialogs = GetDialogueData(objective);
                                if (dialogs.Count > 0)
                                {
                                    dialogueManager.StartConversation(dialogs, objective.end_game_object.first_name, objective.end_game_object.last_name);
                                }

                                //post request to backend that this objective is complete
                                await GooruManager.Instance.CompleteObjective(userObjectiveId, questServiceObject);

                                if (onObjectiveCompleted != null)
                                {
                                    onObjectiveCompleted();
                                }

                                //hide quest marker on current ObjectiveObject
                                foreach (GameObject objectiveObject in objectiveObjects)
                                {
                                    if (objectiveObject.GetComponent<ObjectiveObject>().npcId == objective.end_game_object_id)
                                    {
                                        objectiveObject.GetComponentInChildren<NPCQuestMarker>(true).gameObject.SetActive(false);
                                        Debug.Log($"(Quest Manager) <color={debugColor}>Objective {objective.title} Complete!</color>");
                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"(Quest Manager) Failed to complete objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                                return;
                            }

                            //this objective is the final one in the quest
                            if (objective.sequence == quest.objectives.Count)
                            {
                                dialogueManager.SetAtEndOfQuest(true);
                                Debug.Log($"(Quest Manager) <color={debugColor}>Final objective complete!</color>");

                                if (onQuestComplete != null)
                                {
                                    onQuestComplete();
                                }

                                int nextQuestId = 0;
                                try
                                {
                                    nextQuestId = await GetNextQuestId(quest.user_quest.id);
                                    Debug.Log($"(Quest Manager) <color={debugColor}>NextQuestId: {nextQuestId} for Quest: {quest.user_quest.id}</color>");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"(Quest Manager) GetNextQuestId failed with nextQuestId: {quest.user_quest.id}. \n Error: {e.Message}");
                                }

                                //there is another quest in the sequence
                                if (nextQuestId != 0)
                                {
                                    try
                                    {
                                        await StartQuest(nextQuestId);

                                        if (onQuestStarted != null)
                                        {
                                            onQuestStarted();
                                        }

                                        Debug.Log($"(Quest Manager) <color={debugColor}>Starting new quest: {nextQuestId}</color>");
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"(Quest Manager) StartQuest failed with nextQuestId: {nextQuestId}. \n Error: {e.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.Log($"(Quest Manager) <color={debugColor}>Final Quest Complete!</color>");
                                    ActiveQuestButton.Instance.SetFinishedAllQuestsButtonText();
                                }
                                questList.Remove(quest);
                            }
                            //this objective is the not final one in the quest
                            else
                            {
                                currentSequence = quest.objectives.FindIndex(a => a.sequence == objective.sequence + 1);
                                await StartObjective(quest.id, quest.objectives[currentSequence].user_objective.id);
                                Debug.Log($"(Quest Manager) <color={debugColor}>(UpdateQuestInLog) QuestId: {quest.id} | UserObjectiveId: {quest.objectives[currentSequence].user_objective.id}</color>");
                                ActiveQuestButton.Instance.SetActiveQuestButtonText("", quest.objectives[currentSequence].title);
                            }
                            return;
                        }
                    }
                    Debug.Log("(Quest Manager) <color={debugColor}>Objective:" + userObjectiveId + " not found.</color>");
                    return;
                }
            }
        }


        //NPC
        public void AddToObjectiveObjectsList(GameObject objectiveObject)
        {
            objectiveObjects.Add(objectiveObject);
            Debug.Log($"(Quest Manager) <color={debugColor}>{objectiveObject.GetComponent<ObjectiveObject>().npcId} added to ObjectiveObjects list!</color>");
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
                }
            }
            return dialogueText;
        }
    }
}
