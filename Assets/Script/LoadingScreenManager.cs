using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadingScreenManager : Singleton<LoadingScreenManager>
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private TextMeshProUGUI msgText;

    public void ActivateLoadingScreen(string msgToShow = "Loading...")
    {
        ToggleLoadingScreen(true);
        msgText.text = msgToShow;
    }

    public void DeactivateLoadingScreen()
    {
        ToggleLoadingScreen(false);
    }

    private void ToggleLoadingScreen(bool status)
    {
        loadingScreen.SetActive(status);
        faderScreen.SetActive(status);
    }
}
