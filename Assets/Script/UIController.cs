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

    [Header("Fader screen")]
    [SerializeField] private GameObject faderScreen;

    [Header("Game Over screens")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [Header("Coin Display")]
    [SerializeField] private GameObject coinDisplay;

    [Header("Exit screen")]
    [SerializeField] private GameObject exitScreen;

    public void ShowPlayerInfo(PlayerInfo player1_info, PlayerInfo player2_info)
    {
        player1_nameText.text = player1_info.userName;
        player1_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player1_info.avtarIndex);

        player2_nameText.text = player2_info.userName;
        player2_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player2_info.avtarIndex);
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


