using Gooru;
using UnityEngine;

public class Course : MonoBehaviour
{
    public int id { get; set; }
    public int currentQuestSequence { get; set; }
    public int currentObjectiveSequence { get; set; }
    public string currentCollectionId { get; set; }
    public Quest currentQuest { get; set; }
}
