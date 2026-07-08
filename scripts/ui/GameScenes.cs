namespace godottemplate.UI;

/// <summary>
/// Central registry of top-level scene paths used for screen navigation
/// (<c>GetTree().ChangeSceneToFile(...)</c>). Keeping the paths here rather than
/// scattering <c>res://</c> string literals across controllers makes the screen
/// flow easy to follow and refactor.
///
/// As the flow grows (gameplay session, post-game, settings), add the new screen
/// here and route to it through these constants.
/// </summary>
public static class GameScenes
{
    public const string MainMenu = "res://scenes/main.tscn";
    // public const string Game = "res://scenes/game/game.tscn";
    // public const string Settings = "res://scenes/ui/settings.tscn";
}
