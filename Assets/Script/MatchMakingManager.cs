using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchMakingManager : MonoBehaviour
{
    [Header("Player 1 info")]
    [SerializeField] private TextMeshProUGUI player1_nametext;
    [SerializeField] private Image player1_profileImg;
    [SerializeField] private TextMeshProUGUI player1_feestext;

    [Header("Player 2 info")]
    [SerializeField] private TextMeshProUGUI player2_nametext;
    [SerializeField] private Image[] player2_profileImgs;
    [SerializeField] private TextMeshProUGUI player2_feestext;

    [Header("Reward info")]
    [SerializeField] private TextMeshProUGUI rewardtext;

    public float speed;
    Vector3 offset;

    private Vector3[] initialPos;
    private int currentIndex = 1;
    private bool opponentFound = false;
    private bool imageSet = false;
    private bool canScroll = true;

    private void OnEnable()
    {
        currentIndex = 1;
        opponentFound = false;
        imageSet = false;
        canScroll = true;

        player1_nametext.text = ProfileManager.Instance.GetUserName();
        player1_profileImg.sprite = ProfileManager.Instance.GetSelectedAvtarSprite();
        player1_feestext.text = "Fee : " + 10.ToString();


        offset = Vector3.zero; 
        offset.y = Mathf.Abs(player2_profileImgs[0].transform.position.y - player2_profileImgs[1].transform.position.y);

        SetScrollImages();
    }

    private void Update()
    {
        if(canScroll)
        {
            ScrollImages();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            opponentFound = true;
        }
    }

    private void SetScrollImages()
    {
        initialPos = new Vector3[player2_profileImgs.Length];

        for (int i = 0; i < player2_profileImgs.Length; i++)
        {
            player2_profileImgs[i].sprite = ProfileManager.Instance.GetSprite(currentIndex);
            currentIndex = (currentIndex + 1) % 30;

            initialPos[i] = player2_profileImgs[i].transform.position;
        }
    }

    private void ScrollImages()
    {
        for (int i = 0; i < player2_profileImgs.Length; i++)
        {
            player2_profileImgs[i].transform.position += speed * Time.deltaTime * Vector3.down;

            if (!imageSet && player2_profileImgs[i].transform.position.y <= (initialPos[0].y - offset.y))
            {
                if (i == 0 && opponentFound)
                {
                    player2_profileImgs[0].transform.position = initialPos[2];
                    player2_profileImgs[i].sprite = ProfileManager.Instance.GetSprite(4);
                    imageSet = true;
                }
                else
                {
                    player2_profileImgs[i].transform.position = initialPos[2];
                    currentIndex = (currentIndex % 30) + 1;
                    player2_profileImgs[i].sprite = ProfileManager.Instance.GetSprite(currentIndex);
                }
            }
            else if (imageSet && i == 0 && (player2_profileImgs[0].transform.position.y - initialPos[0].y) <= 0.1f)
            {
                canScroll = false;
                ResetScrollImages();
            }
        }
    }

    private void ResetScrollImages()
    {
        for (int i = 0; i < player2_profileImgs.Length; i++)
        {
            player2_profileImgs[i].transform.position = initialPos[i];
        }
    }
}
