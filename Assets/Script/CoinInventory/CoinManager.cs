using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinManager : Singleton<CoinManager>
{
    private int totalCoin;

    public delegate void CoinValueChanged(int coins, int amountChanged);
    public static event CoinValueChanged OnCoinValueIncreased;
    public static event CoinValueChanged OnCoinValueDecreased;

    private void Start()
    {
        totalCoin = 100;
        OnCoinValueIncreased?.Invoke(totalCoin, 0);
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

    public void AddCoin(int amount)
    {
        totalCoin += amount;
        totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

        OnCoinValueIncreased?.Invoke(totalCoin, amount);
    }

    public void DeductCoin(int amount)
    {
        totalCoin -= amount;
        totalCoin = Mathf.Clamp(totalCoin, 0, totalCoin);

        OnCoinValueDecreased?.Invoke(totalCoin, amount);
    }

    public int GetCoinAmount() { return totalCoin; }
}
