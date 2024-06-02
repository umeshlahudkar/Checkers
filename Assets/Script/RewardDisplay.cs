using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI coinValueText;
    [SerializeField] private Button collectButton;
    [SerializeField] private GameObject faderImg;

    private int day;
    private int coinValue;

    public void InitDisplay(int day, int value, bool isCollected ,bool interactable)
    {
        this.day = day;
        coinValue = value;

        collectButton.interactable = !isCollected;  
        faderImg.SetActive(!interactable);

        dayText.text = day.ToString();
        coinValueText.text = coinValue.ToString();

        collectButton.onClick.AddListener(OnCollectButtonClick);
    }

    public void OnCollectButtonClick()
    {
        Debug.Log("day " + day + " ::: Amound " + coinValue);

        CoinManager.Instance.AddCoin(coinValue, transform);

        SaveData saveData = SavingSystem.Instance.Load();
        saveData.collectedReward[day-1] = true;
        SavingSystem.Instance.Save(saveData);

        collectButton.interactable = false;
    }

    private void OnDisable()
    {
        collectButton.onClick.RemoveAllListeners();
    }
}
