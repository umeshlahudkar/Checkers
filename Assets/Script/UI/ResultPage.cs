using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPage : Page
{
    [Header("Badge")]
    [SerializeField] private Image ringOutline;
    [SerializeField] private Image avatarImage;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI resultLabelText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Coin reward")]
    [SerializeField] private GameObject coinRewardRow;
    [SerializeField] private TextMeshProUGUI coinRewardText;

    [Header("Colors")]
    [SerializeField] private Color victoryColor = new(1f, 0.42745098f, 0.3529412f);
    [SerializeField] private Color defeatColor = new(0.6627451f, 0.6235294f, 0.6901961f);

    public void ShowVictory(string opponentName, int piecesLeft, int coinsWon)
    {
        Setup(true, "VICTORY", "You win!", $"You beat {opponentName} · {piecesLeft} pieces left");
        SetCoinReward(coinsWon);
    }

    public void ShowVictoryByForfeit(int coinsWon)
    {
        Setup(true, "VICTORY", "You win!", "Your opponent left the match");
        SetCoinReward(coinsWon);
    }

    public void ShowDefeat(string opponentName, string reason)
    {
        Setup(false, "DEFEAT", "You lose", $"{opponentName} won · {reason}");
        coinRewardRow.SetActive(false);
    }

    private void Setup(bool isWin, string label, string title, string subtitle)
    {
        resultLabelText.text = label;
        resultLabelText.color = isWin ? victoryColor : defeatColor;
        titleText.text = title;
        subtitleText.text = subtitle;

        ringOutline.color = isWin ? victoryColor : defeatColor;
        avatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
    }

    private void SetCoinReward(int coinsWon)
    {
        coinRewardRow.SetActive(true);
        coinRewardText.text = $"+{coinsWon} coins";
    }

    public void OnRematchButtonClick()
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
}
