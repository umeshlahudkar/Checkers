using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CoinMover : MonoBehaviour
{
    [SerializeField] private Transform thisTransform;
    private bool canDisableAtTarget;
    private CoinAnimator coinAnimation;

    public void SetTarget(Vector2 targetPos, bool canDisableAtTarget, CoinAnimator coinAnimation, float timeToMove)
    {
        this.canDisableAtTarget = canDisableAtTarget;
        this.coinAnimation = coinAnimation;
        //StartCoroutine(Move(thisTransform.position, targetPos, timeToMove));

        thisTransform.DOMove(targetPos, timeToMove).OnComplete( ()=> 
        {
            if(canDisableAtTarget)
            {
                this.transform.localPosition = Vector2.zero;
                gameObject.SetActive(false);
                coinAnimation.IncrementCoinReachedToTarget();
            }
        });
    }

    private IEnumerator Move(Vector2 initialPos, Vector2 targetPos, float timeToMove)
    {
        float elapcedTime = 0;

        while(elapcedTime < timeToMove)
        {
            elapcedTime += Time.deltaTime;
            thisTransform.position = Vector2.Lerp(initialPos, targetPos, elapcedTime/ timeToMove);
            yield return null;
        }

        thisTransform.position = targetPos;
        if (canDisableAtTarget)
        {
            this.transform.localPosition = Vector2.zero;
            gameObject.SetActive(false);
            coinAnimation.IncrementCoinReachedToTarget();
        }
    }

    /*
    private void Update()
    {
        if(canMove) 
        {
            //thisTransform.position = Vector2.Lerp(thisTransform.position, target, Time.deltaTime); 
            dir = (target - (Vector2)thisTransform.position).normalized;
            thisTransform.position += speed * Time.deltaTime * (Vector3)dir;

            if (Vector2.Distance(target, thisTransform.position) < 50f)
            {
                canMove = false;
                if(canDisableAtTarget)
                {
                    this.transform.localPosition = Vector2.zero;
                    gameObject.SetActive(false);
                    coinAnimation.IncrementReachedToTarget();
                }
            }
        }
    }
    */
}
