using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
using Gameplay;

public class GameplayUIController : Singleton<GameplayUIController>
{
    [Header("Player1 profile")]
    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_avtarImag;
    [SerializeField] private Image player1_timerImg;

    [Header("Player2 profile")]
    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_avtarImag;
    [SerializeField] private Image player2_timerImg;

    [Header("Game Win screens")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private Transform winScreenCoinImg;
    [SerializeField] private GameObject winScreenReamatchWithCoin;
    [SerializeField] private GameObject winScreenReamatchWithoutCoin;

    [Header("Game Lose screens")]
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private GameObject loseScreenReamatchWithCoin;
    [SerializeField] private GameObject loseScreenReamatchWithoutCoin;


    [Header("Game Over screens")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI gameOverMsgText;
    [SerializeField] private GameObject gameOverScreenReamatchWithCoin;
    [SerializeField] private GameObject gameOverScreenReamatchWithoutCoin;

    [Header("Msg screens")]
    [SerializeField] private GameObject msgScreen;
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject msgHomeButton;
    [SerializeField] private GameObject loadingBar;
    [SerializeField] public GameObject msgScreenfeeImg;

    [Space(15)]
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private GameObject coinDisplay;
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private GameObject rematchScreen;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private GameObject upperStrip;
    [SerializeField] private GameObject retryButton;


    public void SetUpScreens()
    {
        if(GameManager.Instance.GameMode == GameMode.Online || GameManager.Instance.GameMode == GameMode.PVC)
        {
            winScreenReamatchWithCoin.SetActive(true);
            winScreenReamatchWithoutCoin.SetActive(false);

            loseScreenReamatchWithCoin.SetActive(true);
            loseScreenReamatchWithoutCoin.SetActive(false);

            gameOverScreenReamatchWithCoin.SetActive(true);
            gameOverScreenReamatchWithoutCoin.SetActive(false);


            if(GameManager.Instance.GameMode == GameMode.Online)
            {
                retryButton.SetActive(false);
            }
            else
            {
                retryButton.SetActive(true);
            }
        }
        else if (GameManager.Instance.GameMode == GameMode.PVP)
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

    public void ShowPlayerInfo(string player1_name, Sprite player1_Avtar, string player2_name, Sprite player2_Avtar)
    {
        player1_nameText.text = player1_name;
        player1_avtarImag.sprite = player1_Avtar;

        player2_nameText.text = player2_name;
        player2_avtarImag.sprite = player2_Avtar;
    }

    public void DisableAllScreen()
    {
        coinDisplay.SetActive(false);
        faderScreen.SetActive(false);

        winScreen.SetActive(false);
        loseScreen.SetActive(false);
        gameOverScreen.SetActive(false);

        exitScreen.SetActive(false);
        rematchScreen.SetActive(false);
        msgScreen.SetActive(false);
    }

    public void ToggleGameWinScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);

        if (status)
        {
            winScreen.Activate();
            CoinManager.Instance.AddCoin(500, winScreenCoinImg);
        }
        else
        {
            winScreen.Deactivate();
        }
    }

    public void ToggleGameLoseScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);

