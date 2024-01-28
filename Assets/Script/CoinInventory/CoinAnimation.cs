using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinAnimation : MonoBehaviour
{
    [SerializeField] private CoinMover[] coinMovers;

    private float radius = 100f;

    public Transform target;


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(StartAnimation());
        }
    }

    private IEnumerator StartAnimation()
    {
        SpreadCoin();

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].SetTarget(target.position, true);
        }
    }

    private void SpreadCoin()
    {
        for(int i = 0; i < coinMovers.Length; i++)
        {
            coinMovers[i].gameObject.SetActive(true);

            Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(radius/2, radius);
            Vector2 targetPosition = (Vector2)transform.position + randomDirection;

            coinMovers[i].SetTarget(targetPosition, false);
        }
    }
}
