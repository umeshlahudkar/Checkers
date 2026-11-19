using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The ruleset block in the match setup popup: the flag, the name, the "1 / 9" counter, four chips of
// at-a-glance detail and the page dots. The arrows beside it are a TappableCarousel wired to
// MatchConfirmPage; this component only renders whatever index it is handed, plus the button that
// opens the full rules.
public class RuleSetCard : MonoBehaviour
{
    [SerializeField] private Image flagImage;
    [SerializeField] private TextMeshProUGUI nameText;

    // "1 / 9"
    [SerializeField] private TextMeshProUGUI counterText;

    [Header("Meta Chips")]
    [SerializeField] private TextMeshProUGUI boardSizeChipText;
    [SerializeField] private TextMeshProUGUI pieceCountChipText;
    [SerializeField] private TextMeshProUGUI kingsChipText;
    [SerializeField] private TextMeshProUGUI captureChipText;

    // One dot per ruleset, placed in the prefab; the ones beyond the ruleset count are hidden.
    [Header("Page Dots")]
    [SerializeField] private Image[] dots;
    [SerializeField] private Sprite activeDotSprite;
    [SerializeField] private Sprite idleDotSprite;

    private RuleSetSO currentRuleSet;
    private int ruleSetIndex;
    private int ruleSetCount;

    public void Setup(int index, RuleSetSO ruleSet, int count)
    {
        ruleSetIndex = index;
        ruleSetCount = count;
        currentRuleSet = ruleSet;

        flagImage.sprite = ruleSet.Flag;
        nameText.text = ruleSet.DisplayName;
        counterText.text = $"{index + 1} / {count}";

        boardSizeChipText.text = $"{ruleSet.Rows}×{ruleSet.Columns} BOARD";
        pieceCountChipText.text = $"{ruleSet.PiecesPerSide} PIECES";
        kingsChipText.text = ruleSet.FlyingKings ? "FLYING KINGS" : "SHORT KINGS";
        captureChipText.text = ruleSet.MustCaptureMaximum ? "LONGEST CAPTURE" : "ANY CAPTURE";

        for (int i = 0; i < dots.Length; i++)
        {
            bool used = i < count;
            dots[i].gameObject.SetActive(used);

            if (used)
            {
                dots[i].sprite = i == index ? activeDotSprite : idleDotSprite;
                dots[i].SetNativeSize();
            }
        }
    }

    public void OnViewRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        ServiceLocator.Get<MenuPageManager>().RuleSetInfoPage.Show(currentRuleSet, ruleSetIndex, ruleSetCount);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.RuleSetInfoPage);
    }
}
