using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Arbelos;
using System.Linq;

namespace Arbelos
{
    public class DialogPanel : MonoBehaviour
    {
        //private fields
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text messageText;


        private IDialogManager dialogueManager;

        public void SetDialog(string name, string message)
        {
            nameText.text = name;
            messageText.text = message;
        }

        public void CallProgressConversation()
        {
            if (dialogueManager == null)
            {
                IEnumerable<IDialogManager> dialogueManagerList = FindObjectsOfType<MonoBehaviour>().OfType<IDialogManager>();
                dialogueManager = dialogueManagerList.ElementAt(0); //realistically there should only be 1
            }

            dialogueManager.ProgressConversation();
        }
    }
}
