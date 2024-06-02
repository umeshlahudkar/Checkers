using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardScreen : MonoBehaviour
{
    [SerializeField] private RewardInfoSO rewardInfoSO;
    [SerializeField] private RewardDisplay[] rewardDisplays;

    private int weekDays = 7;


    private void OnEnable()
    {
        InitRewardDisplay();
    }

    private void InitRewardDisplay()
    {
        RewardInfo[] rewardInfos = rewardInfoSO.rewards;
        //int rewardDay = SavingSystem.Instance.Load().sessionInfo.currentSessionCount % 7;
        bool[] collectRewrd = SavingSystem.Instance.Load().collectedReward;
        int rewardDay = 7 % (weekDays + 1);
        for (int i = 0; i < rewardDisplays.Length; i++)
        {
            bool isAvilable = (i < rewardDay);
            rewardDisplays[i].InitDisplay(rewardInfos[i].day, rewardInfos[i].rewardCoin, collectRewrd[i], isAvilable);
        }
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        gameObject.SetActive(false);
    }
}
