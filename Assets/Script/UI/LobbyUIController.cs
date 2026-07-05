using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [Header("Main Menu screen")]
    [SerializeField] private GameObject mainMenuScreen;

    [Header("Network Manager")]
    [SerializeField] private NetworkManager networkManager;

    [SerializeField] private GameDataSO gameDataSO;

    private void Start()
    {
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
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

    public void SetGameMode(GameMode gameMode)
    {
        gameDataSO.gameMode = gameMode;
    }

    public void ToggleMainMenuScreen(bool status)
    {
        MenuPageManager.Instance.CloseCurrentPage();
        mainMenuScreen.SetActive(status);
    }

    public void SetProfile()
    {
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        if (!ProfileManager.Instance.HasUserNameSet)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.UserNameInput);
        }
        else if(!ProfileManager.Instance.HasAvtarSet)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.AvtarSelection);
        }
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
            PersistentUI.Instance.shopScreen.Open();
            return;
        }

        MenuPageManager.Instance.OpenPage(MenuPageType.ModeSelection);
    }

    public void JoinRoom()
    {
        if(networkManager.JoinRandomRoom())
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.Matchmaking);
        }
        else
        {
            PersistentUI.Instance.massageDisplay.ShowMassage("No internet connection!");
        }
    }

    public void OnQuitButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Application.Quit();
    }

    public void OnAvtarButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.AvtarSelection);
    }

    public void OnUsernameClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.UserNameInput);
    }

    public void OnSettingButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.Setting);
    }
}
