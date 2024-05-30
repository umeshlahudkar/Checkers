using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public static class UIUtils
{
    public static void Activate(this GameObject obj, float time = 0.20f, UnityAction action = null)
    {
        obj.transform.localScale = Vector3.zero;
        obj.SetActive(true);
        obj.transform.DOScale(Vector3.one, time).OnComplete(()=>
        {
            action?.Invoke();
        });
    }

    public static void Deactivate(this GameObject obj, float time = 0.10f)
    {
        obj.transform.DOScale(Vector3.zero, time).OnComplete( () => obj.SetActive(false));
    }
}
