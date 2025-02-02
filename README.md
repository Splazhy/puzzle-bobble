# Mimi and The Bubble Forest

A game with mechanics heavily inspired by the classic **Puzzle Bobble** game. The game is a project for the course of Computer Game Programming at King Mongkut's Institute of Technology Ladkrabang (KMITL).

## Development

### Prerequisites

- .NET 8.0 SDK

### Steps

1. Clone this repository:
   ```bash
   git clone https://github.com/Splazhy/puzzle-bobble.git
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build and run the project:
   ```bash
   dotnet run
   ```

## Dependencies

This project uses the following NuGet packages:

- [MonoGame.Framework.DesktopGL](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL) (Version 3.8.2.1105) The MonoGame runtime supporting Windows, Linux and macOS using SDL2 and OpenGL.
- [MonoGame.Content.Builder.Task](https://www.nuget.org/packages/MonoGame.Content.Builder.Task) (Version 3.8.2.1105) MSBuild task to automatically build content for MonoGame.
- [Myra](https://www.nuget.org/packages/Myra) (Version 1.5.9) UI Library for MonoGame, FNA and Stride.
- [Nopipeline.Task](https://www.nuget.org/packages/Nopipeline.Task) (Version 2.3.0) A Content Pipeline addon for Monogame which fully replaces Pipeline UI.

## License

### Code
The code in this repository is licensed under the [MIT License](./LICENSE).

`PuzzleBobble.Easer.Easer` and related classes' ported from [NutchapolSal/javagameproject](https://github.com/NutchapolSal/javagameproject/blob/main/Tetris/data/easer/Easer.java), licensed under the [MIT License](https://github.com/NutchapolSal/javagameproject/blob/main/LICENSE)

### Assets
The art assets in the `Content/Graphics/` folder are licensed under [Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](./Content/Graphics/LICENSE).

Some audio assets in the `Content/Audio/Sfx` folder are created/distributed by [Kenney](www.kenney.nl), licensed under [Creative Commons Zero, CC0](http://creativecommons.org/publicdomain/zero/1.0/) (thanks Kenney!) and some are provided by [Pixabay](https://pixabay.com/) under [Pixabay's Content License](https://pixabay.com/th/service/license-summary/).

## References

- [Implementation of Hex Grids](https://www.redblobgames.com/grids/hexagons/implementation.html) from Red Blob Games
