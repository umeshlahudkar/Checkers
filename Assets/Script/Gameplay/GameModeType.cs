
public enum GameModeType 
{
   None = 0,
   Multiplayer,
   VsPlayer,
   VsBot,

   // Online match against an invited friend rather than a matchmade stranger - the host creates a
   // room and shares its code, the guest joins by typing that code. Deliberately not registered
   // with MatchmakingConnectionManager: the tile's Create/Join buttons drive this mode, not
   // StartMatch(), so it never reaches a handler lookup.
   PrivateRoom
}
