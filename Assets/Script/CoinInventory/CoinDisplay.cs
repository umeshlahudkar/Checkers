using System.Collections;
using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinValueText;
    [SerializeField] private Transform coinImgTran;

    private readonly int iteration = 10;
    private Coroutine animCoroutine;
    private readonly WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);

    private void OnEnable()
    {
        CoinManager.OnCoinValueIncreased += IncrementCoin;
        CoinManager.OnCoinValueDecreased += DecrementCoin;
    }

    private void IncrementCoin(int totalCoin, int amountChanged)
    {
        if(animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinIncrementAnim(totalCoin, amountChanged));
    }

    private void DecrementCoin(int totalCoin, int amountChanged)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinDecrementAnim(totalCoin, amountChanged));
    }

    private IEnumerator CoinIncrementAnim(int totalCoin, int amountChanged)
    {
        int coinIncreasedPerIteration = amountChanged / iteration;
        int currentIteration = iteration;
        int currentCoin = totalCoin - amountChanged;

        while(currentIteration > 0)
        {
            currentCoin += coinIncreasedPerIteration;
            coinValueText.text = currentCoin.ToString();

            currentIteration--;

            yield return waitForSeconds;
        }

        coinValueText.text = totalCoin.ToString();
    }

    private IEnumerator CoinDecrementAnim(int totalCoin, int amountChanged)
    {
        int coinIncreasedPerIteration = amountChanged / iteration;
        int currentIteration = iteration;
        int currentCoin = totalCoin + amountChanged;

        while (currentIteration > 0)
        {
            currentCoin -= coinIncreasedPerIteration;
            coinValueText.text = currentCoin.ToString();

            currentIteration--;

            yield return waitForSeconds;
        }

        coinValueText.text = totalCoin.ToString();
    }

    private void OnDisable()
    {
        CoinManager.OnCoinValueIncreased -= IncrementCoin;
        CoinManager.OnCoinValueDecreased -= DecrementCoin;
    }
}
