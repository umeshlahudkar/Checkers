using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public static class UIUtils
{
    public static void Activate(this GameObject obj, float time = 0.20f)
    {
        obj.transform.localScale = Vector3.zero;
        obj.SetActive(true);
        obj.transform.DOScale(Vector3.one, time);
    }

    public static void Deactivate(this GameObject obj, float time = 0.10f)
    {
        obj.transform.DOScale(Vector3.zero, time).OnComplete( () => obj.SetActive(false));
    }
}
