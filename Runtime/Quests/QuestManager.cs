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
        private string debugColor = "#e8d168";

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

            Debug.Log($"<color={debugColor}>Quest: " + questId + " not found in GetQuestFromList. Null returned.</color>");
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

            Debug.Log($"<color={debugColor}>Objective: " + objectiveId + " not found in GetQuestFromQuestListByObjectiveId. Null returned.</color>");
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
                        Debug.Log($"<color={debugColor}> First quest in sequence starting: {quest.id} | {quest.title}</color>");
                        await StartQuest(quest.id);
                        return;
                    }
                    else
                    {
                        if (quest.user_quest.progress < 100)
                        {
                            Debug.Log($"<color={debugColor}> Found quest with progress under 100: {quest.id} | {quest.title}</color>");
                            await ResumeQuest(quest);
                            return;
                        }
                    }
                }
                Debug.Log($"<color={debugColor}> No quest found with less than 100 progress. All Quests finished???</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"RestoreQuestProgress failed: {e.Message}");
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

                    Debug.Log($"<color=blue>{currentSequence}</color>");

                    await StartObjective(quest.id, quest.objectives[currentSequence].user_objective.id);
                    questLog.AddQuestToLog(quest, currentSequence);
                    Debug.Log($"<color={debugColor}>Resuming quest: {quest.id} at user-objective: {objective.user_objective.id}</color>");
                    return;
                }
            }
        }

        public async Task StartQuest(int questId)
        {
            if (GetQuestFromQuestList(questId) != null)
            {
                Debug.Log($"<color={debugColor}>Quest: " + questId + " is already in the QuestList!</color>");
                return;
            }

            Quest newQuest = await GetQuest(questId);
            if (newQuest == null)
            {
                Debug.Log($"<color={debugColor}>Quest: " + questId + " returned null from the server!</color>");
                return;
            }

            try
            {
                await gooruManager.StartQuest(newQuest.user_quest.id);
                questList.Add(newQuest);
                int index = newQuest.objectives.FindIndex(a => a.sequence == 1);
                await StartObjective(newQuest.id, newQuest.objectives[index].user_objective.id);
                questLog.AddQuestToLog(newQuest);
                Debug.Log($"<color={debugColor}>Quest Started: {questId}</color>");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start quest: questId");
                throw e;
            }
        }

        public async Task StartObjective(int questId, int userObjectiveId)
        {
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log($"<color={debugColor}>Quest: {questId} is not in the questList. StartObjective: {userObjectiveId} failed.</color>");
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
                                            Debug.Log($"<color={debugColor}>CollectionId is null for user_objective: {objective.user_objective.id}</color>");
                                        }

                                        //show StartOfObjectiveDialog here in the future

                                        Debug.Log($"<color={debugColor}>Objective: ({objective.title}) started!</color>");
                                        return;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to start objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                            }
                        }
                    }
                    Debug.Log($"<color={debugColor}>Objective: {userObjectiveId} not found.</color>");
                    return;
                }
            }
        }


        public async Task CompleteObjective(int questId, int userObjectiveId)
        {
            QuestServiceObject questServiceObject = new QuestServiceObject();
            if (GetQuestFromQuestList(questId) == null)
            {
                Debug.Log($"<color={debugColor}>Quest: {questId} is not in the questList. Completing without quest.</color>");
                questServiceObject.withQuest = "false";
                await gooruManager.CompleteObjective(userObjectiveId, questServiceObject);
                return;
            }

            foreach (Quest quest in questList)
            {
                if (quest.id == questId)
                {
                    Debug.Log($"<color={debugColor}>Quest Id: {quest.id}</color>");
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
                                    DialogueManager.Instance.StartConversation(dialogs, objective.end_game_object.firstName, objective.end_game_object.lastName);
                                }

                                await gooruManager.CompleteObjective(userObjectiveId, questServiceObject);
                                foreach (GameObject objectiveObject in objectiveObjects)
                                {
                                    if (objectiveObject.GetComponent<ObjectiveObject>().npcId == objective.end_game_object_id)
                                    {
                                        objectiveObject.GetComponentInChildren<NPCQuestMarker>(true).gameObject.SetActive(false);
                                        Debug.Log($"<color={debugColor}>Objective {objective.title} Complete!</color>");
                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to complete objective: UserObjectiveId: {userObjectiveId} in Quest: {quest.id} \n{e.Message}");
                                return;
                            }

                            if (objective.sequence == quest.objectives.Count)
                            {
                                questLog.RemoveQuestFromLog(quest.id);
                                DialogueManager.Instance.SetAtEndOfQuest(true);
                                Debug.Log("Final objective complete!");

                                int nextQuestId = 0;
                                try
                                {
                                    nextQuestId = await GetNextQuestId(quest.user_quest.id);
                                    Debug.Log($"<color={debugColor}>NextQuestId: {nextQuestId} for Quest: {quest.user_quest.id}</color>");
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
                                        Debug.Log($"<color={debugColor}>Starting new quest: {nextQuestId}</color>");
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"StartQuest failed with nextQuestId: {nextQuestId}. \n Error: {e.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.Log($"<color={debugColor}>Final Quest Complete: NextQuestId: {nextQuestId} | UserQuestId: {quest.user_quest.id}</color>");
                                }
                                questList.Remove(quest);
                            }
                            else
                            {
                                currentSequence = quest.objectives.FindIndex(a => a.sequence == objective.sequence + 1);
                                await StartObjective(quest.id, quest.objectives[currentSequence].user_objective.id);
                                Debug.Log($"<color={debugColor}>(UpdateQuestInLog) QuestId: {quest.id} | UserObjectiveId: {quest.objectives[currentSequence].user_objective.id}</color>");
                                questLog.UpdateQuestInLog(quest.id, quest.objectives[currentSequence].user_objective.id);
                            }
                            return;
                        }
                    }
                    Debug.Log("<color={debugColor}>Objective:" + userObjectiveId + " not found.</color>");
                    return;
                }
            }
        }


        //NPC
        public void AddToObjectiveObjectsList(GameObject objectiveObject)
        {
            objectiveObjects.Add(objectiveObject);
            Debug.Log($"<color={debugColor}>{objectiveObject.GetComponent<ObjectiveObject>().npcId} added to ObjectiveObjects list!</color>");
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
                    Debug.Log($"<color={debugColor}>Message: </color>{message.text}</color>");
                }
            }

            return dialogueText;
        }
    }
}
