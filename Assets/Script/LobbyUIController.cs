
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUIController : MonoBehaviour
{
    [Header("Main Menu screen")]
    [SerializeField] private GameObject mainMenuScreen;

    [Header("Profile screen")]
    [SerializeField] private GameObject userNameInputScreen;
    [SerializeField] private GameObject avtarSelectionScreen;
    [SerializeField] private TMP_InputField usernameInputField;

    [Header("Fader screen")]
    [SerializeField] private GameObject faderScreen;

    [Header("MatchMaking screen")]
    [SerializeField] private GameObject matchmakingScreen;

    [Header("Network Manager")]
    [SerializeField] private NetworkManager networkManager;

    [Header("Avtar Controller")]
    [SerializeField] private AvtarController avtarController;

    private void Start()
    {
        LoadingScreenManager.Instance.ActivateLoadingScreen("Connecting to network...");
    }

    public void SetProfile()
    {
        LoadingScreenManager.Instance.DeactivateLoadingScreen();
        if (!ProfileManager.Instance.HasUserNameSet)
        {
            ToggleUserNameInputScreen(true);
        }
        else if(!ProfileManager.Instance.HasAvtarSet)
        {
            ToggleAvtarSelectionScreen(true);
        }
    }

    public void ToggleUserNameInputScreen(bool status)
    {
        faderScreen.SetActive(status);
        userNameInputScreen.SetActive(status);
    }

    public void ToggleAvtarSelectionScreen(bool status)
    {
        faderScreen.SetActive(status);
        avtarSelectionScreen.SetActive(status);
    }

    public void OnPlayButtonClick()
    {
        if (!ProfileManager.Instance.HasUserNameSet || !ProfileManager.Instance.HasAvtarSet)
        {
            SetProfile();
        }
        else
        {
            networkManager.JoinRandomRoom();
            LoadingScreenManager.Instance.ActivateLoadingScreen();
        }
    }

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }

    public void OnUsernameSaveButtonClick()
    {
        string username = usernameInputField.text;
        if (!string.IsNullOrEmpty(username))
        {
            mainMenuScreen.SetActive(true);

            faderScreen.SetActive(false);
            userNameInputScreen.SetActive(false);

            networkManager.SetUserName(username);
            ProfileManager.Instance.SetUserName(username);

            SetProfile();
        }
    }

    public void OnAvtarSaveButtonClick()
    {
        avtarController.SaveAvtar();
        ToggleAvtarSelectionScreen(false);

        SetProfile();
    }

    public void ToggleMatchmakingScreen(bool status)
    {
        if(status == true)
        {
            mainMenuScreen.SetActive(false);
        }
        matchmakingScreen.SetActive(status);
    }

}
