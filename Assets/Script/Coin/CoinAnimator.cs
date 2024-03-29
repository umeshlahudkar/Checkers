using System.Collections;
using UnityEngine;

public class CoinAnimator : MonoBehaviour
{
    [SerializeField] private CoinMover[] coinMovers;
    private float radius = 150f;   //50
    private int reachedToTargetCount = 0;

    public IEnumerator PlayCoinAnimation(Transform target, CoinAnimationCompleteEvent OnCoinAnimationComplete = null)
    {
        SpreadCoin();

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].SetTarget(target.position, true, this, 0.5f);
        }

        yield return StartCoroutine(HasAllCoinReachedToTarget());

        OnCoinAnimationComplete?.Invoke();
    }

    private void SpreadCoin()
    {
        for(int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].gameObject.SetActive(true);

            Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(radius/2, radius);
            Vector2 targetPosition = (Vector2)transform.position + randomDirection;

            coinMovers[i].SetTarget(targetPosition, false, this, 0.15f);
        }
    }

    private IEnumerator HasAllCoinReachedToTarget()
    {
        while(reachedToTargetCount < coinMovers.Length)
        {
            yield return null;
        }
    }

    public void IncrementCoinReachedToTarget()
    {
        reachedToTargetCount++;
    }
}
