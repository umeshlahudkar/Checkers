using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefeatPage : Page
{
    [Header("Avatars")]
    [SerializeField] private Image localAvatarImage;
    [SerializeField] private Image opponentAvatarImage;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI localPiecesText;
    [SerializeField] private TextMeshProUGUI opponentPiecesText;
    [SerializeField] private TextMeshProUGUI reasonText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI capturesText;
    [SerializeField] private TextMeshProUGUI kingsCrownedText;
    [SerializeField] private TextMeshProUGUI matchTimeText;

    public void Show(GameResult result)
    {
        localAvatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        opponentAvatarImage.sprite = result.OpponentAvatar;
        opponentNameText.text = result.OpponentName;
        localPiecesText.text = result.LocalPiecesLeft.ToString();
        opponentPiecesText.text = result.OpponentPiecesLeft.ToString();
        reasonText.text = result.Reason;

        capturesText.text = result.LocalCaptures.ToString();
        kingsCrownedText.text = result.LocalKingsCrowned.ToString();
        matchTimeText.text = result.MatchDuration;
    }

    public void OnTryAgainButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        ServiceLocator.Get<GameManager>().StartRematch();
    }

    public void OnMainMenuButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        PhotonNetwork.Disconnect();
        ServiceLocator.Get<SceneLoader>().LoadScene("MainScene");
    }

    public void OnRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameSettingsManager gameSettings = ServiceLocator.Get<GameSettingsManager>();
        ServiceLocator.Get<GamePageManager>().RuleSetInfoPage.Show(ServiceLocator.Get<GameManager>().RuleSet, gameSettings.GetRuleSetIndex(), gameSettings.RuleSetCount);
        ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.RuleSetInfoPage);
    }
}
