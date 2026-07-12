using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;

public class GameplayUIController : Service<GameplayUIController>
{
    [Header("Player1 profile")]
    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_avtarImag;
    [SerializeField] private Image player1_timerImg;
    [SerializeField] private TextMeshProUGUI player1_timerText;
    [SerializeField] private GameObject player1_turnBorder;

    [Header("Player2 profile")]
    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_avtarImag;
    [SerializeField] private Image player2_timerImg;
    [SerializeField] private TextMeshProUGUI player2_timerText;
    [SerializeField] private GameObject player2_turnBorder;

    [Header("Turn status")]
    [SerializeField] private TextMeshProUGUI turnStatusText;

    [Header("Game Win screens")]
    [SerializeField] private Transform winScreenCoinImg;
    [SerializeField] private GameObject winScreenReamatchWithCoin;
    [SerializeField] private GameObject winScreenReamatchWithoutCoin;

    [Header("Game Lose screens")]
    [SerializeField] private GameObject loseScreenReamatchWithCoin;
    [SerializeField] private GameObject loseScreenReamatchWithoutCoin;


    [Header("Game Over screens")]
    [SerializeField] private TextMeshProUGUI gameOverMsgText;
    [SerializeField] private GameObject gameOverScreenReamatchWithCoin;
    [SerializeField] private GameObject gameOverScreenReamatchWithoutCoin;

    [Header("Msg screens")]
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject msgHomeButton;
    [SerializeField] private GameObject loadingBar;
    [SerializeField] public GameObject msgScreenfeeImg;

    [Space(15)]
    [SerializeField] private EventManager eventManager;
    [SerializeField] private GameObject upperStrip;
    [SerializeField] private GameObject retryButton;


