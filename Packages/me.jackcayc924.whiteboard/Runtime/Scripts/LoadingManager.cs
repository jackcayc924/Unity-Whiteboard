using System.Collections;
using UnityEngine;
using TMPro;

namespace JackCayc924.Whiteboard
{
    public class LoadingManager : MonoBehaviour
    {
        public GameObject loadingScreen; // Assign the loading screen GameObject in the Inspector
        public TextMeshProUGUI loadingText; // Assign the loading Text component in the Inspector
        public TextMeshProUGUI loadingStatusText;

        private void Start()
        {
            // Hide the loading screen at the start
            loadingScreen.SetActive(false);
        }

        public void ShowLoadingScreen(string message)
        {
            loadingScreen.SetActive(true);
            loadingText.text = message;
            loadingStatusText.text = "Press `ESC` to cancel pasting image";
        }

        public void HideLoadingScreen()
        {
            loadingScreen.SetActive(false);
        }

        public void ShowLoadingScreenCustomMsg(string msg)
        {
            loadingScreen.SetActive(true);
            loadingText.text = msg;
        }

        public void CancelLoading()
        {
            loadingStatusText.text = "";
        }
    }
}
