using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinManager : Singleton<CoinManager>
{
    private int totalCoin;
    [SerializeField] private CoinAnimator coinAnimPrefab;

    public delegate void CoinValueChanged(int coins, int amountChanged, Transform target);
    public static event CoinValueChanged OnCoinValueIncreased;
    public static event CoinValueChanged OnCoinValueDecreased;

    private void Start()
    {
        totalCoin = 100;
        OnCoinValueIncreased?.Invoke(totalCoin, 0, null);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            AddCoin(12);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            DeductCoin(18);
        }
    }

    public void AddCoin(int amount, Transform target = null)
    {
        totalCoin += amount;
        totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

        OnCoinValueIncreased?.Invoke(totalCoin, amount, target);
    }

    public void DeductCoin(int amount, Transform target = null)
    {
        if(totalCoin > 0)
        {
            totalCoin -= amount;
            totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

            OnCoinValueDecreased?.Invoke(totalCoin, amount, target);
        }
    }

    public int GetCoinAmount() { return totalCoin; }

    public CoinAnimator GetCoinAnimPrefab() { return coinAnimPrefab; }
}
