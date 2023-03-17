using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arbelos
{
    public class NPCQuestMarker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Image questMarkerCloseUp;
        [SerializeField] RectTransform closeUpUI;
        [SerializeField] RectTransform farAwayUI;
        private bool closeUPCache = false;

        public void ChangeMode(bool closeUp)
        {
            closeUPCache = closeUp;
            closeUpUI.gameObject.SetActive(closeUPCache);
            questMarkerCloseUp.gameObject.SetActive(gameObject.activeSelf && closeUPCache);

            farAwayUI.gameObject.SetActive(!closeUPCache);
        }

        private void OnDisable()
        {
            questMarkerCloseUp.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            questMarkerCloseUp.gameObject.SetActive(closeUPCache);
        }
    }
}
