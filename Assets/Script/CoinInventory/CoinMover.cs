using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinMover : MonoBehaviour
{
    [SerializeField] private Transform thisTransform;

    private Vector2 target;
    private float speed = 1500f;
    private bool canMove = false;
    private bool canDisableAtTarget;

    private Vector2 dir;

    public void SetTarget(Vector2 targetPos, bool canDisableAtTarget)
    {
        target = targetPos;
        canMove = true;
        this.canDisableAtTarget = canDisableAtTarget;

        dir = (target - (Vector2)thisTransform.position).normalized;
    }

    private void Update()
    {
        if(canMove) 
        {
            //thisTransform.position = Vector2.Lerp(thisTransform.position, target, Time.deltaTime * speed); 
            thisTransform.position += speed * Time.deltaTime * (Vector3)dir;

            if (Vector2.Distance(target, thisTransform.position) < 10f)
            {
                canMove = false;
                if(canDisableAtTarget)
                {
                    this.transform.localPosition = Vector2.zero;
                    gameObject.SetActive(false);
                }
            }
        }
    }

}