        if (status)
        {
            loseScreen.Activate();
        }
        else
        {
            loseScreen.Deactivate();
        }
    }

    public void ToggleGameOverScreen(bool status, string winnerName = "", string loserName = "")
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);

        if (status)
        {
            gameOverScreen.Activate();
            gameOverMsgText.text = "The " + winnerName + " piece wins the game.Better luck next time, "+ loserName + " piece!";
        }
        else
        {
            gameOverScreen.Deactivate();
        }
    }

    public void ToggleExitScreen(bool status)
    {
        faderScreen.SetActive(status);

        if(status)
        {
            exitScreen.Activate();
        }
        else
        {
            exitScreen.Deactivate();
        }
    }

    public void ToggleRematchScreen(bool status)
    {
        faderScreen.SetActive(status);
        coinDisplay.SetActive(status);

        if (status)
        {
            rematchScreen.Activate();
        }
        else
        {
            rematchScreen.Deactivate();
        }
    }

    public void ToggleMsgScreen(bool status, string msg = "", bool homeButtonStatus = false)
    {
        faderScreen.SetActive(status);
        msgScreen.SetActive(status);
        coinDisplay.SetActive(status);
        msgText.text = msg;
        msgHomeButton.SetActive(homeButtonStatus);
        loadingBar.SetActive(!homeButtonStatus);
        msgScreenfeeImg.SetActive(false);
    }

    public void RematchForOnlineMode()
    {
        if(msgScreen.activeSelf)
        {
            msgHomeButton.SetActive(false);
            msgScreenfeeImg.SetActive(true);

            CoinManager.Instance.DeductCoin(250, msgScreenfeeImg.transform, () => 
            {
                DisableAllScreen();
                AudioManager.Instance.StopTimeTickingSound();
                StartCoroutine(GameManager.Instance.Rematch());
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
        AudioManager.Instance.PlayButtonClickSound();

        if (GameManager.Instance.GameMode != GameMode.PVP && CoinManager.Instance.GetCoinAmount() < 250)
        {
            PersistentUI.Instance.shopScreen.gameObject.SetActive(true);
            return;
        }

        if (GameManager.Instance.GameMode == GameMode.Online)
        {
            DisableAllScreen();
            ToggleMsgScreen(true, "waiting for opponent confirmation");
            eventManager.SendRematchConfirmationEvent();
        }
        else if (GameManager.Instance.GameMode == GameMode.PVP)
        {
            DisableAllScreen();
            StartCoroutine(GameManager.Instance.Rematch());
        }
        else
        {
            CoinManager.Instance.DeductCoin(250, coinImgTran, () =>
            {
                DisableAllScreen();
                StartCoroutine(GameManager.Instance.Rematch());
            });
        }
    }

    public void OnRetryButtonClick()
    {
        DisableAllScreen();
        AudioManager.Instance.StopTimeTickingSound();
        StartCoroutine(GameManager.Instance.Rematch());
    }

    public void OnRematchYesButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();

        if (CoinManager.Instance.GetCoinAmount() < 250)
        {
            PersistentUI.Instance.shopScreen.gameObject.SetActive(true);
            return;
        }

        DisableAllScreen();
        eventManager.SendRematchAcceptEvent();
        ToggleMsgScreen(true, "waiting for match restart");
    }

    public void OnRematchNoButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        DisableAllScreen();

        eventManager.SendRematchDeniedEvent();

        OnExitScreenYesButtonClick();
    }

    public bool CanOpenGameOverScreen()
    {
        return !(winScreen.activeSelf || loseScreen.activeSelf || msgScreen.activeSelf);
    }

    public void OnExitScreenYesButtonClick()
    {
        if (GameManager.Instance.GameMode == GameMode.Online && PhotonNetwork.IsConnected)
        {
            GameManager.Instance.IsReadyToLeaveGameplay = true;
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
            ToggleExitScreen(false);
            AudioManager.Instance.PlayButtonClickSound();
            AudioManager.Instance.StopTimeTickingSound();

            StartCoroutine(LoadMainMenu());
        }
        else
        {
            AudioManager.Instance.PlayButtonClickSound();
            StartCoroutine(LoadMainMenu());
        }
    }

    public IEnumerator LoadMainMenu()
    {
        AudioManager.Instance.StopTimeTickingSound();
        DisableAllScreen();
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen();

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(0);
    }

    public void OnPauseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleExitScreen(true);
    }

    public void OnExitScreenCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleExitScreen(false);
    }

    public void OnHintButtonClick()
    {
        Player player = GameManager.Instance.GetPlayer(GameManager.Instance.CurrentTurn);
        player.ShowMoveHint();
    }
}


