using TMPro;
using UnityEngine;

// The rules strip at the top of the main menu: which variant is selected, where it sits in the
// list, and four chips of at-a-glance detail. The arrows beside it are a TappableCarousel wired to
// MainMenuPage; this component only renders whatever index it is handed, plus the info button that
// opens the full rules.
//
// counterText, descriptionText and movementChipText are optional: the strip shows a counter and a
// movement chip and has no description line, so the description is left unassigned there.
public class RuleSetCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    // "1 / 9" - replaces the row of pagination dots the old carousel used, which cost one dot per
    // ruleset and said less.
    [SerializeField] private TextMeshProUGUI counterText;

    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Meta Chips")]
    [SerializeField] private TextMeshProUGUI boardSizeChipText;
    [SerializeField] private TextMeshProUGUI pieceCountChipText;
    [SerializeField] private TextMeshProUGUI movementChipText;
    [SerializeField] private TextMeshProUGUI flyingKingsChipText;

    private RuleSetSO currentRuleSet;
    private int ruleSetIndex;
    private int ruleSetCount;

    public void Setup(int index, RuleSetSO ruleSet, int count)
    {
        ruleSetIndex = index;
        ruleSetCount = count;
        currentRuleSet = ruleSet;

        nameText.text = ruleSet.DisplayName;

        if (counterText != null)
        {
            counterText.text = $"{index + 1} / {count}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = ruleSet.Description;
        }

        boardSizeChipText.text = $"{ruleSet.Rows}\u00D7{ruleSet.Columns}";
        pieceCountChipText.text = $"{ruleSet.PiecesPerSide} PIECES";
        flyingKingsChipText.text = ruleSet.FlyingKings ? "FLYING KINGS" : "NO FLYING KINGS";

        if (movementChipText != null)
        {
            movementChipText.text = ruleSet.MovementScheme == MovementScheme.Diagonal ? "DIAGONAL" : "ORTHOGONAL";
        }
    }

    public void OnViewRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        ServiceLocator.Get<MenuPageManager>().RuleSetInfoPage.Show(currentRuleSet, ruleSetIndex, ruleSetCount);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.RuleSetInfoPage);
    }
}
