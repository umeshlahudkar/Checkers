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

    // Tracks whether PauseTimer actually froze a running countdown, as opposed to being called
    // while the timer was already stopped (enableTurnTimer off, or between turns) - isRunning alone
    // can't tell those two cases apart, and ResumeTimer must never turn a timer back on that wasn't
    // meant to be running at all.
    private bool wasPausedForCommit;

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
        wasPausedForCommit = false;
        currentTime = 0;
        hasPlayedTickingSound = false;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        if (activeCard != null)
        {
            activeCard.ResetDisplay(turnTime);
        }
    }

    // Freezes the countdown at its current remaining value rather than clearing it (unlike
    // ResetTimer) - called the instant a legal move starts committing, so the ~0.5s+ commit
    // animation (Player.HandlePieceMovementAndPieceDelete's settle waits) can never race a timeout
    // firing independently mid-animation for a turn that has, in fact, already been decided in
    // time. A no-op if the timer wasn't running to begin with.
    public void PauseTimer()
    {
        wasPausedForCommit = isRunning;
        isRunning = false;
    }

    // Resumes counting down from exactly where PauseTimer froze it - only if that call actually
    // paused a live countdown, so this can never start a timer that was never meant to run
    // (enableTurnTimer off) or that's already been fully reset for a new turn in the meantime.
    public void ResumeTimer()
    {
        if (!wasPausedForCommit) { return; }

        wasPausedForCommit = false;
        turnDeadline = PhotonNetwork.Time + currentTime;
        isRunning = true;
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
