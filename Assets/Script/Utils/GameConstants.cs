public static class GameConstants
{
    public static class Profile
    {
        public const string GuestNamePrefix = "Guest_";
    }

    // Photon custom-property keys shared between the player who sets them (Connect/Player.Start)
    // and the reader on the other side of the room (BuildPlayerInfo/ResolveOnlinePieceType) - both
    // sides must use the exact same string, so it's centralized here instead of retyped at each spot.
    public static class PhotonPlayerProperties
    {
        public const string AvatarId = "avtarID";
        public const string UserName = "userName";
        public const string PieceType = "pieceType";
    }

    // Photon SQL-lobby room-property keys used by MatchmakingConnectionManager to filter
    // JoinRandomRoom candidates. Not arbitrary names: Photon's SQL lobby only allows filtering on
    // its reserved "C0".."C9" property keys (int/string only), so these can't be renamed to
    // something more descriptive without breaking the filter.
    public static class RoomMatchProperties
    {
        public const string RuleSet = "C0";
        public const string PieceColor = "C1";
    }

    public static class Scenes
    {
        public const string MainScene = "MainScene";
        public const string GameplayScene = "GameplayScene";
    }
}
