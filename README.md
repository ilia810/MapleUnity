# MapleStory Unity Client

A clean reimplementation of the MapleStory v83 client in Unity using C#, following Test-Driven Development (TDD) and strict Separation of Concerns principles.

## Architecture Overview

The project follows a three-layer architecture to ensure maintainability and testability:

### 1. Game Logic Layer (`GameLogic`)
- Core gameplay rules and state management
- Platform-agnostic (no Unity dependencies)
- Fully unit-tested
- Includes: Player, Monster, Combat, Inventory, Physics

### 2. Game Data Layer (`GameData`)
- Handles external data sources
- NX file loading (MapleStory assets)
- Network communication (future)
- Provides data through interfaces

### 3. Game View Layer (`GameView`)
- Unity-specific rendering and input
- UI components
- Visual representations
- Handles Unity lifecycle

## Project Structure

```
Assets/
├── Scripts/
│   ├── GameLogic/          # Core game logic (no Unity dependencies)
│   │   ├── Core/           # Player, Monster, GameWorld, etc.
│   │   ├── Data/           # Data structures (MapData, Platform, etc.)
│   │   ├── Interfaces/     # IInputProvider, IMapLoader, etc.
│   │   └── Math/           # Custom Vector2, collision detection
│   │
│   ├── GameData/           # Data loading layer
│   │   ├── MockMapLoader.cs
│   │   └── NxMapLoader.cs
│   │
│   ├── GameView/           # Unity-specific code
│   │   ├── GameManager.cs  # Main Unity controller
│   │   ├── Input/          # Unity input handling
│   │   ├── UI/             # UI components
│   │   └── Views/          # Visual representations
│   │
│   └── Tests/              # Test suites
│       ├── GameLogic/      # Unit tests
│       ├── GameView/       # View layer tests
│       └── PlayMode/       # Integration tests
```

## Getting Started

### Prerequisites
- Unity 2021.3 LTS or newer
- .NET Standard 2.1 support
- Unity Test Framework package

### Setup
1. Clone the repository
2. Open the project in Unity
3. Wait for Unity to import all assets
4. Open the main scene: `Assets/Scenes/Main.unity`

### Running the Game
1. Press Play in Unity Editor
2. Use the following controls:
   - **Movement**: Arrow Keys or WASD
   - **Jump**: Space or Left Alt
   - **Attack**: Ctrl or Z
   - **Crouch**: Down Arrow (while grounded)
   - **Climb**: Up/Down at ladders
   - **Use Portal**: Up Arrow at portal
   - **Inventory**: I key
   - **Skills**: K key

## Testing

### Running Tests

#### Unit Tests (Edit Mode)
1. Open Test Runner: `Window > General > Test Runner`
2. Switch to "EditMode" tab
3. Click "Run All" or run specific test suites

#### Integration Tests (Play Mode)
1. Open Test Runner: `Window > General > Test Runner`
2. Switch to "PlayMode" tab
3. Click "Run All"

### Test Coverage
- **GameLogic**: Comprehensive unit tests for all core mechanics
- **Integration**: End-to-end gameplay scenarios
- **Performance**: Frame rate and memory usage tests

### Manual Testing
See `TEST_CHECKLIST.md` for comprehensive manual testing procedures.

## Development Guidelines

### Test-Driven Development (TDD)
1. Write test first (Red)
2. Implement minimal code to pass (Green)
3. Refactor while keeping tests green

### Adding New Features
1. Define interfaces in GameLogic layer
2. Write unit tests for the feature
3. Implement logic to pass tests
4. Add visual representation in GameView
5. Write integration tests

### Code Style
- Follow C# naming conventions
- Keep logic and view strictly separated
- Use events for cross-layer communication
- Prefer composition over inheritance

## Assembly Definitions

The project uses Assembly Definition files to enforce architectural boundaries:

- `GameLogic.asmdef`: Core logic, no Unity dependencies
- `GameData.asmdef`: Data loading and external systems
- `GameView.asmdef`: Unity-specific implementations
- `*.Tests.asmdef`: Test assemblies for each layer

## Current Features

### Implemented
- ✅ Player movement and physics
- ✅ Jumping with gravity
- ✅ Platform collision
- ✅ Ladder climbing
- ✅ Crouching
- ✅ Basic combat
- ✅ Monster spawning and AI
- ✅ Inventory system
- ✅ Item drops and pickup
- ✅ Multiple maps with portals
- ✅ UI (HP/MP bars, inventory, skills)

### Planned
- 🔲 Networking (v83 server connection)
- 🔲 Full NX asset loading
- 🔲 Skills and abilities
- 🔲 NPCs and shops
- 🔲 Quests
- 🔲 Party system

## Performance Considerations

- Target: 60 FPS on moderate hardware
- Memory usage monitored via performance tests
- Asset loading optimized for smooth transitions

## Troubleshooting

### Common Issues

1. **Can't jump**: Ensure player is grounded and not crouching
2. **Empty inventory window**: Check if player is properly initialized
3. **Font errors**: System fonts are loaded dynamically

### Debug Mode
Enable debug logging in `GameManager.cs` for additional information.

## Contributing

1. Follow TDD principles for all new features
2. Ensure all tests pass before submitting
3. Maintain separation between layers
4. Document any new interfaces or systems

## License

This project is for educational purposes. MapleStory is a trademark of Nexon.

## Acknowledgments

- Original MapleStory by Nexon
- HeavenClient for reference implementation
- NoLifeNx for NX file reading