using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    private void Awake()
    {
        instance = this;
    }


    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_pieceImag;

    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_pieceImag;

    public void ShowPlayerInfo(string player1_Name, string player2_Name)
    {
        player1_nameText.text = player1_Name;
        player2_nameText.text = player2_Name;
    }
}


