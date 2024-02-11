using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class AvtarScreenOpenAnimation : MonoBehaviour
{
    [SerializeField] private AvtarSelectionButton[] avtarSelectionButtons;

    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private GameObject screenLabel;
    [SerializeField] private GameObject saveButton;
    [SerializeField] private GameObject closeButton;


    private readonly float inBetweenDelay = 0.1f;
    private readonly float startDelay = 0.2f;

    private WaitForSeconds startWait;
    private WaitForSeconds inBetweenWait;

    private void Awake()
    {
        startWait = new WaitForSeconds(startDelay);
        inBetweenWait = new WaitForSeconds(inBetweenDelay);
    }

    private void OnEnable()
    {
        StartCoroutine(ScreenOpenAnimation());
    }

    private IEnumerator ScreenOpenAnimation()
    {
        gridLayout.enabled = false;

        screenLabel.transform.localScale = Vector3.zero;
        saveButton.transform.localScale = Vector3.zero;
        closeButton.transform.localScale = Vector3.zero;
        for (int i = 1; i <= avtarSelectionButtons.Length; i++)
        {
            avtarSelectionButtons[i - 1].transform.localScale = Vector3.zero;
        }

        yield return startWait;

        screenLabel.transform.DOScale(Vector3.one, 0.2f);
        yield return inBetweenWait;
        saveButton.transform.DOScale(Vector3.one, 0.2f);
        yield return inBetweenWait;
        closeButton.transform.DOScale(Vector3.one, 0.2f);
        yield return inBetweenWait;

        int count = 0;
        for (int i = 1; i <= avtarSelectionButtons.Length; i++)
        {
            count++;
            avtarSelectionButtons[i - 1].transform.DOScale(Vector3.one, 0.2f);

            if (count == 4)
            {
                count = 0;
                yield return inBetweenWait;
            }
        }

        gridLayout.enabled = true;
    }

}
