using UnityEngine;
using TMPro;
using Photon.Pun;

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
        // player1Card/player2Card are always bound to the same identity (player1 = whoever is
        // master client, matching GameManager's winner/loser numbering) so turn highlighting,
        // miss indicators and GameOver stay correct regardless of who's viewing. Which one
        // physically renders in the bottom ("own") slot vs the top ("opponent") slot is a pure
        // display choice, decided here so the local viewer's own card is always at the bottom.
        bool ownIsPlayer1 = ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer
            || PhotonNetwork.IsMasterClient;

        RectTransform ownCard = (ownIsPlayer1 ? player1Card : player2Card).RectTransform;
        RectTransform opponentCard = (ownIsPlayer1 ? player2Card : player1Card).RectTransform;

        opponentCard.sizeDelta = new Vector2(boardBorder.rect.width, opponentCard.sizeDelta.y);
        ownCard.sizeDelta = new Vector2(boardBorder.rect.width, ownCard.sizeDelta.y);
        buttonsParent.sizeDelta = new Vector2(boardBorder.rect.width, buttonsParent.sizeDelta.y);

        // The VerticalLayoutGroup on our shared parent has already placed OpponentCard/Board/
        // OwnCard/Buttons as siblings, each of which may get more cell height than its content
        // needs. It anchors each child to a parent edge (not its center), so anchoredPosition
        // isn't directly comparable across them - we work in world space instead, where
        // RectTransform.position is always the true position of the pivot regardless of how
        // the parent anchors it. From the board's world position we place each card's content
        // flush against the board (and against each other), using only cardSpacing as the gap.
        float scaleY = boardBorder.lossyScale.y;
        float boardWorldY = boardBorder.position.y;

        float opponentWorldY = boardWorldY + ((boardBorder.rect.height / 2f) + cardSpacing + (opponentCard.rect.height / 2f)) * scaleY;
        SetWorldY(opponentCard, opponentWorldY);

        float ownWorldY = boardWorldY - ((boardBorder.rect.height / 2f) + cardSpacing + (ownCard.rect.height / 2f)) * scaleY;
        SetWorldY(ownCard, ownWorldY);

        float buttonsWorldY = ownWorldY - ((ownCard.rect.height / 2f) + cardSpacing + (buttonsParent.rect.height / 2f)) * scaleY;
        SetWorldY(buttonsParent, buttonsWorldY);
    }

    private static void SetWorldY(RectTransform content, float worldY)
    {
        Vector3 position = content.position;
        position.y = worldY;
        content.position = position;
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
