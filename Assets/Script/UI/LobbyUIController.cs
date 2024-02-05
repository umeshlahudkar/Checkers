using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [Header("Main Menu screen")]
    [SerializeField] private GameObject mainMenuScreen;

    [Header("Profile screen")]
    [SerializeField] private GameObject userNameInputScreen;
    [SerializeField] private GameObject avtarSelectionScreen;

    [Header("Fader screen")]
    [SerializeField] private GameObject faderScreen;

    [Header("MatchMaking screen")]
    [SerializeField] private GameObject matchmakingScreen;

    [Header("Network Manager")]
    [SerializeField] private NetworkManager networkManager;

    [Header("Avtar Controller")]
    [SerializeField] private AvtarSelectionScreen avtarController;

    [Header("Setting screen")]
    [SerializeField] private GameObject settingScreen;

    [SerializeField] private GameDataSO gameDataSO;

    private void Start()
    {
        //PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Connecting to network...");
    }

    public void SetPlayerData(Player opponentPlayer, int avtarIndex)
    {
        gameDataSO.ownPlayer.isMasterClient = PhotonNetwork.IsMasterClient;
        gameDataSO.ownPlayer.userName = ProfileManager.Instance.GetUserName();
        gameDataSO.ownPlayer.avtarIndex = ProfileManager.Instance.GetProfileAvtarIndex();

        gameDataSO.opponentPlayer.isMasterClient = opponentPlayer.IsMasterClient;
        gameDataSO.opponentPlayer.userName = opponentPlayer.NickName;
        gameDataSO.opponentPlayer.avtarIndex = avtarIndex;
    }

    private void SetGameMode(GameMode gameMode)
    {
        gameDataSO.gameMode = gameMode;
    }


    private void DisableAllScreen()
    {
        userNameInputScreen.SetActive(false);
        avtarSelectionScreen.SetActive(false);
        faderScreen.SetActive(false);
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
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
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

    public void ToggleMatchmakingScreen(bool status)
    {
        if (status == true)
        {
            //mainMenuScreen.SetActive(false);
        }
        matchmakingScreen.SetActive(status);
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
            PersistentUI.Instance.shopScreen.gameObject.SetActive(true);
            return;
        }
      
        networkManager.JoinRandomRoom();
    }

    public void OnPlay_PVP_ButtonClick()
    {
        SetGameMode(GameMode.PVP);
        SceneManager.LoadScene(1);
    }

    public void OnQuitButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Application.Quit();
    }

    public void OnAvtarButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleAvtarSelectionScreen(true);
    }

    public void OnUsernameClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleUserNameInputScreen(true);
    }

    public void OnSettingButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(true);
        settingScreen.SetActive(true);
    } 
}
