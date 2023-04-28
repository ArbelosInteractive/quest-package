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
        [SerializeField] private List<Course> courseList = new List<Course>();
        private Quest activeQuest = new Quest();
        private List<ObjectiveObject> objectiveObjects = new List<ObjectiveObject>();

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
            return await GooruManager.Instance.GetQuest(questId);
        }

        public Quest GetQuestFromCourseList(int questId)
        {
            foreach (Course course in courseList)
            {
                if (course.quest.id == questId)
                {
                    return course.quest;
                }
            }

            Debug.Log($"<color={debugColor}>(Quest Manager) Quest: {questId} not found in GetQuestFromCourseList. Null returned.</color>");
            return null;
        }

        private async Task<int> GetNextQuestId(int questId)
        {
            NextQuestResponse nextQuest = await GooruManager.Instance.GetNextQuest(questId);
            return nextQuest.quest_id;
        }

        //TODO:change this to take a courseId and return a collectionId of a certain course
        public string GetCollectionId()
        {
            return "";
        }

        private void SetActiveQuest(Quest quest, int objectiveSequence)
        {
            activeQuest = quest;
            ActiveQuestButton.Instance.SetActiveQuestButtonText(activeQuest.title, activeQuest.objectives[objectiveSequence - 1].title);
        }
        
        //TODO: unused method? figure out if this ok to remove
        // public Quest GetQuestFromQuestListByObjectiveId(int objectiveId)
        // {
        //     foreach (Quest quest in questList)
        //     {
        //         foreach (Objective objective in quest.objectives)
        //         {
        //             if (objective.id == objectiveId)
        //             {
        //                 return quest;
        //             }
        //         }
        //     }
        //
        //     Debug.Log($"<color={debugColor}>(Quest Manager) Objective: {objectiveId} not found in GetQuestFromQuestListByObjectiveId. Null returned.</color>");
        //     return null;
        // }

        //TODO:currently there is only 1 course (quest line) from the backend, this method needs to be upgraded in the future to support multiple
        //TODO: rename this method "InitializeCourses" after finishing everything else to avoid redoing reference in inspector
        public async Task InitializeQuests()
        {
            try
            {
                List<Quest> quests = await GetAllQuests();
                quests.Sort((x, y) => x.user_quest.sequence.CompareTo(y.user_quest.sequence));

                foreach (Quest quest in quests)
                {
                    if (quest.user_quest.sequence == 1 && quest.user_quest.progress == 0)
                    {
                        Course course = new Course
                        {
                            id = 0,
                            title = "Course Title",
                            quest = quests[0],
                            questSequence = 1,
                            objectiveSequence = 1,
                            collectionId = ""
                        };
                        await StartCourse(course);
                        Debug.Log($"<color={debugColor}>(Quest Manager) First quest in sequence starting: {quest.id} | {quest.title}</color>");
                        return;
                    }

                    if (quest.user_quest.progress < 100)
                    {
                        quest.objectives.Sort((x, y) => x.sequence.CompareTo(y.sequence));
                        foreach (Objective objective in quest.objectives)
                        {
                            if (objective.user_objective.progress < 100)
                            {
                                Course course = new Course
                                {
                                    id = 0,
                                    title = "Course Title",
                                    quest = quests[quest.user_quest.sequence - 1],
                                    questSequence = quest.user_quest.sequence,
                                    objectiveSequence = objective.sequence,
                                    collectionId = ""
                                };
                                await StartCourse(course);
                                Debug.Log($"<color={debugColor}>(Quest Manager) Found quest with progress under 100: {quest.id} | {quest.title}</color>");
                                return;
                            }
                        }
                    }
                }
                ActiveQuestButton.Instance.SetFinishedAllQuestsButtonText();
                Debug.Log($"<color={debugColor}>(Quest Manager) No quest found with less than 100 progress. All Quests finished!</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"(Quest Manager) InitializeQuests failed: {e.Message}");
            }
        }

        private async Task StartCourse(Course course)
        {
            courseList.Add(course);
            await StartQuest(course.quest.id, course.objectiveSequence);
            Debug.Log($"<color={debugColor}>(Quest Manager) Starting course: {course.id} | {course.title} at objective: {course.objectiveSequence}</color>");
        }
        
        public async Task StartQuest(int questId, int objectiveSequence)
        {
            if (GetQuestFromCourseList(questId) != null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: " + questId + " is already in the CourseList!</color>");
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
                SetActiveQuest(newQuest, 1);
                await StartObjective(newQuest.id, newQuest.objectives[objectiveSequence].user_objective.id);

                //Quest Started Event
                onQuestStarted?.Invoke();
                
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest Started: {questId}</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"(Quest Manager) Failed to start quest: {questId} \n {e.Message}");
            }
        }

        private async Task StartObjective(int questId, int userObjectiveId)
        {
            foreach (Course course in courseList)
            {
                if (course.quest.id != questId) continue;
                foreach (Objective objective in course.quest.objectives)
                {
                    if (objective.user_objective.id != userObjectiveId) continue;
                    try
                    {
                        await GooruManager.Instance.StartObjective(userObjectiveId);
                        onObjectiveStarted?.Invoke();
                                
                        foreach (ObjectiveObject objectiveObject in objectiveObjects)
                        {
                            if (objectiveObject.npcId != objective.end_game_object_id) continue;
                            objectiveObject.SetInfo(questId, objective);
                            objectiveObject.ShowNPCQuestMarker();

                            try
                            {
                                course.collectionId = objective.navigator_objective_detail.collection_id;
                            }
                            catch
                            {
                                Debug.Log($"<color={debugColor}>(Quest Manager) CollectionId is null for user_objective: {objective.user_objective.id}</color>");
                            }

                            Debug.Log($"<color={debugColor}>(Quest Manager) Objective: ({objective.title}) started!</color>");
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"(Quest Manager) Failed to start objective: UserObjectiveId: {userObjectiveId} in Quest: {course.quest.id} \n{e.Message}");
                    }
                }
                Debug.Log($"<color={debugColor}>(Quest Manager) Objective: {userObjectiveId} not found.</color>");
                return;
            }
        }


        public async Task CompleteObjective(int questId, int userObjectiveId)
        {
            QuestServiceObject questServiceObject = new QuestServiceObject();

            //an assessment is completed without the appropriate quest in the list
            if (GetQuestFromCourseList(questId) == null)
            {
                Debug.Log($"<color={debugColor}>(Quest Manager) Quest: {questId} is not in the questList. Completing without quest.</color>");
                questServiceObject.withQuest = "false";
                await GooruManager.Instance.CompleteObjective(userObjectiveId, questServiceObject);
                return;
            }

            //complete objective with quest in the course list
            foreach (Course course in courseList)
            {
                if (course.quest.id != questId) continue;
                
                foreach (Objective objective in course.quest.objectives)
                {
                    if (objective.user_objective.id != userObjectiveId) continue;
                    
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
                        
                        //Complete Objective Event
                        onObjectiveCompleted?.Invoke();

                        //hide quest marker on current ObjectiveObject
                        foreach (ObjectiveObject objectiveObject in objectiveObjects)
                        {
                            if (objectiveObject.npcId != objective.end_game_object_id) continue;
                            objectiveObject.GetComponentInChildren<NPCQuestMarker>(true).gameObject.SetActive(false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"(Quest Manager) Failed to complete objective: UserObjectiveId: {userObjectiveId} in Quest: {course.quest.id} \n{e.Message}");
                        return;
                    }
                    
                    Debug.Log($"<color={debugColor}>(Quest Manager) Objective {objective.title} Complete!</color>");

                    //this objective is the final one in the quest
                    if (objective.sequence == course.quest.objectives.Count)
                    {
                        dialogueManager.SetAtEndOfQuest(true);
                        Debug.Log($"<color={debugColor}>(Quest Manager) Final objective complete!</color>");
                        
                        //Quest Complete Event
                        onQuestComplete?.Invoke();

                        int nextQuestId = 0;
                        try
                        {
                            nextQuestId = await GetNextQuestId(course.quest.user_quest.id);
                            Debug.Log($"<color={debugColor}>(Quest Manager) NextQuestId: {nextQuestId} for Quest: {course.quest.user_quest.id}</color>");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"(Quest Manager) GetNextQuestId failed with nextQuestId: {course.quest.user_quest.id}. \n Error: {e.Message}");
                        }

                        //there is another quest in the sequence
                        if (nextQuestId != 0)
                        {
                            try
                            {
                                await StartQuest(nextQuestId, 1);
                                Debug.Log($"<color={debugColor}>(Quest Manager) Starting new quest: {nextQuestId}</color>");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"(Quest Manager) StartQuest failed with nextQuestId: {nextQuestId}. \n Error: {e.Message}");
                            }
                        }
                        //there are no more quests in the sequence, course finished!
                        else
                        {
                            Debug.Log($"<color={debugColor}>(Quest Manager) Final Quest Complete!</color>");
                            ActiveQuestButton.Instance.SetFinishedAllQuestsButtonText();
                            courseList.Remove(course);
                        }
                    }
                    //this objective is the not final one in the quest
                    else
                    {
                        course.objectiveSequence = course.quest.objectives.FindIndex(a => a.sequence == objective.sequence + 1);
                        await StartObjective(course.quest.id, course.quest.objectives[course.objectiveSequence].user_objective.id);
                        Debug.Log($"<color={debugColor}>(Quest Manager) QuestId: {course.quest.id} | UserObjectiveId: {course.quest.objectives[course.objectiveSequence].user_objective.id}</color>");
                        ActiveQuestButton.Instance.SetActiveQuestButtonText("", course.quest.objectives[course.objectiveSequence].title);
                    }
                    return;
                }
                Debug.Log($"<color={debugColor}>(Quest Manager) Objective: {userObjectiveId} not found.</color>");
                return;
            }
        }

        //NPC
        public void AddToObjectiveObjectsList(ObjectiveObject objectiveObject)
        {
            objectiveObjects.Add(objectiveObject);
            Debug.Log($"<color={debugColor}>(Quest Manager) NPC: {objectiveObject.npcDisplayName} added to ObjectiveObjects list!</color>");
        }

        private List<string> GetDialogueData(Objective objective)
        {
            List<string> dialogueText = new List<string>();

            if (objective.objective_dialog != null)
            {
                List<Message> messages = objective.objective_dialog.messages;
                messages.Sort((x,y) => x.sequence.CompareTo(y.sequence));
                
                //TODO: remove this line if new sorting works
                //List<Message> sortedMessages = messages.OrderBy(message => message.sequence).ToList();
                
                foreach (var message in messages)
                {
                    dialogueText.Add(message.text);
                }
            }
            return dialogueText;
        }
    }
}
