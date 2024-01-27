using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUIController : MonoBehaviour
{
    [Header("Connecting screen")]
    [SerializeField] private GameObject connectingScreen;

    [Header("Main Menu screen")]
    [SerializeField] private GameObject mainMenuScreen;

    [Header("User name input screen")]
    [SerializeField] private GameObject userNameInputScreen;
    [SerializeField] private TMP_InputField usernameInputField;

    [Header("Fader screen")]
    [SerializeField] private GameObject faderScreen;

    [SerializeField] private NetworkManager networkManager;

    private void Start()
    {
        faderScreen.SetActive(true);
        connectingScreen.SetActive(true);
    }

    public void ToggleConnectingScreen(bool status)
    {
        faderScreen.SetActive(status);
        connectingScreen.SetActive(status);
    }

    public void ToggleUserNameInputScreen(bool status)
    {
        faderScreen.SetActive(status);
        userNameInputScreen.SetActive(status);
    }

    public void OnPlayButtonClick()
    {
        if (!NetworkManager.hasUsernameSet)
        {
            ToggleUserNameInputScreen(true);
        }
        else
        {
            networkManager.JoinRandomRoom();
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
        }
    }

    public void OnProfileButtonClick()
    {
        Debug.Log("Profile button click");
    }

    public void OnUsernameTextClick()
    {
        Debug.Log("Username button click");
    }
}
