using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchmakingPage : Page
{
    [Header("Own Player")]
    [SerializeField] private Image ownPlayerAvtarImg;
    [SerializeField] private TextMeshProUGUI ownPlayerNameText;

    [Header("Opponent")]
    [SerializeField] private Image opponentParentImage;
    [SerializeField] private Sprite opponentFoundFrameSprite;
    [SerializeField] private Image opponentAvtarImg;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private RectTransform opponentSearchIcon;

    [Header("Status")]
    [SerializeField] private GameObject searchingSpinner;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI countdownTimeText;

    [Header("Match Info")]
    [SerializeField] private TextMeshProUGUI rulesText;
    [SerializeField] private TextMeshProUGUI turnTimerText;

    // The page doubles as the host's private-room lobby: same lineup, status and rules card, plus a
    // room code card that appears between the status line and the rules card. Everything below is
    // switched on by ShowPrivateRoom and back off every time the page opens, so the random-match
    // flow never sees any of it.
    [Header("Private Room")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI cancelButtonText;
    [SerializeField] private GameObject roomCodeCard;
    [SerializeField] private TextMeshProUGUI[] codeCharTexts;
    [SerializeField] private TextMeshProUGUI copyButtonText;
    [SerializeField] private RectTransform infoCard;

    private const int TurnTimerSeconds = 15;
    private const float SearchIconMoveRange = 70f;
    private const float SearchIconMoveDuration = 0.6f;

    // Where the status line and rules card go while the room code card is showing - the card takes
    // the space the countdown ring normally has, so both move to sit around it.
    private const float HostStatusY = 330f;
    private const float HostInfoCardY = -140f;
    private const float CopiedLabelSeconds = 2f;

    private bool opponentFound;
    private Sprite opponentDefaultFrameSprite;
    private Coroutine searchIconRoutine;

    private string defaultHeaderText;
    private string defaultCancelText;
    private string defaultCopyText;
    private Vector2 statusRestPosition;
    private Vector2 infoCardRestPosition;
    private bool restStateCached;
    private string currentRoomCode;
    private Tween copiedLabelTween;

    protected override void OnOpened()
    {
        if (opponentDefaultFrameSprite == null)
        {
            opponentDefaultFrameSprite = opponentParentImage.sprite;
        }

        CacheRestState();
        ResetPrivateRoomMode();

        ownPlayerNameText.text = ServiceLocator.Get<ProfileManager>().GetUserName();
        ownPlayerAvtarImg.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();

        GameSettingsManager gameSettings = ServiceLocator.Get<GameSettingsManager>();
        RuleSetSO ruleSet = gameSettings.GetRuleSet(gameSettings.GetRuleSetIndex());
        rulesText.text = $"{ruleSet.DisplayName} · {ruleSet.Rows}×{ruleSet.Columns}";
        turnTimerText.text = TurnTimerSeconds + " seconds";

        ResetOpponentUI();
        ShowConnecting();
    }

    private void ResetOpponentUI()
    {
        opponentFound = false;

        opponentParentImage.sprite = opponentDefaultFrameSprite;
        opponentAvtarImg.gameObject.SetActive(false);
        opponentNameText.text = "Finding...";

        opponentSearchIcon.gameObject.SetActive(true);
        StartSearchIconAnimation();
    }

    public void ShowConnecting()
    {
        searchingSpinner.SetActive(false);
        statusText.text = "Connecting...";
    }

    public void ShowConnected()
    {
        statusText.text = "Connected";
    }

    public void ShowJoinedRoom()
    {
        statusText.text = "Joined";
        searchingSpinner.SetActive(true);
    }

    public void ShowSearchingOpponent()
    {
        statusText.text = "Searching for opponent...";
    }

    public void UpdateRemainingTime(int secondsLeft)
    {
        countdownTimeText.text = secondsLeft + "s";
    }

    // Switches the page to its private-room wording (title and cancel label). The host's room code is
    // deliberately not part of this - it only goes up once the room exists, see ShowRoomCode.
    public void ShowPrivateRoom(bool isHost)
    {
        headerText.text = "PRIVATE ROOM";
        cancelButtonText.text = isHost ? "CLOSE ROOM" : "LEAVE ROOM";
    }

    // Host side, while the connection and the room are still being set up: the page is open but there
    // is no code to give out yet, since a code for a room that failed to open would never work.
    public void ShowCreatingRoom()
    {
        statusText.text = "Creating your room...";
        searchingSpinner.SetActive(false);
    }

    // Host side, once the room has really been created and joined: the code to share goes up while
    // the room waits for its guest. The guest never calls this - it already has the code.
    public void ShowRoomCode(string code)
    {
        currentRoomCode = code;

        for (int i = 0; i < codeCharTexts.Length; i++)
        {
            codeCharTexts[i].text = i < code.Length ? code[i].ToString() : string.Empty;
        }

        roomCodeCard.SetActive(true);

        statusText.rectTransform.anchoredPosition = new Vector2(statusRestPosition.x, HostStatusY);
        infoCard.anchoredPosition = new Vector2(infoCardRestPosition.x, HostInfoCardY);

        statusText.text = "Waiting for your friend";
        opponentNameText.text = "Waiting...";
        searchingSpinner.SetActive(false);
    }

    public void OnCopyCodeButtonClick()
    {
        if (string.IsNullOrEmpty(currentRoomCode)) { return; }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GUIUtility.systemCopyBuffer = currentRoomCode;
        ServiceLocator.Get<WarningNotifier>().Show("Room code copied");

        // The button says so for a moment as well, since the toast can be missed.
        copyButtonText.text = "COPIED";
        copiedLabelTween?.Kill();
        copiedLabelTween = DOVirtual.DelayedCall(CopiedLabelSeconds, () => copyButtonText.text = defaultCopyText).SetLink(gameObject);
    }

    public void OnShareCodeButtonClick()
    {
        if (string.IsNullOrEmpty(currentRoomCode)) { return; }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ShareUtility.ShareText(ShareUtility.BuildRoomInvite(currentRoomCode), "Share room code");
    }

    // The rest values are read from the prefab the first time the page opens rather than hard-coded,
    // so moving either element in the prefab later doesn't leave this restoring a stale position.
    private void CacheRestState()
    {
        if (restStateCached) { return; }
        restStateCached = true;

        defaultHeaderText = headerText.text;
        defaultCancelText = cancelButtonText.text;
        defaultCopyText = copyButtonText.text;
        statusRestPosition = statusText.rectTransform.anchoredPosition;
        infoCardRestPosition = infoCard.anchoredPosition;
    }

    private void ResetPrivateRoomMode()
    {
        copiedLabelTween?.Kill();

        headerText.text = defaultHeaderText;
        cancelButtonText.text = defaultCancelText;
        copyButtonText.text = defaultCopyText;
        currentRoomCode = null;

        HideRoomCodeCard();
    }

    private void HideRoomCodeCard()
    {
        roomCodeCard.SetActive(false);
        statusText.rectTransform.anchoredPosition = statusRestPosition;
        infoCard.anchoredPosition = infoCardRestPosition;
    }

    public void ShowOpponentFound(string opponentName, Sprite opponentAvtar)
    {
        opponentFound = true;

        // The code has done its job once somebody is in - the page goes back to the normal lineup
        // layout for the countdown.
        HideRoomCodeCard();

        StopSearchIconAnimation();
        opponentSearchIcon.gameObject.SetActive(false);

        opponentParentImage.sprite = opponentFoundFrameSprite;
        opponentAvtarImg.gameObject.SetActive(true);
        opponentAvtarImg.sprite = opponentAvtar;
        opponentNameText.text = opponentName;

        searchingSpinner.SetActive(true);
        statusText.text = "Starting match";
    }

    public void ShowPreGameCountdown(string text)
    {
        countdownTimeText.text = text;
    }

    public void ShowFailed(string message)
    {
        StopSearchIconAnimation();

        searchingSpinner.SetActive(false);
        statusText.text = message;
    }

    public void OnBackButtonClick()
    {
        if (opponentFound)
        {
            return;
        }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MatchmakingConnectionManager>().CancelMatch();
    }

    // Back cancels the search, exactly like the page's own back button - which already refuses once
    // an opponent has been found, so the press is swallowed rather than leaving a match that is
    // about to start.
    public override bool OnBackPressed()
    {
        OnBackButtonClick();
        return true;
    }

    private void StartSearchIconAnimation()
    {
        StopSearchIconAnimation();
        searchIconRoutine = StartCoroutine(AnimateSearchIcon());
    }

    private void StopSearchIconAnimation()
    {
        if (searchIconRoutine != null)
        {
            StopCoroutine(searchIconRoutine);
            searchIconRoutine = null;
        }
    }

    private IEnumerator AnimateSearchIcon()
    {
        Vector2 current = opponentSearchIcon.anchoredPosition;

        while (true)
        {
            Vector2 target = new Vector2(Random.Range(-SearchIconMoveRange, SearchIconMoveRange), Random.Range(-SearchIconMoveRange, SearchIconMoveRange));
            float elapsed = 0f;

            while (elapsed < SearchIconMoveDuration)
            {
                elapsed += Time.deltaTime;
                opponentSearchIcon.anchoredPosition = Vector2.Lerp(current, target, elapsed / SearchIconMoveDuration);
                yield return null;
            }

            current = target;
        }
    }
}
