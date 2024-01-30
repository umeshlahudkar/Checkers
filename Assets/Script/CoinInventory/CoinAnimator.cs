using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinAnimator : MonoBehaviour
{
    [SerializeField] private CoinMover[] coinMovers;
    private float radius = 75f;   //150 //40
    private int reachedToTargetCount = 0;

    public IEnumerator PlayAnimation(Transform target, float spreadRadius = 75f)
    {
        radius = spreadRadius;
        SpreadCoin();

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].SetTarget(target.position, true, this);
        }

        yield return StartCoroutine(HasAllReachedToTarget());
    }

    private void SpreadCoin()
    {
        for(int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].gameObject.SetActive(true);

            Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(radius/2, radius);
            Vector2 targetPosition = (Vector2)transform.position + randomDirection;

            coinMovers[i].SetTarget(targetPosition, false, this);
        }
    }

    private IEnumerator HasAllReachedToTarget()
    {
        while(reachedToTargetCount >= coinMovers.Length)
        {
            yield return null;
        }
    }

    public void IncrementReachedToTarget()
    {
        reachedToTargetCount++;
    }
}
