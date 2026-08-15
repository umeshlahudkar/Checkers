using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GamePage : Page
{
    [Header("Player cards")]
    [SerializeField] private PlayerCardUI player1Card;
    [SerializeField] private PlayerCardUI player2Card;

    [Header("Buttons")]
    [SerializeField] private RectTransform buttonsParent;

    [Header("Hint/Undo (offline modes only)")]
    [SerializeField] private Button hintButton;
    [SerializeField] private Button undoButton;

    [Header("Draw (Multiplayer only)")]
    [SerializeField] private CustomButton offerDrawButton;
    private const string OfferDrawButtonIdleLabel = "DRAW";

    [Header("Layout")]
    [SerializeField] private RectTransform boardBorder;
    [SerializeField] private float cardSpacing = 20f;

    [Header("Gratification Text")]
    [SerializeField] private RectTransform floatingTextParent;
    [SerializeField] private FloatingText floatingTextPrefab;

    [Header("Capture Effect")]
    [SerializeField] private RectTransform captureEffectParent;
    [SerializeField] private CaptureEffect captureEffectPrefab;

    [Header("Crown Effect")]
    [SerializeField] private RectTransform crownEffectParent;
    [SerializeField] private CrownEffect crownEffectPrefab;

    private readonly Queue<(string text, Color color)> floatingTextQueue = new();
    private bool isShowingFloatingText;

    private TextMeshProUGUI offerDrawButtonLabel;
    private Image offerDrawButtonImage;
    private Coroutine drawOfferCountdownCoroutine;

    // Always spawned centered on screen (floatingTextParent is a fixed, board-independent anchor),
    // not tied to any specific board square. Queued rather than shown immediately - a crowning and
    // a multi-capture can land on the exact same move, and firing both at once would stack two
    // callouts on top of each other at the same spot.
    public void ShowFloatingText(string text, Color color)
    {
        floatingTextQueue.Enqueue((text, color));

        if (!isShowingFloatingText)
        {
            StartCoroutine(ProcessFloatingTextQueue());
        }
    }

    private IEnumerator ProcessFloatingTextQueue()
    {
        isShowingFloatingText = true;

        while (floatingTextQueue.Count > 0)
        {
            (string text, Color color) next = floatingTextQueue.Dequeue();
            FloatingText instance = Instantiate(floatingTextPrefab, floatingTextParent);
            instance.Play(next.text, next.color);

            yield return new WaitForSeconds(FloatingText.TotalDuration);
        }

        isShowingFloatingText = false;
    }

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

    public void ShowPlayerInfo(string player1_name, Sprite player1_Avtar, Sprite player1PieceSprite, string player2_name, Sprite player2_Avtar, Sprite player2PieceSprite)
    {
        player1Card.SetPlayerInfo(player1_name, player1_Avtar, player1PieceSprite);
        player2Card.SetPlayerInfo(player2_name, player2_Avtar, player2PieceSprite);
    }

    public void InitTurnIndicators(int maxMissCount)
    {
        player1Card.InitTurnIndicators(maxMissCount);
        player2Card.InitTurnIndicators(maxMissCount);
    }

    // Turn timer is a Multiplayer-only concern (see GameManager.StartTurn) - offline matches
    // (VsBot/VsPlayer) hide the countdown entirely rather than showing one that never runs.
    public void SetTimerVisible(bool visible)
    {
        player1Card.SetTimerVisible(visible);
        player2Card.SetTimerVisible(visible);
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

    public void PlayPieceCapturedAnimation(int playerNumber)
    {
        GetPlayerCard(playerNumber).PlayPieceCapturedAnimation();
    }

    // Spawned under the dedicated captureEffectParent (not the captured piece's own parent) and
    // positioned/sized to match the captured square, so the burst lands on the actual board square
    // rather than a fixed screen spot, regardless of the prefab's own default RectTransform size.
    // Left to play out and destroy itself independently of the piece, which may finish its own
    // shrink-and-destroy first.
    public void PlayCaptureEffect(Vector2 anchoredPosition, Vector2 blockSize)
    {
        if (captureEffectPrefab == null || captureEffectParent == null)
        {
            return;
        }

        //Vector2 pos = boardBorder.TransformPoint(anchoredPosition);

        CaptureEffect effect = Instantiate(captureEffectPrefab, captureEffectParent);
        effect.ThisTransform.position = anchoredPosition;
        effect.ThisTransform.sizeDelta = blockSize;
        effect.ThisTransform.SetAsLastSibling();
        effect.Play();
    }

    // Same spawn/position pattern as PlayCaptureEffect - lands on the promoted piece's square and
    // plays out independently of it.
    public void PlayCrownEffect(Vector2 anchoredPosition, Vector2 blockSize)
    {
        if (crownEffectPrefab == null || crownEffectParent == null)
        {
            return;
        }

        CrownEffect effect = Instantiate(crownEffectPrefab, crownEffectParent);
        effect.ThisTransform.position = anchoredPosition;
        effect.ThisTransform.sizeDelta = blockSize;
        effect.ThisTransform.SetAsLastSibling();
        effect.Play();
    }

    public void UpdateMissIndicators(int playerNumber, int missCount)
    {
        GetPlayerCard(playerNumber).SetMissCount(missCount);
    }

    public void SetActiveTurn(int playerNumber)
    {
        player1Card.SetTurnActive(playerNumber == 1);
        player2Card.SetTurnActive(playerNumber == 2);
        RefreshHintUndoButtons();
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

    public void OnOfferDrawButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GameManager>().OfferDraw();
    }

    public void OnRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameSettingsManager gameSettings = ServiceLocator.Get<GameSettingsManager>();
        ServiceLocator.Get<GamePageManager>().RuleSetInfoPage.Show(ServiceLocator.Get<GameManager>().RuleSet, gameSettings.GetRuleSetIndex(), gameSettings.RuleSetCount);
        ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.RuleSetInfoPage);
    }

    public void OnHintButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager.GetPlayer(gameManager.CurrentTurn) is Gameplay.HumanPlayer humanPlayer)
        {
            humanPlayer.ShowHint();
        }
    }

    public void OnUndoButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        // UndoLastMove ends by calling GameManager.StartTurn -> SetActiveTurn, which refreshes
        // these buttons' visibility/interactable state for the restored turn - no extra call needed.
        ServiceLocator.Get<GameManager>().UndoLastMove();
    }

    // Hidden entirely outside offline modes (or once the match is over); otherwise shown, with
    // interactable reflecting whether it's currently a HumanPlayer's turn to act (undoButton also
    // requires GameManager.CanUndo()). Called at every turn boundary via SetActiveTurn, including
    // right after an Undo.
    public void RefreshHintUndoButtons()
    {
        RefreshOfferDrawButtonVisibility();

        // Buttons are wired in the Editor separately from this script (see plan) - no-op until then
        // instead of throwing, since this runs every turn.
        if (hintButton == null || undoButton == null) { return; }

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        bool offlineAndPlaying = gameManager.GameMode != GameModeType.Multiplayer && gameManager.GameState == GameState.Playing;

        hintButton.gameObject.SetActive(offlineAndPlaying);
        undoButton.gameObject.SetActive(offlineAndPlaying);

        if (!offlineAndPlaying) { return; }

        Gameplay.HumanPlayer humanPlayer = gameManager.GetPlayer(gameManager.CurrentTurn) as Gameplay.HumanPlayer;
        bool isHumanTurn = humanPlayer != null;

        // Neither button is meaningful mid-capture-chain (a hint could suggest an unrelated piece,
        // and both would corrupt the in-progress chain's bookkeeping the same way an abandoned chain
        // does - see HumanPlayer.ShowHint's and GameManager.CanUndo's own guards for the full story).
        // CanUndo already accounts for this itself; Hint has no equivalent method for this button to
        // defer to, so it's checked directly here instead.
        hintButton.interactable = isHumanTurn && !humanPlayer.IsChainInProgress;
        undoButton.interactable = isHumanTurn && gameManager.CanUndo();
    }

    // Immediately silences both buttons the moment a move commits (Player.HandlePieceMovementAndPieceDelete),
    // so a click can't land mid-animation - RefreshHintUndoButtons re-enables them at the next turn boundary.
    public void SetHintUndoInteractable(bool interactable)
    {
        if (hintButton == null || undoButton == null) { return; }

        hintButton.interactable = interactable;
        undoButton.interactable = interactable;
    }

    // Multiplayer is the only mode that keeps the draw-offer feature: VsBot's "opponent" always
    // accepts anyway (see GameManager.OfferDraw), and VsPlayer is local pass-and-play with no one
    // else to negotiate a draw with, so the button is hidden entirely rather than shown but inert.
    // Public so GameManager can apply it immediately during match setup (see SetupLocalMatch/
    // PrepareOnlineMode), rather than waiting for RefreshHintUndoButtons' first call from
    // StartFirstTurn - that runs only after the pieces-appear animation, which left the button
    // showing its prefab-default state (active) for that whole stretch in offline modes.
    public void RefreshOfferDrawButtonVisibility()
    {
        if (offerDrawButton == null) { return; }

        offerDrawButton.gameObject.SetActive(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer);
    }

    // Shows a live 15-to-0 countdown on the Offer Draw button's own label in place of "DRAW", and
    // blocks further clicks for the duration - GameManager.OfferDraw already no-ops on a repeat
    // click while an offer is pending, so spamming the button was never unsafe, but nothing
    // previously told the player their click actually landed or that one was already in flight.
    // GameManager starts this the moment its own offer is confirmed sent (ReceiveDrawOffer's
    // offerer branch). This is a flat cooldown on the button itself, not tied to the offer's own
    // outcome - it runs to completion regardless of an early accept/decline or a turn change in
    // between (see GameManager.ChangeTurn/ReceiveDrawResponse, which deliberately don't call
    // StopDrawOfferCountdown). That method still exists purely as a defensive reset at the start of
    // a fresh match (see GameManager.SetupLocalMatch/PrepareOnlineMode).
    public void StartDrawOfferCountdown(float durationSeconds)
    {
        if (offerDrawButton == null) { return; }

        if (drawOfferCountdownCoroutine != null)
        {
            StopCoroutine(drawOfferCountdownCoroutine);
        }

        drawOfferCountdownCoroutine = StartCoroutine(RunDrawOfferCountdown(durationSeconds));
    }

    // Only ever called defensively at the start of a fresh match (this MonoBehaviour, and any
    // coroutine running on it, persists across a rematch) - nothing during an active match calls
    // this, since the cooldown above is deliberately unconditional.
    public void StopDrawOfferCountdown()
    {
        if (offerDrawButton == null) { return; }

        if (drawOfferCountdownCoroutine != null)
        {
            StopCoroutine(drawOfferCountdownCoroutine);
            drawOfferCountdownCoroutine = null;
        }

        SetOfferDrawButtonClickable(true);
        GetOfferDrawButtonLabel().text = OfferDrawButtonIdleLabel;
    }

    private IEnumerator RunDrawOfferCountdown(float durationSeconds)
    {
        SetOfferDrawButtonClickable(false);

        TextMeshProUGUI label = GetOfferDrawButtonLabel();
        float remaining = durationSeconds;
        int lastDisplayedSeconds = -1;

        while (remaining > 0f)
        {
            int displaySeconds = Mathf.CeilToInt(remaining);
            if (displaySeconds != lastDisplayedSeconds)
            {
                lastDisplayedSeconds = displaySeconds;
                label.text = displaySeconds.ToString();
            }

            yield return null;
            remaining -= Time.deltaTime;
        }

        drawOfferCountdownCoroutine = null;
        SetOfferDrawButtonClickable(true);
        label.text = OfferDrawButtonIdleLabel;
    }

    private void SetOfferDrawButtonClickable(bool clickable)
    {
        // CustomButton has no built-in disabled state - gating the Image's raycast target is what
        // actually stops IPointerDown/IPointerUp/OnClick from ever reaching it (see CustomButton).
        if (offerDrawButtonImage == null)
        {
            offerDrawButtonImage = offerDrawButton.GetComponent<Image>();
        }

        offerDrawButtonImage.raycastTarget = clickable;
    }

    private TextMeshProUGUI GetOfferDrawButtonLabel()
    {
        if (offerDrawButtonLabel == null)
        {
            offerDrawButtonLabel = offerDrawButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        return offerDrawButtonLabel;
    }
}
