using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinMover : MonoBehaviour
{
    [SerializeField] private Transform thisTransform;

    private Vector2 target;
    private readonly float speed = 2000f;  // 500
    private bool canMove = false;
    private bool canDisableAtTarget;

    private Vector2 dir;
    private CoinAnimator coinAnimation;

    public void SetTarget(Vector2 targetPos, bool canDisableAtTarget, CoinAnimator coinAnimation, float timeToMove)
    {
        target = targetPos;
        canMove = true;
        this.canDisableAtTarget = canDisableAtTarget;
        this.coinAnimation = coinAnimation;

        dir = (target - (Vector2)thisTransform.position).normalized;

        StartCoroutine(Move(thisTransform.position, targetPos, timeToMove));
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
            coinAnimation.IncrementReachedToTarget();
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
