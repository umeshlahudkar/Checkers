using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MatchmakingScreenAnimation : MonoBehaviour
{
    [Header("Screen open Animation")]
    [SerializeField] private Transform vsImg;
    [SerializeField] private Transform player1_Info;
    [SerializeField] private Transform player2_Info;
    [SerializeField] private Transform[] elements;

    private Vector3 player1_InitialPosition;
    private Vector3 player2_InitialPosition;
    private WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);


    private void OnEnable()
    {
        SetUpScreenForAnimation();
        StartCoroutine(ScreenOpenAnimation());
    }

    private void SetUpScreenForAnimation()
    {
        player1_InitialPosition = player1_Info.position;
        player2_InitialPosition = player2_Info.position;

        float screenWidth = Screen.width;

        player1_Info.position = new Vector3(player1_InitialPosition.x - (screenWidth / 2), player1_InitialPosition.y, player1_InitialPosition.z);
        player2_Info.position = new Vector3(player2_InitialPosition.x + (screenWidth / 2), player2_InitialPosition.y, player2_InitialPosition.z);

        vsImg.localScale = 4f * Vector3.one;

        for (int i = 0; i < elements.Length; i++)
        {
            elements[i].localScale = Vector3.zero;
        }
    }

    private IEnumerator ScreenOpenAnimation()
    {
        yield return waitForSeconds;

        vsImg.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutBounce);
        player1_Info.DOMove(player1_InitialPosition, 0.25f).SetEase(Ease.Flash);
        player2_Info.DOMove(player2_InitialPosition, 0.25f).SetEase(Ease.Flash);

        yield return waitForSeconds;

        for (int i = 0; i < elements.Length; i++)
        {
            elements[i].DOScale(Vector3.one, 0.2f);
        }

        gameObject.GetComponent<MatchMakingController>()?.StartScrolling();
    }
}
