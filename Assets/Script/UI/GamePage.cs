using UnityEngine;
using TMPro;

public class GamePage : Page
{
    [Header("Player cards")]
    [SerializeField] private PlayerCardUI player1Card;
    [SerializeField] private PlayerCardUI player2Card;

    [Header("Buttons")]
    [SerializeField] private RectTransform buttonsParent;

    [Header("Layout")]
    [SerializeField] private RectTransform boardBorder;
    [SerializeField] private float cardSpacing = 20f;

    public void PositionCardsAroundBoard()
    {
        RectTransform opponentCard = (RectTransform)player2Card.transform;
        RectTransform ownCard = (RectTransform)player1Card.transform;

        opponentCard.sizeDelta = new Vector2(boardBorder.rect.width, opponentCard.sizeDelta.y);
        ownCard.sizeDelta = new Vector2(boardBorder.rect.width, ownCard.sizeDelta.y);

        float height = (boardBorder.rect.height / 2) + cardSpacing + (opponentCard.rect.height / 2);
        opponentCard.anchoredPosition = new Vector2(0, height);
        ownCard.anchoredPosition = new Vector2(0, -height);

        height += (opponentCard.rect.height / 2) + cardSpacing + (buttonsParent.rect.height / 2);
        buttonsParent.anchoredPosition = new Vector2(0, -height);
    }

    public PlayerCardUI GetPlayerCard(int playerNumber)
    {
        return (playerNumber == 1) ? player1Card : player2Card;
    }

    public void ShowPlayerInfo(string player1_name, Sprite player1_Avtar, string player2_name, Sprite player2_Avtar)
    {
        player1Card.SetPlayerInfo(player1_name, player1_Avtar);
        player2Card.SetPlayerInfo(player2_name, player2_Avtar);
    }

    public void InitTurnIndicators(int maxMissCount)
    {
        player1Card.InitTurnIndicators(maxMissCount);
        player2Card.InitTurnIndicators(maxMissCount);
    }

    public void UpdateMissIndicators(int playerNumber, int missCount)
    {
        GetPlayerCard(playerNumber).SetMissCount(missCount);
    }

    public void SetActiveTurn(int playerNumber)
    {
        player1Card.SetTurnActive(playerNumber == 1);
        player2Card.SetTurnActive(playerNumber == 2);
    }
}
