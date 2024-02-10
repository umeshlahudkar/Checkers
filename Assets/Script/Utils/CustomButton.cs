using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CustomButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    private RectTransform thisTransform;
    private bool pointerOverButton = false;
    public UnityEvent unityEvent;

    private void Awake()
    {
        thisTransform = gameObject.GetComponent<RectTransform>();
    }

    public void OnClick()
    {
        thisTransform.DOScale(Vector3.one * 0.8f, 0.2f).OnComplete(() =>
        {
            unityEvent.Invoke();
            thisTransform.DOScale(Vector3.one, 0.2f);

        });
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        thisTransform.DOScale(Vector3.one * 0.8f, 0.1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(pointerOverButton)
        {
            thisTransform.DOScale(Vector3.one, 0.1f);
            unityEvent.Invoke();
        }
        else
        {
            thisTransform.DOScale(Vector3.one, 0.1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOverButton = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOverButton = true;
    }
}
