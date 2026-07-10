using UnityEngine;
using UnityEngine.Events;

public delegate void CoinAnimationCompleteEvent();

public class CoinManager : Singleton<CoinManager>
{
    private int totalCoin;
    [SerializeField] private CoinAnimator coinAnimPrefab;

    public delegate void CoinValueChanged(int coins, int amountChanged, Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete);
    public static event CoinValueChanged OnCoinValueIncreased;
    public static event CoinValueChanged OnCoinValueDecreased;

    private void Start()
    {
        totalCoin = 0;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
        totalCoin = data.coins;
#endif

        OnCoinValueIncreased?.Invoke(totalCoin, 0, null, null);
    }

    public void AddCoin(int amount, Transform coinMovetarget = null, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        totalCoin += amount;
        totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

        OnCoinValueIncreased?.Invoke(totalCoin, amount, coinMovetarget, OnCoinAnimationComplete);
        AudioManager.Instance.PlayCoinSound();

        SaveCoin();
    }

    public void DeductCoin(int amount, Transform target = null, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        if(totalCoin > 0)
        {
            totalCoin -= amount;
            totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

            OnCoinValueDecreased?.Invoke(totalCoin, amount, target, OnCoinAnimationComplete);
            AudioManager.Instance.PlayCoinSound();

            SaveCoin();
        }
    }

    private void SaveCoin()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
        data.coins = totalCoin;
        SavingSystem.Save(ProfileData.FileName, data);
#endif
    }

    public int GetCoinAmount() { return totalCoin; }

    public CoinAnimator GetCoinAnimPrefab() { return coinAnimPrefab; }
}
