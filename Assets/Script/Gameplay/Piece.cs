using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Button button;

    [SerializeField] private Image whitePieceImage;
    [SerializeField] private Image blackPieceImage;
    [SerializeField] private Image crownImage;
    [SerializeField] private PieceType pieceType;

    [SerializeField] private int rowID;
    [SerializeField] private int columID;

    [SerializeField] private bool isCrownedKing;
    [SerializeField] private int playerID;


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

        if(pieceType == PieceType.White)
        {
            whitePieceImage.gameObject.SetActive(true);
        }
        else
        {
            blackPieceImage.gameObject.SetActive(true);
        }

        thisTransform.position = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.position;
        thisTransform.sizeDelta = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.sizeDelta;
        thisTransform.localScale = Vector3.one;

        ServiceLocator.Get<GameplayController>().board[rowID, columID].SetBlockPiece(true, this);

        bool isOwnPiece = ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer
            || ServiceLocator.Get<GameManager>().GetPlayer(playerID).PhotonView.IsMine;

        if(isOwnPiece)
        {
            button.interactable = true;
        }
    }

    private static readonly Color KingTextColor = new(1f, 0.42745098f, 0.3529412f); // FF6D5A

    public void SetCrownKing()
    {
        SetKingState(true);
        ServiceLocator.Get<AudioManager>().PlayCrownKingSound();
        ServiceLocator.Get<GameManager>().ShowFloatingText("CROWNED KING!", KingTextColor);
    }

    // Silent version of SetCrownKing, with no sound/floating-text fanfare - used when restoring a
    // king (or a non-king) from an Undo snapshot, where nothing was "just promoted".
    public void SetKingState(bool isKing)
    {
        isCrownedKing = isKing;
        crownImage.gameObject.SetActive(isKing);
    }

    // Board/list/UI bookkeeping happens immediately (move generation and the pieces-left count need
    // this piece gone right away, not after an animation delay) - only the actual GameObject
    // destruction is deferred, so the capture reads visually as the piece being knocked out rather
    // than just vanishing.
    public void Destroy()
    {
        ServiceLocator.Get<GameplayController>().board[rowID, columID].SetBlockPiece(false, null);
        button.interactable = false;

        ServiceLocator.Get<AudioManager>().PlayPieceKillSound();

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

        PlayDisappearAnimation(0f, () => Destroy(gameObject));
    }

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
