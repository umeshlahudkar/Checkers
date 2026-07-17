using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchmakingPage : Page
{
    [Header("Own Player")]
    [SerializeField] private Image ownPlayerAvtarImg;
    [SerializeField] private TextMeshProUGUI ownPlayerNameText;

    [Header("Opponent")]
    [SerializeField] private Image opponentAvtarImg;
    [SerializeField] private TextMeshProUGUI opponentNameText;

    [Header("Status")]
    [SerializeField] private GameObject searchingSpinner;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Stake")]
    [SerializeField] private TextMeshProUGUI stakeText;

    [Header("Back Button")]
    [SerializeField] private CustomButton backButton;

    private const int StakeAmount = 250;

    protected override void OnOpened()
    {
        ownPlayerNameText.text = ServiceLocator.Get<ProfileManager>().GetUserName();
        ownPlayerAvtarImg.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();

        stakeText.text = StakeAmount + " coin stake";

        ShowSearching();

        ServiceLocator.Get<AudioManager>().PlayMatchmakingScrollSound();
    }

    protected override void OnClosed()
    {
        ServiceLocator.Get<AudioManager>().StopMatchmakingScrollSound();
    }

    private void ShowSearching()
    {
        backButton.enabled = true;

        opponentAvtarImg.gameObject.SetActive(false);
        opponentNameText.text = "Opponent";

        searchingSpinner.SetActive(true);
        statusText.text = "Searching for opponent...";
    }

    public void UpdateRemainingTime(int secondsLeft)
    {
        statusText.text = $"Searching for opponent... {secondsLeft}s";
    }

    public void ShowOpponentFound(string opponentName, Sprite opponentAvtar)
    {
        backButton.enabled = false;

        opponentAvtarImg.gameObject.SetActive(true);
        opponentAvtarImg.sprite = opponentAvtar;
        opponentNameText.text = opponentName;

        searchingSpinner.SetActive(false);
        statusText.text = "Opponent found — starting...";

        ServiceLocator.Get<AudioManager>().StopMatchmakingScrollSound();
    }

    public void ShowFailed(string message)
    {
        backButton.enabled = true;

        searchingSpinner.SetActive(false);
        statusText.text = message;

        ServiceLocator.Get<AudioManager>().StopMatchmakingScrollSound();
    }

    public void OnBackButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<PhotonNetworkManager>().CancelMatch();
    }
}
