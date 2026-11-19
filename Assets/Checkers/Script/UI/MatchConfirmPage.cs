using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay popup shown between tapping a mode and the match actually starting: the match setup. It is
// where the ruleset is picked (a carousel over the rulesets, with the chosen one remembered between
// sessions) and, for Vs Bot, the bot's difficulty. The button at the bottom right starts the match;
// for Friends the two buttons are Join and Create room instead of Back and Play.
//
// Same Show(...)-then-OpenPageAsOverlay(...) shape as RuleSetInfoPage, and the same DOScale/DOFade
// entrance that ConfirmationPopup and WarningNotifier use, so popups across the app materialise the
// same way.
public class MatchConfirmPage : Page
{
    [SerializeField] private TextMeshProUGUI modeTitleText;

    [Header("Rules")]
    [SerializeField] private RuleSetCard ruleSetCard;
    [SerializeField] private TappableCarousel ruleSetCarousel;

    // The bot's difficulty, shown for Vs Bot only.
    [Header("Bot Difficulty")]
    [SerializeField] private GameObject difficultySection;

    [SerializeField] private TextMeshProUGUI easyLabel;
    [SerializeField] private Image easyImg;

    [SerializeField] private TextMeshProUGUI mediumLabel;
    [SerializeField] private Image mediumImg;

    [SerializeField] private TextMeshProUGUI hardLabel;
    [SerializeField] private Image hardImg;

    [SerializeField] private Color selectedLabelColor;
    [SerializeField] private Color unselectedLabelColor;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    // The left button is Back, or Join for Friends; the right one is Play, or Create room for Friends.
    [Header("Buttons")]
    [SerializeField] private TextMeshProUGUI leftButtonText;
    [SerializeField] private TextMeshProUGUI confirmButtonText;

    // The card's height with and without the difficulty section; the buttons are anchored to its
    // bottom, so this is all the card needs to fit the mode being shown.
    [Header("Card Size")]
    [SerializeField] private float heightWithDifficulty;
    [SerializeField] private float heightWithoutDifficulty;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform contentTransform;

    private const float BgFadeInDuration = 0.2f;
    private const float ContentPopInDuration = 0.3f;
    private const float BgFadeOutDuration = 0.15f;
    private const float ContentPopOutDuration = 0.2f;

    // Bg is a partly-transparent dim overlay, so its rest alpha is cached once rather than assumed
    // to be 1 - same reason as ConfirmationPopup.bgRestAlpha.
    private float bgRestAlpha;

    // Play, Back/close and Join all land in the exit animation, and the panel stays clickable while
    // it is still shrinking, so a second click must not start the match twice. Reset on every open.
    private bool isClosing;

    private Action<RuleSetSO> onConfirmed;
    private Action onJoin;
    private bool isPrivateRoom;
    private int selectedRuleSetIndex;

    private void Awake()
    {
        bgRestAlpha = bgImage.color.a;
    }

    // onConfirmed gets the ruleset that was chosen. onJoin is only used for Friends, whose left
    // button is Join.
    public void Show(ModeInfo info, Action<RuleSetSO> onConfirmed, Action onJoin)
    {
        this.onConfirmed = onConfirmed;
        this.onJoin = onJoin;

        isPrivateRoom = info.mode == GameModeType.PrivateRoom;
        bool isVsBot = info.mode == GameModeType.VsBot;

        modeTitleText.text = info.modeName;
        leftButtonText.text = isPrivateRoom ? "JOIN" : "BACK";
        confirmButtonText.text = isPrivateRoom ? "CREATE ROOM" : "PLAY";

        difficultySection.SetActive(isVsBot);
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, isVsBot ? heightWithDifficulty : heightWithoutDifficulty);

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();

        // The carousel pages through the rulesets that are switched on, so it works in places among
        // those; the ruleset index (its position in the full list) is what is saved and sent on.
        selectedRuleSetIndex = settings.GetRuleSetIndex();
        int position = Mathf.Max(0, settings.GetAvailablePosition(selectedRuleSetIndex));
        ruleSetCarousel.Setup(settings.AvailableRuleSetCount, position, OnRuleSetPositionChanged);
        ruleSetCard.Setup(position, settings.GetRuleSet(selectedRuleSetIndex), settings.AvailableRuleSetCount);

        if (isVsBot)
        {
            HighlightDifficulty(settings.GetBotDifficulty());
        }
    }

    // Called by the carousel's arrows - refreshes the block and remembers the choice.
    private void OnRuleSetPositionChanged(int position)
    {
        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();

        selectedRuleSetIndex = settings.GetIndexAtAvailablePosition(position);
        ruleSetCard.Setup(position, settings.GetRuleSet(selectedRuleSetIndex), settings.AvailableRuleSetCount);
        settings.SetRuleSetIndex(selectedRuleSetIndex);
    }

    public void OnEasyButtonClick()
    {
        SelectDifficulty(BotDifficulty.Easy);
    }

    public void OnMediumButtonClick()
    {
        SelectDifficulty(BotDifficulty.Medium);
    }

    public void OnHardButtonClick()
    {
        SelectDifficulty(BotDifficulty.Hard);
    }

    private void SelectDifficulty(BotDifficulty difficulty)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GameSettingsManager>().SetBotDifficulty(difficulty);
        HighlightDifficulty(difficulty);
    }

    private void HighlightDifficulty(BotDifficulty difficulty)
    {
        bool easySelected = difficulty == BotDifficulty.Easy;
        bool mediumSelected = difficulty == BotDifficulty.Medium;
        bool hardSelected = difficulty == BotDifficulty.Hard;

        easyLabel.color = easySelected ? selectedLabelColor : unselectedLabelColor;
        mediumLabel.color = mediumSelected ? selectedLabelColor : unselectedLabelColor;
        hardLabel.color = hardSelected ? selectedLabelColor : unselectedLabelColor;

        easyImg.sprite = easySelected ? selectedSprite : unselectedSprite;
        mediumImg.sprite = mediumSelected ? selectedSprite : unselectedSprite;
        hardImg.sprite = hardSelected ? selectedSprite : unselectedSprite;
    }

    // Play, or Create room for Friends.
    public void OnConfirmButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        RuleSetSO ruleSet = ServiceLocator.Get<GameSettingsManager>().GetRuleSet(selectedRuleSetIndex);
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.MatchConfirm);
            onConfirmed?.Invoke(ruleSet);
        });
    }

    // Back, or Join for Friends.
    public void OnLeftButtonClick()
    {
        if (!isPrivateRoom)
        {
            OnCloseButtonClick();
            return;
        }

        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.MatchConfirm);
            onJoin?.Invoke();
        });
    }

    public void OnCloseButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.MatchConfirm);
        });
    }

    public override bool OnBackPressed()
    {
        OnCloseButtonClick();
        return true;
    }

    protected override void OnOpened()
    {
        isClosing = false;
        PlayEntranceAnimation();
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

    // Page.Close disables the GameObject immediately, so the close has to wait on this tween's
    // OnComplete rather than animate from an OnClosed hook - same as ConfirmationPopup.
    private void PlayExitAnimation(Action onComplete)
    {
        DOTween.Kill(bgImage);
        bgImage.DOFade(0f, BgFadeOutDuration);

        DOTween.Kill(contentTransform);
        contentTransform.DOScale(0f, ContentPopOutDuration).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }
}
