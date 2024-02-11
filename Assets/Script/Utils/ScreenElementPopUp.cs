using System.Collections;
using UnityEngine;

public class ScreenElementPopUp : MonoBehaviour
{
    [SerializeField] private float startDelay;
    [SerializeField] private float inBetweenDelay;
    [SerializeField] private GameObject[] elements;

    private WaitForSeconds startWait;
    private WaitForSeconds inBetweenWait;

    private void Awake()
    {
        startWait = new WaitForSeconds(startDelay);
        inBetweenWait = new WaitForSeconds(inBetweenDelay);
    }

    private void OnEnable()
    {
        SetScaleToZero();
        StartCoroutine(StartAnimation());
    }

    private IEnumerator StartAnimation()
    {
        yield return startWait;

        for (int i = 0; i < elements.Length; i++)
        {
            elements[i].Activate();
            yield return inBetweenWait;
        }
    }

    private void SetScaleToZero()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            elements[i].transform.localScale = Vector3.zero;
           
        }
    }
}
