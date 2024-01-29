using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : Singleton<UIController>
{
    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_avtarImag;

    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_avtarImag;

    public void ShowPlayerInfo(PlayerInfo player1_info, PlayerInfo player2_info)
    {
        player1_nameText.text = player1_info.userName;
        player1_avtarImag.sprite = ProfileManager.Instance.GetSprite(player1_info.avtarIndex);

        player2_nameText.text = player2_info.userName;
        player2_avtarImag.sprite = ProfileManager.Instance.GetSprite(player2_info.avtarIndex);
    }
}


