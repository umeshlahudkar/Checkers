using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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

        coinValueText.text = CoinManager.Instance.GetCoinAmount().ToString();
    }

    private void IncrementCoin(int totalCoin, int amountChanged, Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        if(animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinIncrementAnim(totalCoin, amountChanged, target, OnCoinAnimationComplete));
    }

    private void DecrementCoin(int totalCoin, int amountChanged, Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(CoinDecrementAnim(totalCoin, amountChanged, target, OnCoinAnimationComplete));
    }

    private IEnumerator CoinIncrementAnim(int totalCoin, int amountChanged, Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        if (target != null)
        {
            CoinAnimator anim = Instantiate<CoinAnimator>(CoinManager.Instance.GetCoinAnimPrefab(),
                            target.position, Quaternion.identity, PersistentUI.Instance.transform);
            yield return StartCoroutine(anim.PlayCoinAnimation(coinImgTran, OnCoinAnimationComplete));
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

    private IEnumerator CoinDecrementAnim(int totalCoin, int amountChanged, Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        if (target != null)
        {
            CoinAnimator anim = Instantiate<CoinAnimator>(CoinManager.Instance.GetCoinAnimPrefab(),
                          coinImgTran.position, Quaternion.identity, PersistentUI.Instance.transform);
            StartCoroutine(anim.PlayCoinAnimation(target, OnCoinAnimationComplete));
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
