using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchMakingManager : MonoBehaviour
{
    [Header("Player 1 info")]
    [SerializeField] private TextMeshProUGUI ourPlayer_nametext;
    [SerializeField] private Image ourPlayer_profileImg;
    [SerializeField] private TextMeshProUGUI ourPlayer_feestext;

    [Header("Player 2 info")]
    [SerializeField] private TextMeshProUGUI opponentPlayer_nametext;
    [SerializeField] private Image[] opponentPlayer_ScrollImgs;
    [SerializeField] private TextMeshProUGUI opponentPlayer_feestext;

    [Header("Reward info")]
    [SerializeField] private TextMeshProUGUI rewardtext;

    public float speed;
    Vector3 offset;

    private Vector3[] initialPos;
    private int currentIndex = 1;
    private bool opponentFound = false;
    private bool hasOpponentSpriteSet = false;
    private bool canScroll = true;

    private void OnEnable()
    {
        currentIndex = 1;
        opponentFound = false;
        hasOpponentSpriteSet = false;
        canScroll = true;

        offset = Vector3.zero; 
        offset.y = Mathf.Abs(opponentPlayer_ScrollImgs[0].transform.position.y - opponentPlayer_ScrollImgs[1].transform.position.y);

        SetOurPlayerProfile();
        SetScrollImages();
    }

    private void Update()
    {
        if(canScroll)
        {
            ScrollImages();
        }
    }

    private void SetOurPlayerProfile()
    {
        ourPlayer_nametext.text = ProfileManager.Instance.GetUserName();
        ourPlayer_profileImg.sprite = ProfileManager.Instance.GetSelectedAvtarSprite();
        ourPlayer_feestext.text = "Fee : " + 10.ToString();
    }

    private void SetOpponentPlayerProfile()
    {
        opponentPlayer_nametext.text = ProfileManager.Instance.GetUserName();
        opponentPlayer_ScrollImgs[0].sprite = ProfileManager.Instance.GetSelectedAvtarSprite();
        opponentPlayer_feestext.text = "Fee : " + 10.ToString();
    }

    private void SetScrollImages()
    {
        initialPos = new Vector3[opponentPlayer_ScrollImgs.Length];

        for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
        {
            opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetSprite(currentIndex);
            currentIndex = (currentIndex + 1) % 30;

            initialPos[i] = opponentPlayer_ScrollImgs[i].transform.position;
        }
    }

    private void ScrollImages()
    {
        for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
        {
            opponentPlayer_ScrollImgs[i].transform.position += speed * Time.deltaTime * Vector3.down;

            if (!hasOpponentSpriteSet && opponentPlayer_ScrollImgs[i].transform.position.y <= (initialPos[0].y - offset.y))
            {
                if (i == 0 && opponentFound)
                {
                    opponentPlayer_ScrollImgs[0].transform.position = initialPos[2];
                    opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetSprite(4);
                    hasOpponentSpriteSet = true;
                }
                else
                {
                    opponentPlayer_ScrollImgs[i].transform.position = initialPos[2];
                    currentIndex = (currentIndex % 30) + 1;
                    opponentPlayer_ScrollImgs[i].sprite = ProfileManager.Instance.GetSprite(currentIndex);
                }
            }
            else if (hasOpponentSpriteSet && i == 0 && (opponentPlayer_ScrollImgs[0].transform.position.y - initialPos[0].y) <= 0.1f)
            {
                canScroll = false;
                ResetScrollImages();
            }
        }
    }

    private void ResetScrollImages()
    {
        for (int i = 0; i < opponentPlayer_ScrollImgs.Length; i++)
        {
            opponentPlayer_ScrollImgs[i].transform.position = initialPos[i];
        }
    }
}
