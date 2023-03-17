using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Arbelos;
using System.Linq;

namespace Arbelos
{
    public class DialoguePanel : MonoBehaviour
    {
        //private fields
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text messageText;

        private IDialogueManager dialogueManager;

        public void SetDialog(string name, string message)
        {
            nameText.text = name;
            messageText.text = message;
        }

        public void CallProgressConversation()
        {
            if (dialogueManager == null)
            {
                IEnumerable<IDialogueManager> dialogueManagerList = FindObjectsOfType<MonoBehaviour>().OfType<IDialogueManager>();
                dialogueManager = dialogueManagerList.ElementAt(0); //realisticlly there should only be 1
            }

            dialogueManager.ProgressConversation();
        }
    }
}
