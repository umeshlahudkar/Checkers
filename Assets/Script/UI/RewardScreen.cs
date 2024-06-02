using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardScreen : MonoBehaviour
{
    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        gameObject.SetActive(false);
    }
}
