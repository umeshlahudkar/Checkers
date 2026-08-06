using Photon.Pun;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    private PlayerCardUI activeCard;
    private readonly float turnTime = 15f;
    private float currentTime = 0f;
    private double turnDeadline = 0;
    private bool hasPlayedTickingSound = false;
    private bool isRunning;

    public float CurrentTime { get { return currentTime; } }

    public void StartTimer()
    {
        activeCard = ServiceLocator.Get<GamePageManager>().GamePage.GetPlayerCard(ServiceLocator.Get<GameManager>().CurrentTurn);

        currentTime = turnTime;
        turnDeadline = PhotonNetwork.Time + turnTime;
        hasPlayedTickingSound = false;
        isRunning = true;

        activeCard.UpdateTimer(currentTime, turnTime);
    }

    public void ResetTimer()
    {
        isRunning = false;
        currentTime = 0;
        hasPlayedTickingSound = false;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        if (activeCard != null)
        {
            activeCard.ResetDisplay(turnTime);
        }
    }

    private void Update()
    {
        if (!isRunning || ServiceLocator.Get<GameManager>().GameState != GameState.Playing)
        {
            return;
        }

        // Counts down against a synced deadline (PhotonNetwork.Time, which also runs offline off a
        // local stopwatch) instead of accumulating Time.deltaTime, so a client resuming from being
        // backgrounded sees the real elapsed time immediately rather than a countdown that silently
        // paused while away.
        currentTime = Mathf.Max(0f, (float)(turnDeadline - PhotonNetwork.Time));

        if (currentTime <= 0)
        {
            currentTime = 0;
            ResetTimer();
            ServiceLocator.Get<GameManager>().HandleTurnMissCount();
            return;
        }

        bool isLowTime = activeCard.UpdateTimer(currentTime, turnTime);

        if (!hasPlayedTickingSound && isLowTime)
        {
            hasPlayedTickingSound = true;
            ServiceLocator.Get<AudioManager>().PlayTimeTickingSound();
        }

        if (isLowTime)
        {
            float urgency = 1f - Mathf.Clamp01(currentTime / activeCard.LowTimeThreshold);
            ServiceLocator.Get<AudioManager>().SetTimeTickingUrgency(urgency);
        }
    }
}
