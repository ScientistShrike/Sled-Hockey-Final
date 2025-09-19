// This static class holds game state that needs to persist across scene loads.
public static class GameSession
{
    public static int PlayerScore = 0;
    public static int BotScore = 0;

    // We store the time remaining so it continues from where it left off.
    public static float TimeRemaining = 120f;

    // A flag to check if this is the first time the game is starting.
    public static bool IsInitialized = false;
}