using System.Collections;
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

    private const int TurnTimerSeconds = 15;
    private const float SearchIconMoveRange = 70f;
    private const float SearchIconMoveDuration = 0.6f;

    private bool opponentFound;
    private Sprite opponentDefaultFrameSprite;
    private Coroutine searchIconRoutine;

    protected override void OnOpened()
    {
        if (opponentDefaultFrameSprite == null)
        {
            opponentDefaultFrameSprite = opponentParentImage.sprite;
        }

        ownPlayerNameText.text = ServiceLocator.Get<ProfileManager>().GetUserName();
        ownPlayerAvtarImg.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();

        GameSettingsManager gameSettings = ServiceLocator.Get<GameSettingsManager>();
        RuleSetSO ruleSet = gameSettings.GetRuleSet(gameSettings.GetRuleSetIndex());
        rulesText.text = $"{ruleSet.DisplayName} · {ruleSet.Rows}×{ruleSet.Columns}";
        turnTimerText.text = TurnTimerSeconds + " seconds";

        ShowSearching();
    }

    private void ShowSearching()
    {
        opponentFound = false;

        opponentParentImage.sprite = opponentDefaultFrameSprite;
        opponentAvtarImg.gameObject.SetActive(false);
        opponentNameText.text = "Finding...";

        opponentSearchIcon.gameObject.SetActive(true);
        StartSearchIconAnimation();

        searchingSpinner.SetActive(true);
        statusText.text = "Searching for opponent...";
    }

    public void UpdateRemainingTime(int secondsLeft)
    {
        countdownTimeText.text = secondsLeft + "s";
    }

    public void ShowOpponentFound(string opponentName, Sprite opponentAvtar)
    {
        opponentFound = true;

        StopSearchIconAnimation();
        opponentSearchIcon.gameObject.SetActive(false);

        opponentParentImage.sprite = opponentFoundFrameSprite;
        opponentAvtarImg.gameObject.SetActive(true);
        opponentAvtarImg.sprite = opponentAvtar;
        opponentNameText.text = opponentName;

        searchingSpinner.SetActive(false);
        statusText.text = "Opponent found — starting...";
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
