using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Button button;

    [SerializeField] private Image pieceIcon;
    [SerializeField] private PieceType pieceType;

    [SerializeField] private int rowID;
    [SerializeField] private int columID;

    [SerializeField] private bool isCrownedKing;
    [SerializeField] private int playerID;

    public static Sprite white_piece;
    public static Sprite black_piece;
    public static Sprite crowned_white_piece;
    public static Sprite crowned_black_piece;

    // Set by MarkCaptured, never by Destroy - true for the DeferCaptureRemoval window where a
    // captured piece is still sitting on its square (blocking it) but can no longer be captured
    // again or threaten anyone, pending the real Destroy() once the whole chain ends.
    private bool isCaptured;


    [Header("Piece AI")]
    [HideInInspector] public List<BoardPosition> movablePositions = new();
    [HideInInspector] public List<CaptureSequence> captureSequences = new();


    public void SetPiece(int _playerID, int _row, int _colum, int _pieceType)
    {
        playerID = _playerID;
        columID = _colum;
        rowID = _row;
        pieceType = (PieceType)_pieceType;
        isCrownedKing = false;

        UpdatePieceIcon();

        thisTransform.position = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.position;
        thisTransform.sizeDelta = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.sizeDelta;
        thisTransform.localScale = Vector3.one;

        ServiceLocator.Get<GameplayController>().SetSquare(rowID, columID, this);

        bool isOwnPiece = ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer
            || ServiceLocator.Get<GameManager>().GetPlayer(playerID).PhotonView.IsMine;

        if(isOwnPiece)
        {
            button.interactable = true;
        }
    }

    private static readonly Color KingTextColor = Color.white;

    public void SetCrownKing()
    {
        SetKingState(true);
        ServiceLocator.Get<AudioManager>().PlayCrownKingSound();
        HapticFeedback.TriggerKingPromotionVibration();
        ServiceLocator.Get<GameManager>().ShowFloatingText("CROWNED KING!", KingTextColor);
    }

    // Silent version of SetCrownKing, with no sound/floating-text fanfare - used when restoring a
    // king (or a non-king) from an Undo snapshot, where nothing was "just promoted".
    public void SetKingState(bool isKing)
    {
        isCrownedKing = isKing;
        UpdatePieceIcon();
    }

    private void UpdatePieceIcon()
    {
        if (pieceType == PieceType.White)
        {
            pieceIcon.sprite = isCrownedKing ? crowned_white_piece : white_piece;
        }
        else
        {
            pieceIcon.sprite = isCrownedKing ? crowned_black_piece : black_piece;
        }
    }

    // Board/list/UI bookkeeping happens immediately (move generation and the pieces-left count need
    // this piece gone right away, not after an animation delay) - only the actual GameObject
    // destruction is deferred, so the capture reads visually as the piece being knocked out rather
    // than just vanishing.
    public void Destroy()
    {
        // MarkCaptured already fired the kill sound/capture-flash and started the shrink at the
        // moment this piece was actually captured - if this is the deferred final removal for a
        // piece that went through that path, don't repeat those "just captured" cues, just finish
        // the job (board/list bookkeeping + the real disappear/destroy).
        bool alreadyMarked = isCaptured;
        isCaptured = true;

        ServiceLocator.Get<GameplayController>().SetSquare(rowID, columID, null);
        button.interactable = false;

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();

        if (playerID == 2)
        {
            gameplayController.whitePieces.Remove(this);
        }
        else
        {
            gameplayController.blackPieces.Remove(this);
        }

        ServiceLocator.Get<GamePageManager>().GamePage.UpdatePiecesLeft(gameplayController.blackPieces.Count, gameplayController.whitePieces.Count);

        if (!alreadyMarked)
        {
            ServiceLocator.Get<AudioManager>().PlayPieceKillSound();
            HapticFeedback.TriggerCaptureVibration();
            ServiceLocator.Get<GamePageManager>().GamePage.PlayPieceCapturedAnimation(playerID);
            PlayCaptureFlashAnimation();
        }

        // Stops MarkCaptured's idle pulse (if this piece went through that path) so it doesn't
        // fight the final shrink-to-nothing tween started below.
        thisTransform.DOKill();
        PlayDisappearAnimation(0f, () => Destroy(gameObject));
    }

    // DeferCaptureRemoval rulesets (International/Brazilian/Spanish/Canadian): a captured piece
    // stays on the board - still occupying its square, still blocking a flying king's path - until
    // the whole capture turn ends, so this stops short of Destroy()'s board/list bookkeeping. It
    // only flags the piece as consumed (can't be captured again, can't threaten anyone - see
    // MoveGenerator's IsCaptured checks) and shrinks it halfway so it reads as knocked out. The real
    // Destroy() runs later, once the chain finishes (see Player.FinalizeCapturedChain).
    public void MarkCaptured()
    {
        isCaptured = true;
        button.interactable = false;

        ServiceLocator.Get<AudioManager>().PlayPieceKillSound();
        HapticFeedback.TriggerCaptureVibration();
        ServiceLocator.Get<GamePageManager>().GamePage.PlayPieceCapturedAnimation(playerID);
        PlayCaptureFlashAnimation();

        // Shrinks to half size and stays there - it has to keep occupying its square as a
        // rules-required obstacle until the whole capture chain ends (see class comment above).
        // The chain's actual last hop never reaches this path any more (see
        // Player.WouldChainContinue) - it destroys itself for real immediately instead - so this
        // only ever runs for a genuine mid-chain hop, which is about to be superseded by the next
        // hop's own move/capture a moment later anyway.
        thisTransform.DOScale(0.5f, DisappearDuration).SetEase(Ease.InBack);
    }

    private void OnDestroy()
    {
        thisTransform.DOKill();
    }

    // Routed through GamePage (which owns the captureEffectPrefab reference, same as
    // floatingTextPrefab) rather than instantiating here, so the prefab only needs to be wired in
    // one place instead of on every Piece. Only fired once per piece (see the
    // !alreadyMarked/first-hit callers), same as the kill sound/haptics/card-punch it plays
    // alongside.
    //
    // [ContextMenu] lets this be triggered directly from a live Piece's inspector while in Play
    // mode, to preview the effect (through the real GamePage call chain) without a real capture.
    [ContextMenu("Play Capture Effect")]
    private void PlayCaptureFlashAnimation()
    {
        Block currentBlock = ServiceLocator.Get<GameplayController>().board[rowID, columID];
        ServiceLocator.Get<GamePageManager>().GamePage.PlayCaptureEffect(currentBlock.ThisTransform.position, currentBlock.ThisTransform.sizeDelta);
    }

    public bool IsCaptured => isCaptured;

    public const float AppearDuration = 0.3f;
    public const float DisappearDuration = 0.25f;

    // Pops in from nothing (used when a match starts) - scale starts at 0 so there's something to
    // animate from regardless of when in the piece's lifecycle this is called.
    public void PlayAppearAnimation(float delay)
    {
        thisTransform.localScale = Vector3.zero;
        thisTransform.DOScale(1f, AppearDuration).SetDelay(delay).SetEase(Ease.OutBack);
    }

    // Shrinks away (used both when a match ends, and per-piece on capture). Purely visual - callers
    // that need to read remaining piece counts (e.g. the game-over result screen) do their own
    // board/list bookkeeping before calling this, so it's safe to run independently of that.
    public void PlayDisappearAnimation(float delay, System.Action onComplete = null)
    {
        thisTransform.DOScale(0f, DisappearDuration).SetDelay(delay).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }

    // A quick wiggle - used both as an idle nudge toward pieces the player can move, and as
    // feedback when they click a piece that isn't a legal one to pick up this turn. DOKill first so
    // a repeated nudge (or a click landing mid-shake) doesn't stack a second shake on top.
    public void PlayShakeAnimation()
    {
        thisTransform.DOKill();
        thisTransform.DOShakeAnchorPos(0.4f, new Vector2(12f, 4f), vibrato: 20, randomness: 90, fadeOut: true);
        HapticFeedback.TriggerInvalidMoveVibration();
    }

    public bool IsCrownedKing
    {
        get { return isCrownedKing; }
    }

    public int Row_ID 
    { 
        get { return rowID; }
        set { rowID = value; }
    }

    public int Coloum_ID 
    { 
        get { return columID; }
        set { columID = value; }
    }

    public int Player_ID
    {
        get { return playerID; }
    }

    public PieceType PieceType { get { return pieceType; } }

    public RectTransform ThisTransform { get { return thisTransform; } }

    public void OnClick()
    {
        if(playerID == ServiceLocator.Get<GameManager>().CurrentTurn)
        {
            ServiceLocator.Get<GameManager>().GetPlayer(playerID).OnHighlightedPieceClick(this);
        }
        else
        {
            PlayShakeAnimation();
        }
    }

    public void ResetAllList()
    {
        movablePositions.Clear();
        captureSequences.Clear();
    }
}

public enum PieceType
{
    None = 0,
    White,
    Black
}
