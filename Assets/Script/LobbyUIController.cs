
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Shop screen")]
    [SerializeField] private GameObject shopScreen;
    [SerializeField] private Button getButton;
    [SerializeField] private Transform shopScreenCoinImg;

    [Header("Setting screen")]
    [SerializeField] private GameObject settingScreen;

    private void Start()
    {
        LoadingScreenManager.Instance.ActivateLoadingScreen("Connecting to network...");
    }

    private void DisableAllScreen()
    {
        userNameInputScreen.SetActive(false);
        avtarSelectionScreen.SetActive(false);
        faderScreen.SetActive(false);
        shopScreen.SetActive(false);
        settingScreen.SetActive(false);
        matchmakingScreen.SetActive(false);
    }

    public void ToggleMainMenuScreen(bool status)
    {
        DisableAllScreen();
        mainMenuScreen.SetActive(status);
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

    private void ToggleUserNameInputScreen(bool status)
    {
        faderScreen.SetActive(status);
        userNameInputScreen.SetActive(status);
    }

    private void ToggleAvtarSelectionScreen(bool status)
    {
        faderScreen.SetActive(status);
        avtarSelectionScreen.SetActive(status);
    }

    private void ToggleShopScreen(bool status)
    {
        faderScreen.SetActive(status);
        shopScreen.SetActive(status);
    }

    public void ToggleMatchmakingScreen(bool status)
    {
        if (status == true)
        {
            mainMenuScreen.SetActive(false);
        }
        matchmakingScreen.SetActive(status);
    }

    private void ToggleSettingScreen(bool status)
    {
        faderScreen.SetActive(status);
        settingScreen.SetActive(status);
    }

    public void OnPlayButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        if (!ProfileManager.Instance.HasUserNameSet || !ProfileManager.Instance.HasAvtarSet)
        {
            SetProfile();
            return;
        }

        if(CoinManager.Instance.GetCoinAmount() < 250)
        {
            ToggleShopScreen(true);
            return;
        }
      
        networkManager.JoinRandomRoom();
    }

    public void OnQuitButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
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

            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void OnAvtarSaveButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        avtarController.SaveAvtar();
        ToggleAvtarSelectionScreen(false);

        SetProfile();
    }

    public void OnShopScreenGetButtonClick(int coinAmount)
    {
        AudioManager.Instance.PlayButtonClickSound();
        getButton.interactable = false;
        CoinManager.Instance.AddCoin(coinAmount, shopScreenCoinImg);
    }

    public void OnShopScreenCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        getButton.interactable = true;
        ToggleShopScreen(false);
    }

    public void OnAvtarButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleAvtarSelectionScreen(true);
    }

    public void OnAvtarCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleAvtarSelectionScreen(false);
    }

    public void OnUsernameClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleUserNameInputScreen(true);
    }

    public void OnUsernameCloseClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleUserNameInputScreen(false);
    }

    public void OnSettingButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleSettingScreen(true);
    } 

    public void OnSettingScreenCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleSettingScreen(false);
    }
        

}
