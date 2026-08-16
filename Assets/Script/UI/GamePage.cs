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

    [Header("Hint/Undo/Restart (offline modes only)")]
    [SerializeField] private CustomButton hintButton;
    [SerializeField] private CustomButton undoButton;
    [SerializeField] private CustomButton restartButton;

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
    private Image hintButtonImage;
    private Image undoButtonImage;
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

    // Offline-only (see RefreshHintUndoButtons) - a real Multiplayer opponent has no equivalent way
    // to restart, so the button is hidden entirely there rather than shown but meaningless.
    // Restarting is only ever gated by this one confirmation - see GameManager.StartRematch for what
    // it actually does. A misclick here is otherwise a single tap away from wiping the current match,
    // since (unlike the old QuitPage-only Restart) this button sits on the main HUD rather than
    // behind Home's own quit confirmation.
    public void OnRestartButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        DDOLPageManager ddolPageManager = ServiceLocator.Get<DDOLPageManager>();
        ddolPageManager.ConfirmationPopup.Show(
            "Restart match?",
            "Are you sure you want to restart? Your current progress will be lost.",
            "Restart",
            "Cancel",
            () =>
            {
                ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
                ServiceLocator.Get<GameManager>().StartRematch();
            },
            () => { });
        ddolPageManager.OpenPageAsOverlay(DDOLPageType.ConfirmationPopup);
    }

    public void OnHomeButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        DDOLPageManager ddolPageManager = ServiceLocator.Get<DDOLPageManager>();
        ddolPageManager.ConfirmationPopup.Show(
            "Quit match?",
            "Are you sure you want to quit? You will lose all your progress in this game, and you cannot go back.",
            "Quit Anyway",
            "Keep Playing",
            () => ServiceLocator.Get<GameManager>().GoToMainMenu(),
            () => { });
        ddolPageManager.OpenPageAsOverlay(DDOLPageType.ConfirmationPopup);
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
        // Enforced here too, not just via the button's own interactable state (same "don't just
        // trust the button" philosophy as HumanPlayer.ShowHint's own IsChainInProgress re-check) -
        // in real Multiplayer the opponent's Player object is a HumanPlayer too, so without
        // IsLocalPlayer this would let a click during their turn request a hint (and run its search)
        // against their own Player instance instead of failing closed.
        if (gameManager.GetPlayer(gameManager.CurrentTurn) is Gameplay.HumanPlayer humanPlayer
            && (gameManager.GameMode != GameModeType.Multiplayer || humanPlayer.IsLocalPlayer))
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

    // restartButton/undoButton are hidden entirely outside offline modes (or once the match is
    // over) - Undo has no sensible meaning once a move is already synced to a real opponent.
    // hintButton, unlike those two, stays available in every mode including Multiplayer (it's a
    // local-only suggestion, nothing to sync) - only its interactable state is turn-gated. Both
    // buttons' interactable state reflects whether it's currently the local player's turn to act
    // (undoButton also requires GameManager.CanUndo()). restartButton has no per-turn interactable
    // state of its own, so its visibility is set here directly rather than through a
    // SetXButtonClickable helper. Called at every turn boundary via SetActiveTurn, including right
    // after an Undo - and also directly from GameManager.SetupLocalMatch/PrepareOnlineMode during
    // match setup, since that runs before StartFirstTurn's first SetActiveTurn call and all three
    // buttons default to active in the prefab: without that early call, a Multiplayer match would
    // flash Undo/Restart visible for the whole pieces-appear animation.
    public void RefreshHintUndoButtons()
    {
        RefreshOfferDrawButtonVisibility();

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        bool isPlaying = gameManager.GameState == GameState.Playing;
        bool offlineAndPlaying = gameManager.GameMode != GameModeType.Multiplayer && isPlaying;

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(offlineAndPlaying);
        }

        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(isPlaying);
        }

        if (undoButton != null)
        {
            undoButton.gameObject.SetActive(offlineAndPlaying);
        }

        if (!isPlaying) { return; }

        Gameplay.HumanPlayer humanPlayer = gameManager.GetPlayer(gameManager.CurrentTurn) as Gameplay.HumanPlayer;

        // In VsBot/VsPlayer, "is this a HumanPlayer" alone already correctly identifies whoever's
        // physically at this device (VsPlayer's two seats are both HumanPlayer for the same local
        // player passing the device back and forth; VsBot's only human seat is player 1). In real
        // Multiplayer, though, the opponent's Player object is ALSO a HumanPlayer on this client, so
        // that alone can't tell your turn from theirs - IsLocalPlayer (PhotonView.IsMine under the
        // hood) is the only reliable check there.
        bool isHumanTurn = humanPlayer != null
            && (gameManager.GameMode != GameModeType.Multiplayer || humanPlayer.IsLocalPlayer);

        // Neither button is meaningful mid-capture-chain (a hint could suggest an unrelated piece,
        // and both would corrupt the in-progress chain's bookkeeping the same way an abandoned chain
        // does - see HumanPlayer.ShowHint's and GameManager.CanUndo's own guards for the full story).
        // CanUndo already accounts for this itself; Hint has no equivalent method for this button to
        // defer to, so it's checked directly here instead.
        if (hintButton != null)
        {
            SetHintButtonClickable(isHumanTurn && !humanPlayer.IsChainInProgress);
        }

        if (undoButton != null && offlineAndPlaying)
        {
            SetUndoButtonClickable(isHumanTurn && gameManager.CanUndo());
        }
    }

    // Immediately silences both buttons the moment a move commits (Player.HandlePieceMovementAndPieceDelete),
    // so a click can't land mid-animation - RefreshHintUndoButtons re-enables them at the next turn boundary.
    public void SetHintUndoInteractable(bool interactable)
    {
        if (hintButton == null || undoButton == null) { return; }

        SetHintButtonClickable(interactable);
        SetUndoButtonClickable(interactable);
    }

    // CustomButton (unlike a standard UI Button) has no built-in disabled state - gating each
    // button's own Image's raycast target is what actually stops IPointerDown/IPointerUp/OnClick
    // from ever reaching it (same trick as SetOfferDrawButtonClickable below).
    private void SetHintButtonClickable(bool clickable)
    {
        if (hintButtonImage == null) { hintButtonImage = hintButton.GetComponent<Image>(); }
        hintButtonImage.raycastTarget = clickable;
    }

    private void SetUndoButtonClickable(bool clickable)
    {
        if (undoButtonImage == null) { undoButtonImage = undoButton.GetComponent<Image>(); }
        undoButtonImage.raycastTarget = clickable;
    }

    // Multiplayer is the only mode that keeps the draw-offer feature: VsBot's "opponent" always
    // accepts anyway (see GameManager.OfferDraw), and VsPlayer is local pass-and-play with no one
    // else to negotiate a draw with, so the button is hidden entirely rather than shown but inert.
    // Public so it can also be called standalone (RefreshHintUndoButtons already calls this itself
    // as its first line, covering the match-setup timing this comment used to describe).
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
