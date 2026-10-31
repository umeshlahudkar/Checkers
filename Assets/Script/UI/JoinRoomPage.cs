using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay popup where the guest of a private room types the code the host shared. The code goes into
// an ordinary, visible TMP_InputField - so typing, moving the caret, selecting, deleting and the
// system paste all behave the way players already expect from any text field.
//
// Same Show(...)-then-OpenPageAsOverlay(...) shape and entrance animation as MatchConfirmPage. The
// popup stays open through the whole join attempt - errors are shown in it and the player can fix
// the code and retry - and only closes when the room is joined (OnJoinSucceeded) or backed out of.
public class JoinRoomPage : Page
{
    [Header("Code Entry")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Image fieldImage;
    [SerializeField] private CanvasGroup codeGroup;

    // One chip, two jobs: "PASTE CODE" while the field is empty, "CLEAR" once there is anything in it.
    [SerializeField] private GameObject pasteButton;
    [SerializeField] private TextMeshProUGUI pasteButtonText;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Join Button")]
    [SerializeField] private Image joinButtonImage;
    [SerializeField] private TextMeshProUGUI joinButtonText;
    [SerializeField] private Sprite joinReadySprite;
    [SerializeField] private Sprite joinIdleSprite;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform contentTransform;

    private const float BgFadeInDuration = 0.2f;
    private const float ContentPopInDuration = 0.3f;
    private const float BgFadeOutDuration = 0.15f;
    private const float ContentPopOutDuration = 0.2f;
    private const float JoiningCodeAlpha = 0.5f;

    private static readonly Color ErrorColor = new Color(0.95f, 0.42f, 0.42f, 1f);
    private static readonly Color NormalTint = Color.white;
    private static readonly Color ErrorTint = new Color(1f, 0.55f, 0.55f, 1f);

    private float bgRestAlpha;
    private bool isClosing;
    private bool isJoining;
    private bool hasAttempted;
    private bool hasError;
    private string defaultSubtitle;

    private Action<string> onSubmit;
    private Action onCancelled;

    // What has been typed, as a code: uppercase, nothing but valid characters. The field itself keeps
    // whatever case was typed (it only displays uppercase), so this is the one place that normalises.
    private string CurrentCode => RoomCode.Sanitize(codeInput.text);

    private void Awake()
    {
        bgRestAlpha = bgImage.color.a;
        defaultSubtitle = subtitleText.text;

        codeInput.characterLimit = RoomCode.Length;

        // Characters that can't be in a code are refused as they are typed, whether they come from the
        // keyboard or a paste. Case is left alone on purpose: rewriting the field's text while the
        // player types makes the phone keyboard reset its own buffer and drop characters.
        codeInput.onValidateInput = (text, index, c) => RoomCode.IsAllowed(c) ? c : '\0';
        codeInput.onValueChanged.AddListener(OnCodeChanged);
    }

    // onSubmit gets the complete code when Join is pressed. onCancelled runs when the player backs
    // out after at least one attempt - the connection that attempt opened has to be dropped.
    public void Show(Action<string> onSubmit, Action onCancelled)
    {
        this.onSubmit = onSubmit;
        this.onCancelled = onCancelled;
    }

    protected override void OnOpened()
    {
        isClosing = false;
        isJoining = false;
        hasAttempted = false;

        codeInput.SetTextWithoutNotify(string.Empty);
        SetJoining(false);
        ClearError();
        Refresh();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);

        PlayEntranceAnimation();
        StartCoroutine(FocusInputNextFrame());
    }

    // Activating the field the same frame the popup opens is swallowed (it is still being laid out
    // and scaled up from zero), so the keyboard only comes up if this waits a frame.
    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        // A room link can open this popup and join straight away; bringing the keyboard up over that
        // would be wrong.
        if (isJoining) { yield break; }

