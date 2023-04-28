using Gooru;

namespace Arbelos
{
    [System.Serializable]
    public class Course
    {
        public int id { get; set; }
        public string title { get; set; }
        public int questSequence { get; set; }
        public int objectiveSequence { get; set; }
        public string collectionId { get; set; }
        public Quest quest { get; set; }
    }
}