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

    private void IncrementCoin(int totalCoin, int amountChanged, Transform target)
    {
        if(animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinIncrementAnim(totalCoin, amountChanged, target));
    }

    private void DecrementCoin(int totalCoin, int amountChanged, Transform target)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinDecrementAnim(totalCoin, amountChanged, target));
    }

    private IEnumerator CoinIncrementAnim(int totalCoin, int amountChanged, Transform target)
    {
        if (target != null)
        {
            CoinAnimator anim = Instantiate<CoinAnimator>(CoinManager.Instance.GetCoinAnimPrefab(),
                            target.position, Quaternion.identity, transform);
            yield return StartCoroutine(anim.PlayAnimation(coinImgTran));
        }

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

    private IEnumerator CoinDecrementAnim(int totalCoin, int amountChanged, Transform target)
    {
        if (target != null)
        {
            CoinAnimator anim = Instantiate<CoinAnimator>(CoinManager.Instance.GetCoinAnimPrefab(),
                          coinImgTran.position, Quaternion.identity, transform);
            StartCoroutine(anim.PlayAnimation(target));
        }

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
