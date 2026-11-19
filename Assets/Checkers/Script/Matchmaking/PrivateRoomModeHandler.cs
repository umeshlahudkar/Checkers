using System;
using Photon.Pun;
using Photon.Realtime;

// A match against a friend in a room reached by code. Two sides share this one handler: the host
// creates the room and shows its code on the matchmaking page, the guest types that code into the
// Join popup. Once both are in, everything - the pre-game countdown, the scene load, the forfeit and
// timeout handling - is the same as an online match, which is why this builds on OnlineModeHandler
// instead of repeating it.
//
// What is deliberately different from random matchmaking: there is no matchmaking timer and so no
// bot fallback (the opponent is a specific person, who either shows up or doesn't), the room is
// private, and piece colors are settled after joining instead of by lobby filter.
public class PrivateRoomModeHandler : OnlineModeHandler
{
    private const int MaxCreateAttempts = 5;

    private enum Role
    {
        Host,
        Guest
    }

    private Role role;
    private string roomCode;
    private int createAttempts;

    private Action onJoinAccepted;
    private Action<string> onJoinFailed;

    // The guest is in the Join popup, not on the matchmaking page, until the room is joined - which
    // changes what cancelling and a lost connection should do (see CancelMatch/OnDisconnected).
    private bool isOnMatchmakingPage;

    // The room can fire several more callbacks after the countdown starts (player properties change
    // all the time once gameplay is loading); only the first full-and-ready one may start it.
    private bool hasStartedCountdown;

    public PrivateRoomModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.PrivateRoom;

    // Host entry point, reached like any other mode (MainMenuPage -> StartMatch(PrivateRoom)).
    public override void StartMatch()
    {
        BeginSession(Role.Host);
        roomCode = RoomCode.Generate();
        createAttempts = 0;

        // The code is generated now but only shown once the room exists (OnJoinedRoom) - until then
        // the page just says it is connecting.
        OpenMatchmakingPage();
        matchmakingPage.ShowPrivateRoom(true);

        ConnectAndContinue();
    }

    // Guest entry point, called from the Join popup with a typed code.
    public void StartJoin(string code, Action onAccepted, Action<string> onFailed)
    {
        BeginSession(Role.Guest);
        roomCode = code;
        onJoinAccepted = onAccepted;
        onJoinFailed = onFailed;

        ConnectAndContinue();
    }

    private void BeginSession(Role newRole)
    {
        // Gameplay decides everything online-specific (no undo, draw offers, rematch, forfeit) from
        // gameMode == Multiplayer, and a private room plays exactly like that, so that is what it
        // reports once the scene loads. The handler is still looked up by PrivateRoom.
        gameDataSO.gameMode = GameModeType.Multiplayer;

        ResetMatchState();
        role = newRole;
        isOnMatchmakingPage = false;
        hasStartedCountdown = false;
        onJoinAccepted = null;
        onJoinFailed = null;
    }

    private void OpenMatchmakingPage()
    {
        isOnMatchmakingPage = true;
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Matchmaking);
    }

    private void ConnectAndContinue()
    {
        if (connectionManager.IsConnectedAndReady)
        {
            ContinueToRoom();
        }
        else
        {
            connectionManager.Connect();
        }
    }

    private void ContinueToRoom()
    {
        if (role == Role.Host)
        {
            matchmakingPage.ShowCreatingRoom();
            connectionManager.CreatePrivateRoom(roomCode);
        }
        else if (!connectionManager.JoinRoomByCode(roomCode))
        {
            FailJoin("Could not join the room. Try again.");
        }
    }

    public override void OnJoinedLobby()
    {
        // Not base: random matchmaking would go looking for an opponent here.
        if (isCancelled)
        {
            return;
        }

        ContinueToRoom();
    }

    public override void OnCreateRoomFailed()
    {
        if (isCancelled)
        {
            return;
        }

        // Most likely the generated code is already taken by another room - rare, but a fresh code
        // fixes it. Nothing has been shown yet, so the host never sees a code that isn't the room's.
        createAttempts++;
        if (createAttempts < MaxCreateAttempts)
        {
            roomCode = RoomCode.Generate();
            ContinueToRoom();
            return;
        }

        ServiceLocator.Get<WarningNotifier>().Show("Could not create a room. Please try again.");
        CancelMatch();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (isCancelled || role != Role.Guest)
        {
            return;
        }

        switch (returnCode)
        {
            case ErrorCode.GameDoesNotExist:
                FailJoin("Room not found. Check the code and try again.");
                break;
            case ErrorCode.GameFull:
                FailJoin("That room is full.");
                break;
            case ErrorCode.GameClosed:
                FailJoin("That match has already started.");
                break;
            default:
                FailJoin("Could not join the room. Try again.");
                break;
        }
    }

    private void FailJoin(string message)
    {
        // The connection is kept so the player can correct the code and retry straight away.
        onJoinFailed?.Invoke(message);
    }

    public override void OnJoinedRoom()
    {
        // Not base: it starts the random-matchmaking timer (and with it the bot fallback).
        if (role == Role.Host)
        {
            // Joined the room just created: it exists, so the code is good to hand out.
            matchmakingPage.ShowRoomCode(roomCode);
            return;
        }

        AdoptHostSettings();

        onJoinAccepted?.Invoke();
        OpenMatchmakingPage();
        matchmakingPage.ShowPrivateRoom(false);

        TryStartPrivateRoom();
    }

    // The match is played by the host's rules, not the guest's own selection, and on the opposite
    // color to the host's.
    private void AdoptHostSettings()
    {
        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        int ruleSetIndex = connectionManager.GetRoomRuleSetIndex();
        if (ruleSetIndex >= 0 && ruleSetIndex < settings.RuleSetCount)
        {
            gameDataSO.ruleSet = settings.GetRuleSet(ruleSetIndex);
        }

        connectionManager.MatchOwnPieceColorToRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Not base, which would call TryStartGameplay directly and skip the once-only guard.
        TryStartPrivateRoom();
    }

    // The guest's color correction arrives after it joins, so the host can see a full room whose
    // colors still clash for a moment - it waits (CanStartGameplay) and starts once this lands.
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        TryStartPrivateRoom();
    }

    protected override bool CanStartGameplay()
    {
        return connectionManager.OpponentHasOppositePieceType();
    }

    private void TryStartPrivateRoom()
    {
        if (hasStartedCountdown)
        {
            return;
        }

        TryStartGameplay();

        if (isPreGameCountdownRunning)
        {
            hasStartedCountdown = true;
        }
    }

    public override void CancelMatch()
    {
        if (isOnMatchmakingPage)
        {
            // Closing the room from the matchmaking page: the shared cancel (disconnect, go back).
            isOnMatchmakingPage = false;
            base.CancelMatch();
            return;
        }

        // Backing out of the Join popup: there is no page to go back from, so just drop the attempt.
        // Going through the base here would pop the main menu itself.
        isCancelled = true;
        isWaitingForSceneLoad = false;
        StopPreGameCountdown();

        if (connectionManager.IsConnected)
        {
            connectionManager.Disconnect();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (role == Role.Guest && !isOnMatchmakingPage)
        {
            if (isCancelled)
            {
                return;
            }

            // Still in the Join popup: report it there, where the player is looking, instead of with
            // a toast from a page change that isn't going to happen.
            isCancelled = true;
            onJoinFailed?.Invoke(MatchmakingConnectionManager.GetDisconnectMessage(cause));
            return;
        }

        base.OnDisconnected(cause);
    }
}
