using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [Header("Network Manager")]
    [SerializeField] private NetworkManager networkManager;

    [SerializeField] private GameDataSO gameDataSO;

    private void Start()
    {
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        MenuPageManager.Instance.OpenPage(MenuPageType.MainMenu);
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
        if (status)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.MainMenu);
        }
        else
        {
            MenuPageManager.Instance.CloseCurrentPage();
        }
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
}
