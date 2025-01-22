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

- [MonoGame.Framework.DesktopGL](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL) (Version 3.8.2.1105) A cross-platform gaming framework based on XNA for creating 2D and 3D games.
- [MonoGame.Content.Builder.Task](https://www.nuget.org/packages/MonoGame.Content.Builder.Task) (Version 3.8.2.1105) A tool for building and managing game content in MonoGame projects.
- [Myra](https://www.nuget.org/packages/Myra) (Version 1.5.9) A UI library for MonoGame, providing widgets and controls for building interfaces.
- [Nopipeline.Task](https://www.nuget.org/packages/Nopipeline.Task) (Version 2.3.0) A tool for managing pipeline-free workflows in game development.

## License

### Code
The code in this repository is licensed under the [MIT License](./LICENSE).

### Assets
The art assets in the `Content/Graphics/` folder are licensed under [Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](./Content/Graphics/LICENSE).

The audio assets in the `Content/Audio/Sfx` folder are created/distributed by [Kenney](www.kenney.nl), licensed under [Creative Commons Zero, CC0](http://creativecommons.org/publicdomain/zero/1.0/). Thanks Kenney!

## References

- [Implementation of Hex Grids](https://www.redblobgames.com/grids/hexagons/implementation.html) from Red Blob Games