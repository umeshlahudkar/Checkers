using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeSelectionPage : Page
{
    [SerializeField] private ModeListSO modeList;
    [SerializeField] private ModeTile tileTemplate;
    [SerializeField] private Transform tileContainer;

    private bool tilesCreated;

    [Header("Ruleset")]
    [SerializeField] private RuleSetCard ruleSetCard;
    [SerializeField] private TappableCarousel ruleSetCarousel;
    [SerializeField] private GameDataSO gameDataSO;

    [Header("Ruleset Pagination")]
    [SerializeField] private Image dotTemplate;
    [SerializeField] private Transform dotsContainer;
    [SerializeField] private Sprite dotActiveSprite;
    [SerializeField] private Sprite dotIdleSprite;

    private static readonly Vector2 DotIdleSize = new Vector2(12, 12);
    private static readonly Vector2 DotActiveSize = new Vector2(44, 12);

    private readonly List<Image> ruleSetDots = new List<Image>();
    private bool dotsCreated;

    [Header("Layout")]
    [SerializeField] private RectTransform scrollContent;

    private int selectedRuleSetIndex;

    protected override void OnOpened()
    {
        CreateTiles();

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        CreateDots(settings.RuleSetCount);
        int startIndex = settings.GetRuleSetIndex();
        ruleSetCarousel.Setup(settings.RuleSetCount, startIndex, OnRuleSetIndexChanged);
        OnRuleSetIndexChanged(startIndex);

        RefreshLayout();
    }

    // The mode tiles are instantiated into a nested VerticalLayoutGroup/ContentSizeFitter chain
    // (section -> scroll content), which Unity doesn't always re-measure correctly the same frame
    // new children are added/activated. Force a full rebuild so the scroll view's size matches
    // what's actually on screen.
    public void RefreshLayout()
    {
        if (scrollContent == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
    }

    // One dot per ruleset, same instantiate-once-into-a-container pattern as CreateTiles - the
    // template stays inactive in the prefab and is cloned RuleSetCount times.
    private void CreateDots(int ruleSetCount)
    {
        if (dotsCreated)
        {
            return;
        }

        for (int i = 0; i < ruleSetCount; i++)
        {
            Image dot = Instantiate(dotTemplate, dotsContainer);
            dot.gameObject.SetActive(true);
            ruleSetDots.Add(dot);
        }

        dotsCreated = true;
    }

    private void RefreshDots(int index)
    {
        for (int i = 0; i < ruleSetDots.Count; i++)
        {
            bool isActive = i == index;
            ruleSetDots[i].sprite = isActive ? dotActiveSprite : dotIdleSprite;
            ruleSetDots[i].rectTransform.sizeDelta = isActive ? DotActiveSize : DotIdleSize;
        }
    }

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        foreach (ModeInfo info in modeList.modes)
        {
            if (!info.isActive)
            {
                continue;
            }

            ModeTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(info, OnModeSelected);
        }

        tilesCreated = true;
    }

    // Called on open (to show the persisted choice) and whenever the carousel's arrows move -
    // refreshes the single card's content and persists the choice, same as SettingPage does for
    // its theme picker.
    private void OnRuleSetIndexChanged(int index)
    {
        selectedRuleSetIndex = index;
        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        ruleSetCard.Setup(index, settings.GetRuleSet(index), null);
        settings.SetRuleSetIndex(index);

        RefreshDots(index);
    }

    // Tapping a tile still starts the match immediately, same as before - it just also carries
    // over whatever ruleset is currently selected on this same page, and (for VsBot) whatever
    // difficulty is currently selected inline on that tile itself (see ModeTile).
    private void OnModeSelected(ModeInfo info)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        gameDataSO.ruleSet = settings.GetRuleSet(selectedRuleSetIndex);
        gameDataSO.enableTurnTimer = info.enableTurnTimer;

        if (info.mode == GameModeType.VsBot)
        {
            gameDataSO.botDifficulty = settings.GetBotDifficulty();
        }

        ServiceLocator.Get<MatchmakingConnectionManager>().StartMatch(info.mode);
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    public void OnBackButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }
}