        codeInput.ActivateInputField();
    }

    // The popup was opened by a room link: fill the code in and join without waiting for a tap. Call
    // after the popup has been opened.
    public void SubmitCode(string code)
    {
        codeInput.text = code;
        OnJoinButtonClick();
    }

    // A room link arrived while the popup is already open: put its code in the field, nothing more.
    public void PrefillCode(string code)
    {
        if (isJoining) { return; }

        codeInput.text = code;
        codeInput.ActivateInputField();
    }

    private void OnCodeChanged(string raw)
    {
        // Editing the code is how a rejected one gets fixed, so the error goes the moment it changes.
        ClearError();
        Refresh();
    }

    private void Refresh()
    {
        string code = CurrentCode;

        fieldImage.color = hasError ? ErrorTint : NormalTint;
        pasteButton.SetActive(!isJoining);
        pasteButtonText.text = code.Length == 0 ? "PASTE CODE" : "CLEAR";
        joinButtonImage.sprite = RoomCode.IsComplete(code) && !isJoining ? joinReadySprite : joinIdleSprite;
    }

    public void OnPasteButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        // With something typed the chip says CLEAR; otherwise it pastes.
        if (CurrentCode.Length > 0)
        {
            codeInput.text = string.Empty;
            codeInput.ActivateInputField();
            return;
        }

        // Whatever was copied - the bare code, a link, or the whole invite message - gives the code.
        string pasted = RoomCode.Extract(GUIUtility.systemCopyBuffer);
        if (pasted.Length == 0)
        {
            ShowMessage("No room code found. Copy the code first.");
            return;
        }

        codeInput.text = pasted;
        codeInput.ActivateInputField();
        codeInput.caretPosition = pasted.Length;
    }

    public void OnJoinButtonClick()
    {
        if (isClosing || isJoining) { return; }

        string code = CurrentCode;
        if (!RoomCode.IsComplete(code))
        {
            ShowMessage($"Enter all {RoomCode.Length} characters of the code.");
            return;
        }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        hasAttempted = true;
        ClearError();
        SetJoining(true);
        codeInput.DeactivateInputField();

        onSubmit?.Invoke(code);
    }

    // The attempt failed (wrong code, full room, lost connection) - say why and let them try again.
    public void ShowError(string message)
    {
        if (!IsOpen) { return; }

        SetJoining(false);
        hasError = true;
        ShowMessage(message);
        Refresh();
        StartCoroutine(FocusAndSelectAll());
    }

    // The rejected code is still in the field. Selecting all of it when the keyboard comes back means
    // the first key typed (or a backspace) replaces it, instead of hitting the character limit. The
    // selection has to wait for the activation to land, which takes a frame.
    private IEnumerator FocusAndSelectAll()
    {
        yield return null;
        codeInput.ActivateInputField();
        yield return null;

        codeInput.selectionAnchorPosition = 0;
        codeInput.selectionFocusPosition = codeInput.text.Length;
    }

    // The room was joined: the matchmaking page opens over this one, so just close without the
    // cancel callback (there is nothing to drop - the connection is now the match's).
    public void OnJoinSucceeded()
    {
        if (isClosing) { return; }
        isClosing = true;

        codeInput.DeactivateInputField();
        PlayExitAnimation(() => ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.JoinRoom));
    }

    public void OnCancelButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        codeInput.DeactivateInputField();

        if (hasAttempted)
        {
            onCancelled?.Invoke();
        }

        PlayExitAnimation(() => ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.JoinRoom));
    }

    public void OnCloseButtonClick()
    {
        OnCancelButtonClick();
    }

    public override bool OnBackPressed()
    {
        OnCancelButtonClick();
        return true;
    }

    private void SetJoining(bool joining)
    {
        isJoining = joining;

        subtitleText.text = joining ? "Joining room..." : defaultSubtitle;
        codeGroup.alpha = joining ? JoiningCodeAlpha : 1f;
        codeInput.interactable = !joining;
        joinButtonText.text = joining ? "JOINING" : "JOIN";
        Refresh();
    }

    private void ShowMessage(string message)
    {
        messageText.color = ErrorColor;
        messageText.text = message;
    }

    private void ClearError()
    {
        if (!hasError && string.IsNullOrEmpty(messageText.text)) { return; }

        hasError = false;
        messageText.text = string.Empty;
        Refresh();
    }

    private void PlayEntranceAnimation()
    {
        DOTween.Kill(bgImage);
        Color bgColor = bgImage.color;
        bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
        bgImage.DOFade(bgRestAlpha, BgFadeInDuration);

        DOTween.Kill(contentTransform);
        contentTransform.localScale = Vector3.zero;
        contentTransform.DOScale(1f, ContentPopInDuration).SetEase(Ease.OutBack);
    }

    private void PlayExitAnimation(Action onComplete)
    {
        DOTween.Kill(bgImage);
        bgImage.DOFade(0f, BgFadeOutDuration);

        DOTween.Kill(contentTransform);
        contentTransform.DOScale(0f, ContentPopOutDuration).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }
}
