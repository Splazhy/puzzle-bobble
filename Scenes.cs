using PuzzleBobble.Scene;

namespace PuzzleBobble;

public class Scenes
{
    // NOTE: we might want every scene to be a singleton
    public static readonly AbstractScene MAIN = new MainScene();
    public static readonly AbstractScene MENU = new MenuScene();
}
