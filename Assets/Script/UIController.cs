using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class UIController : Singleton<UIController>
{
    [Header("Player1 profile")]
    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_avtarImag;

    [Header("Player2 profile")]
    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_avtarImag;

    [Header("Game Over screens")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [Header("Msg screens")]
    [SerializeField] private GameObject msgScreen;
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject msgHomeButton;

    [Space(15)]
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private GameObject coinDisplay;
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private GameObject rematchScreen;
    [SerializeField] private EventManager eventManager;

    public void ShowPlayerInfo(PlayerInfo player1_info, PlayerInfo player2_info)
    {
        player1_nameText.text = player1_info.userName;
        player1_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player1_info.avtarIndex);

        player2_nameText.text = player2_info.userName;
        player2_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player2_info.avtarIndex);
    }

    public void DisableAllScreen()
    {
        coinDisplay.SetActive(false);
        faderScreen.SetActive(false);

        winScreen.SetActive(false);
        loseScreen.SetActive(false);

        exitScreen.SetActive(false);
        rematchScreen.SetActive(false);
        msgScreen.SetActive(false);
    }

    public void ToggleGameWinScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        winScreen.SetActive(status);
    }

    public void ToggleGameLoseScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        loseScreen.SetActive(status);
    }

    public void ToggleExitScreen(bool status)
    {
        faderScreen.SetActive(status);
        exitScreen.SetActive(status);
    }

    public void ToggleRematchScreen(bool status)
    {
        if(status)
        {
            ToggleGameWinScreen(false);
            ToggleGameLoseScreen(false);
        }

        faderScreen.SetActive(status);
        rematchScreen.SetActive(status);
    }

    public void ToggleMsgScreen(bool status, string msg = "", bool homeButtonStatus = false)
    {
        faderScreen.SetActive(status);
        msgScreen.SetActive(status);
        msgText.text = msg;
        msgHomeButton.SetActive(homeButtonStatus);
    }

    public void OnRematchButtonClick()
    {
        ToggleGameLoseScreen(false);
        ToggleGameWinScreen(false);

        ToggleMsgScreen(true, "waiting for opponent confirmation");
        eventManager.SendRematchConfirmationEvent();
    }

    public void OnRematchYesButtonClick()
    {
        ToggleGameLoseScreen(false);
        ToggleGameWinScreen(false);

        ToggleMsgScreen(false);

        eventManager.SendRematchAcceptEvent();
    }

    public void OnRematchNoButtonClick()
    {
        ToggleGameLoseScreen(false);
        ToggleGameWinScreen(false);

        ToggleMsgScreen(false);

        eventManager.SendRematchDeniedEvent();

        OnExitScreenYesButtonClick();
    }

    public bool IsGameOverScreenActive()
    {
        return winScreen.activeSelf || loseScreen.activeSelf;
    }

    public void OnExitScreenYesButtonClick()
    {
        if(PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
            ToggleExitScreen(false);
        }
    }
}


