using PuzzleBobble.Scene;

namespace PuzzleBobble;

public class Scenes
{
    // NOTE: we might want every scene to be a singleton
    public static readonly AbstractScene MENU = new MenuScene();
    public static readonly AbstractScene CREDITS = new CreditsScene();
    public static readonly AbstractScene GAME = new GameScene();
}
