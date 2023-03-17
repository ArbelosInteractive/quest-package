using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arbelos
{
    public interface IDialogueManager
    {
        public void SetAtEndOfQuest(bool atEnd);
        public void StartConversation(List<string> dialogues, string firstName, string lastName);
        public void ShowDialogPanel(string npcName, string npcMessage);
        public void HideDialogPanel();
        public void ProgressConversation();
    }
}