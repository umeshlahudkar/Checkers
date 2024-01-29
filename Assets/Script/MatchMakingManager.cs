using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchMakingManager : MonoBehaviour
{
    [Header("Player 1 info")]
    [SerializeField] private TextMeshProUGUI ownPlayer_nametext;
    [SerializeField] private Image ownPlayer_profileImg;
    [SerializeField] private TextMeshProUGUI ownPlayer_feestext;

    [Header("Player 2 info")]
    [SerializeField] private TextMeshProUGUI opponentPlayer_nametext;
    [SerializeField] private Image[] opponentPlayer_ScrollImgs;
    [SerializeField] private TextMeshProUGUI opponentPlayer_feestext;

    [Header("Reward info")]
    [SerializeField] private TextMeshProUGUI rewardtext;
    [SerializeField] private Image rewardCoinImg;

    [Header("Coin Anim Prefab")]
    [SerializeField] private CoinAnimator coinAnimatorPrefab;

    private readonly float speed = 500f; //1000
    Vector3 offset;

    private Vector3[] initialPos;
    private int currentIndex = 1;
    private bool opponentFound = false;
    private bool hasOpponentSpriteSet = false;
    private bool canScroll = false;

    private string opponentName = string.Empty;
    private int opponentAvtarIndex = -1;

    private void OnEnable()
    {
        currentIndex = 1;
        opponentFound = false;
        hasOpponentSpriteSet = false;

        offset = Vector3.zero; 
        offset.y = Mathf.Abs(opponentPlayer_ScrollImgs[0].transform.position.y - opponentPlayer_ScrollImgs[1].transform.position.y);

        SetOwnPlayerProfile();
        SetScrollImages();
        ResetScrollImages();

        canScroll = true;
    }

    private void Update()
    {
        if(canScroll)
        {
            ScrollImages();
        }
    }

    public void SetPlayerFound(string name, int avtarIndex)
    {
        this.opponentName = name;
        this.opponentAvtarIndex = avtarIndex;
        opponentFound = true;
    }

    private void SetOwnPlayerProfile()
    {
        ownPlayer_nametext.text = ProfileManager.Instance.GetUserName();
        ownPlayer_profileImg.sprite = ProfileManager.Instance.GetProfileAvtar();
        ownPlayer_feestext.text = "Fee : " + 250.ToString();
    }

    private void SetOpponentPlayerProfile()
    {
        opponentPlayer_nametext.text = opponentName;
        opponentPlayer_feestext.text = "Fee : " + 250.ToString();
    }

    private void SetScrollImages()
    {
        initialPos = new Vector3[opponentPlayer_ScrollImgs.Length];

        for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
        {
            opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetAvtar(currentIndex);
            currentIndex = (currentIndex + 1) % 30;

            initialPos[i] = opponentPlayer_ScrollImgs[i].transform.position;
        }
    }

    private void ScrollImages()
    {
        if(!hasOpponentSpriteSet)
        {
            for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
            {
                opponentPlayer_ScrollImgs[i].transform.position += speed * Time.deltaTime * Vector3.down;

                if (opponentPlayer_ScrollImgs[i].transform.position.y <= (initialPos[0].y - offset.y))
                {
                    if (i == 0 && opponentFound)
                    {
                        opponentPlayer_ScrollImgs[0].transform.position = initialPos[2];
                        opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetAvtar(opponentAvtarIndex);
                        hasOpponentSpriteSet = true;
                    }
                    else
                    {
                        opponentPlayer_ScrollImgs[i].transform.position = initialPos[2];
                        currentIndex = (currentIndex % 30) + 1;
                        opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetAvtar(currentIndex);
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
            {
                opponentPlayer_ScrollImgs[i].transform.position += speed * Time.deltaTime * Vector3.down;

                if (i == 0 && (opponentPlayer_ScrollImgs[0].transform.position.y - initialPos[0].y) <= 0.1f)
                {
                    StartCoroutine(HandleOnOpponentPlayerFound());
                    break;
                }
            }
        }
    }

    private IEnumerator HandleOnOpponentPlayerFound()
    {
        
        canScroll = false;
        ResetScrollImages();

        for (int j = 1; j < opponentPlayer_ScrollImgs.Length; j++)
        {
            opponentPlayer_ScrollImgs[j].enabled = false;
        }

        SetOpponentPlayerProfile();

        yield return StartCoroutine(ShowCoinAnimation());

        //yield return new WaitForSeconds(1);

        if (Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            Photon.Pun.PhotonNetwork.LoadLevel(1);
        }
    }

    private IEnumerator ShowCoinAnimation()
    {
        CoinAnimator anim1 = Instantiate<CoinAnimator>(coinAnimatorPrefab, ownPlayer_feestext.transform.position, Quaternion.identity, transform);
        StartCoroutine(anim1.PlayAnimation(rewardCoinImg.transform));

        CoinAnimator anim2 = Instantiate<CoinAnimator>(coinAnimatorPrefab, opponentPlayer_feestext.transform.position, Quaternion.identity, transform);
        yield return StartCoroutine(anim2.PlayAnimation(rewardCoinImg.transform));
    }

    private void ResetScrollImages()
    {
        for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
        {
            opponentPlayer_ScrollImgs[i].enabled = true;
            opponentPlayer_ScrollImgs[i].transform.position = initialPos[i];
        }
    }
}