    public void SetUpScreens()
    {
        if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer || ServiceLocator.Get<GameManager>().GameMode == GameModeType.VsBot)
        {
            winScreenReamatchWithCoin.SetActive(true);
            winScreenReamatchWithoutCoin.SetActive(false);

            loseScreenReamatchWithCoin.SetActive(true);
            loseScreenReamatchWithoutCoin.SetActive(false);

            gameOverScreenReamatchWithCoin.SetActive(true);
            gameOverScreenReamatchWithoutCoin.SetActive(false);


            if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
            {
                retryButton.SetActive(false);
            }
            else
            {
                retryButton.SetActive(true);
            }
        }
        else if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.VsPlayer)
        {
            winScreenReamatchWithCoin.SetActive(false);
            winScreenReamatchWithoutCoin.SetActive(true);

            loseScreenReamatchWithCoin.SetActive(false);
            loseScreenReamatchWithoutCoin.SetActive(true);

            gameOverScreenReamatchWithCoin.SetActive(false);
            gameOverScreenReamatchWithoutCoin.SetActive(true);

            retryButton.SetActive(true);
        }
    }

    public Image GetTimerImg(int playerNumber)
    {
        return (playerNumber == 1) ? player1_timerImg : player2_timerImg;
    }

    public TextMeshProUGUI GetTimerText(int playerNumber)
    {
        return (playerNumber == 1) ? player1_timerText : player2_timerText;
    }

    public void ShowPlayerInfo(string player1_name, Sprite player1_Avtar, string player2_name, Sprite player2_Avtar)
    {
        player1_nameText.text = player1_name;
        player1_avtarImag.sprite = player1_Avtar;

        player2_nameText.text = player2_name;
        player2_avtarImag.sprite = player2_Avtar;
    }

    public void SetActiveTurn(int playerNumber)
    {
        player1_turnBorder.SetActive(playerNumber == 1);
        player2_turnBorder.SetActive(playerNumber == 2);

        if (turnStatusText == null)
        {
            return;
        }

        bool isLocalTurn;
        if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
        {
            Gameplay.Player currentPlayer = ServiceLocator.Get<GameManager>().GetPlayer(playerNumber);
            isLocalTurn = currentPlayer != null && currentPlayer.PhotonView.IsMine;
        }
        else if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.VsBot)
        {
            isLocalTurn = playerNumber == 1;
        }
        else
        {
            isLocalTurn = true;
        }

        string activeName = (playerNumber == 1) ? player1_nameText.text : player2_nameText.text;
        turnStatusText.text = isLocalTurn ? "Your move" : activeName + "'s move";
    }

    public void DisableAllScreen()
    {
        ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
    }

    public void ToggleGameWinScreen(bool status)
    {
        if (status)
        {
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.Win);
            ServiceLocator.Get<CoinManager>().AddCoin(500, winScreenCoinImg);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void ToggleGameLoseScreen(bool status)
    {
        if (status)
        {
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.Lose);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void ToggleGameOverScreen(bool status, string winnerName = "", string loserName = "")
    {
        if (status)
        {
            gameOverMsgText.text = "The " + winnerName + " piece wins the game.Better luck next time, "+ loserName + " piece!";
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.GameOver);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void ToggleExitScreen(bool status)
    {
        if(status)
        {
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.Exit);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void ToggleRematchScreen(bool status)
    {
        if (status)
        {
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.Rematch);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void ToggleMsgScreen(bool status, string msg = "", bool homeButtonStatus = false)
    {
        msgText.text = msg;
        msgHomeButton.SetActive(homeButtonStatus);
        loadingBar.SetActive(!homeButtonStatus);
        msgScreenfeeImg.SetActive(false);

        if (status)
        {
            ServiceLocator.Get<GameplayPageManager>().OpenPage(GameplayPageType.Message);
        }
        else
        {
            ServiceLocator.Get<GameplayPageManager>().CloseCurrentPage();
        }
    }

    public void RematchForOnlineMode()
    {
        if(ServiceLocator.Get<GameplayPageManager>().IsPageOpen(GameplayPageType.Message))
        {
            msgHomeButton.SetActive(false);
            msgScreenfeeImg.SetActive(true);

            ServiceLocator.Get<CoinManager>().DeductCoin(250, msgScreenfeeImg.transform, () =>
            {
                DisableAllScreen();
                ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
                StartCoroutine(ServiceLocator.Get<GameManager>().Rematch());
            });
        }
    }

    public void OnGameLoseRematchButtonClick()
    {
        HandleRematch(loseScreenReamatchWithCoin.transform);
    }

    public void OnGameWinRematchButtonClick()
    {
        HandleRematch(winScreenReamatchWithCoin.transform);
    }

    public void OnGameOverRematchButtonClick()
    {
        HandleRematch(gameOverScreenReamatchWithCoin.transform);
    }

    private void HandleRematch(Transform coinImgTran)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if (ServiceLocator.Get<GameManager>().GameMode != GameModeType.VsPlayer && ServiceLocator.Get<CoinManager>().GetCoinAmount() < 250)
        {
            ServiceLocator.Get<PersistentUI>().shopScreen.Open();
            return;
        }

        if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
        {
            DisableAllScreen();
            ToggleMsgScreen(true, "waiting for opponent confirmation");
            eventManager.SendRematchConfirmationEvent();
        }
        else if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.VsPlayer)
        {
            DisableAllScreen();
            StartCoroutine(ServiceLocator.Get<GameManager>().Rematch());
        }
        else
        {
            ServiceLocator.Get<CoinManager>().DeductCoin(250, coinImgTran, () =>
            {
                DisableAllScreen();
                StartCoroutine(ServiceLocator.Get<GameManager>().Rematch());
            });
        }
    }

    public void OnRetryButtonClick()
    {
        DisableAllScreen();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
        StartCoroutine(ServiceLocator.Get<GameManager>().Rematch());
    }

    public void OnRematchYesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if (ServiceLocator.Get<CoinManager>().GetCoinAmount() < 250)
        {
            ServiceLocator.Get<PersistentUI>().shopScreen.Open();
            return;
        }

        DisableAllScreen();
        eventManager.SendRematchAcceptEvent();
        ToggleMsgScreen(true, "waiting for match restart");
    }

    public void OnRematchNoButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        DisableAllScreen();

        eventManager.SendRematchDeniedEvent();

        OnExitScreenYesButtonClick();
    }

    public bool CanOpenGameOverScreen()
    {
        return !(ServiceLocator.Get<GameplayPageManager>().IsPageOpen(GameplayPageType.Win) || ServiceLocator.Get<GameplayPageManager>().IsPageOpen(GameplayPageType.Lose) || ServiceLocator.Get<GameplayPageManager>().IsPageOpen(GameplayPageType.Message));
    }

    public void OnExitScreenYesButtonClick()
    {
        if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer && PhotonNetwork.IsConnected)
        {
            ServiceLocator.Get<GameManager>().IsReadyToLeaveGameplay = true;
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
            ToggleExitScreen(false);
            ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
            ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

            StartCoroutine(LoadMainMenu());
        }
        else
        {
            ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
            StartCoroutine(LoadMainMenu());
        }
    }

    public IEnumerator LoadMainMenu()
    {
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
        DisableAllScreen();
        ServiceLocator.Get<PersistentUI>().loadingScreen.ActivateLoadingScreen();

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(0);
    }

    public void OnPauseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ToggleExitScreen(true);
    }

    public void OnExitScreenCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ToggleExitScreen(false);
    }
}

