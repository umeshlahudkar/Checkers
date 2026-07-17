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
        RectTransform opponentCard = player2Card.RectTransform;
        RectTransform ownCard = player1Card.RectTransform;

        opponentCard.sizeDelta = new Vector2(boardBorder.rect.width, opponentCard.sizeDelta.y);
        ownCard.sizeDelta = new Vector2(boardBorder.rect.width, ownCard.sizeDelta.y);
        buttonsParent.sizeDelta = new Vector2(boardBorder.rect.width, buttonsParent.sizeDelta.y);

        // The VerticalLayoutGroup on our shared parent has already placed OpponentCard/Board/
        // OwnCard/Buttons as siblings, each of which may get more cell height than its content
        // needs. boardBorder sits centered with zero offset inside its own cell, so that cell's
        // anchoredPosition.y is the board's vertical center in the shared parent space. From
        // there we place each card's content flush against the board (and against each other),
        // using only cardSpacing as the gap, regardless of how tall the layout group's cells are.
        RectTransform boardCell = (RectTransform)boardBorder.parent;
        float boardCenterY = boardCell.anchoredPosition.y;

        float opponentTargetY = boardCenterY + (boardBorder.rect.height / 2) + cardSpacing + (opponentCard.rect.height / 2);
        PositionRelativeToBoard(opponentCard, opponentTargetY);

        float ownTargetY = boardCenterY - (boardBorder.rect.height / 2) - cardSpacing - (ownCard.rect.height / 2);
        PositionRelativeToBoard(ownCard, ownTargetY);

        float buttonsTargetY = ownTargetY - (ownCard.rect.height / 2) - cardSpacing - (buttonsParent.rect.height / 2);
        PositionRelativeToBoard(buttonsParent, buttonsTargetY);
    }

    private static void PositionRelativeToBoard(RectTransform content, float targetSharedY)
    {
        RectTransform cell = (RectTransform)content.parent;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetSharedY - cell.anchoredPosition.y);
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

    public void InitPiecesLeft(int player1Total, int player2Total)
    {
        player1Card.InitPiecesLeft(player1Total);
        player2Card.InitPiecesLeft(player2Total);
    }

    public void UpdatePiecesLeft(int player1PiecesLeft, int player2PiecesLeft)
    {
        player1Card.SetPiecesLeft(player1PiecesLeft);
        player2Card.SetPiecesLeft(player2PiecesLeft);
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

    public void OnRetryButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
        ServiceLocator.Get<GameManager>().StartRematch();
    }

    public void OnHomeButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.QuitPage);
    }

}
