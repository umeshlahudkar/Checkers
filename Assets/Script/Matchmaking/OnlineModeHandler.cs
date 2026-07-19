using UnityEngine;
using Photon.Realtime;

public class OnlineModeHandler : MatchModeHandler
{
    private const string GameplaySceneName = "GameplayScene";

    private const float MatchmakingTimeoutSeconds = 15f;
    private const float BotFallbackLeadTimeSeconds = 3f;

    private bool isCancelled;
    private bool isTimerRunning;
    private float matchmakingElapsed;
    private int lastDisplayedSeconds;

    public OnlineModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.Multiplayer;

    public override void StartMatch()
    {
        gameDataSO.gameMode = Mode;
        isCancelled = false;

        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Matchmaking);

        StartMatchmakingTimer();

        if(connectionManager.IsConnectedAndReady)
        {
            if(!connectionManager.JoinRandomRoom())
            {
                connectionManager.CreateRoom();
            }
        }
        else
        {
            connectionManager.Connect();
        }
    }

    public override void CancelMatch()
    {
        isCancelled = true;
        StopMatchmakingTimer();
        // Disconnect entirely (not just leave the room) - canceling means the player isn't
        // matchmaking anymore, so there's no reason to keep an idle Photon connection open.
        connectionManager.Disconnect();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }

    public override void Update()
    {
        if(!isTimerRunning)
        {
            return;
        }

        matchmakingElapsed += Time.deltaTime;
        float remaining = MatchmakingTimeoutSeconds - matchmakingElapsed;

        if(remaining <= BotFallbackLeadTimeSeconds)
        {
            StopMatchmakingTimer();
            OnMatchmakingTimeout();
            return;
        }

        int displaySeconds = Mathf.CeilToInt(remaining);
        if(displaySeconds != lastDisplayedSeconds)
        {
            lastDisplayedSeconds = displaySeconds;
            matchmakingPage.UpdateRemainingTime(displaySeconds);
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        if(isCancelled)
        {
            return;
        }

        if(!connectionManager.JoinRandomRoom())
        {
            Debug.LogError("[OnlineModeHandler] JoinRandomRoom could not be processed");
            StopMatchmakingTimer();
            matchmakingPage.ShowFailed("Could not start matchmaking. Please try again.");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        if(isCancelled)
        {
            return;
        }

        StopMatchmakingTimer();
        matchmakingPage.ShowFailed($"Connection lost ({cause}). Please try again.");
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        base.OnCustomAuthenticationFailed(debugMessage);

        if(isCancelled)
        {
            return;
        }

        StopMatchmakingTimer();
        matchmakingPage.ShowFailed("Could not authenticate. Please try again.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);

        if(isCancelled)
        {
            return;
        }

        connectionManager.CreateRoom();
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
    }

    public override void OnCreateRoomFailed()
    {
        base.OnCreateRoomFailed();

        if(isCancelled)
        {
            return;
        }

        StopMatchmakingTimer();
        matchmakingPage.ShowFailed("Could not create a match. Please try again.");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        TryStartGameplay();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        TryStartGameplay();
    }

    private void TryStartGameplay()
    {
        if(isCancelled || !connectionManager.IsRoomFull)
        {
            return;
        }

        StopMatchmakingTimer();

        gameDataSO.ownPlayer = connectionManager.GetOwnPlayerInfo();
        gameDataSO.opponentPlayer = connectionManager.GetOpponentPlayerInfo();

        matchmakingPage.ShowOpponentFound(gameDataSO.opponentPlayer.userName, gameDataSO.opponentPlayer.avatar);

        connectionManager.CloseRoomAndLoadOnlineScene(GameplaySceneName);
    }

    private void OnMatchmakingTimeout()
    {
        if(isCancelled || connectionManager.IsRoomFull)
        {
            return;
        }

        isCancelled = true;

        // Starting a VsBot match sets gameDataSO.opponentPlayer to "Computer" (with the correct
        // piece type) via PvcModeHandler; overwrite the name/avatar afterwards so the player
        // believes they matched with a real opponent, but keep the piece type it assigned.
        connectionManager.StartMatch(GameModeType.VsBot);

        PlayerInfo disguisedOpponent = CreateDisguisedOpponent(gameDataSO.opponentPlayer.pieceType);
        gameDataSO.opponentPlayer = disguisedOpponent;

        matchmakingPage.ShowOpponentFound(disguisedOpponent.userName, disguisedOpponent.avatar);
    }

    private PlayerInfo CreateDisguisedOpponent(PieceType pieceType)
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();
        int avtarIndex = Random.Range(1, profileManager.AvtarCount + 1);

        return new PlayerInfo
        {
            userName = "Random_" + Random.Range(1000, 10000),
            avatar = profileManager.GetAvtar(avtarIndex),
            pieceType = pieceType
        };
    }

    private void StartMatchmakingTimer()
    {
        matchmakingElapsed = 0f;
        lastDisplayedSeconds = -1;
        isTimerRunning = true;
    }

    private void StopMatchmakingTimer()
    {
        isTimerRunning = false;
    }
}
