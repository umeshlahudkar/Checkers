using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingBar : MonoBehaviour
{
    [SerializeField] private Transform rotatingObjTran;
    [SerializeField] private float speed;

    private void Update()
    {
        rotatingObjTran.Rotate(0, 0, -Time.deltaTime * speed);
    }
}
