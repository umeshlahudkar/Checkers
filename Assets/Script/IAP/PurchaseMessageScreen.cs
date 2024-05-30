using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PurchaseMessageScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI screenLabelText;
    [SerializeField] private TextMeshProUGUI msgText;
    public Transform coinImg;

    public void InitScreen(string screenLabel, string msg)
    {
        screenLabelText.text = screenLabel;
        msgText.text = msg;
        gameObject.Activate();
    }

    public void OnCloseButtonClick()
    {
        gameObject.Deactivate();
    }
}
