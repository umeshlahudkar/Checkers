using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchMakingManager : MonoBehaviour
{
    [Header("Player 1 info")]
    [SerializeField] private TextMeshProUGUI player1_nametext;
    [SerializeField] private Image player1_profileImg;
    [SerializeField] private TextMeshProUGUI player1_feestext;

    [Header("Player 2 info")]
    [SerializeField] private TextMeshProUGUI player2_nametext;
    [SerializeField] private Image player2_profileImg;
    [SerializeField] private TextMeshProUGUI player2_feestext;

    [Header("Player 1 info")]
    [SerializeField] private TextMeshProUGUI rewardtext;

    private void OnEnable()
    {
        player1_nametext.text = ProfileManager.Instance.GetUserName();
        player1_profileImg.sprite = ProfileManager.Instance.GetSelectedAvtarSprite();
        player1_feestext.text = "Fee : " + 10.ToString();
    }
}
