using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPage : Page
{
    [Header("Badge")]
    [SerializeField] private Image avatarImage;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI resultLabelText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Colors")]
    [SerializeField] private Color victoryColor = new(1f, 0.42745098f, 0.3529412f);
    [SerializeField] private Color defeatColor = new(0.6627451f, 0.6235294f, 0.6901961f);

    public void ShowVictory(string opponentName, int piecesLeft)
    {
        Setup(true, "VICTORY", "You win!", $"You beat {opponentName} · {piecesLeft} pieces left");
    }

    public void ShowVictoryByForfeit()
    {
        Setup(true, "VICTORY", "You win!", "Your opponent left the match");
    }

    public void ShowDefeat(string opponentName, string reason)
    {
        Setup(false, "DEFEAT", "You lose", $"{opponentName} won · {reason}");
    }

    public void ShowDraw(string reason)
    {
        Setup(false, "DRAW", "It's a draw", reason);
    }

    private void Setup(bool isWin, string label, string title, string subtitle)
    {
        resultLabelText.text = label;
        resultLabelText.color = isWin ? victoryColor : defeatColor;
        titleText.text = title;
        subtitleText.text = subtitle;

        avatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
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
