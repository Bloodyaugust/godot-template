# Claude Instructions

## Project Overview

Godot 4.5.1 mono (C#) game project.

**Packages**: `System.Reactive`, `Microsoft.NET.Test.Sdk`, `gdUnit4.api`

## Environment

Windows with Git Bash as the default shell environment.

## Commands

| Command | Description |
|---------|-------------|
| `godot-mono` | Run Godot commands (available in shell env) |
| `./addons/gdUnit4/runtest.sh -a ./tests` | Run all tests |

## Development Rules

- New unit tests should be implemented for all new unit-testable functionality
- New integration tests should only be implemented at user request
- New script files that will be attached to a node should be created by the user in the Godot interface. Same goes for files that are being moved or deleted. Pass these requests on to the user. Files that don't directly interface with Godot are fine to be created directly by the LLM.

## Folder Structure

| Folder | Purpose | Contents |
|--------|---------|----------|
| `actors/` | Gameplay objects | Packed scenes for objects that need manual placement or script instantiation |
| `addons/` | Editor extensions | Godot editor addon scripts |
| `autoloads/` | Global singletons | Auto-loaded scripts (takes precedence over all other folders: any type of autoload goes here instead of other subfolders) |
| `build/` | Build output | Game builds; CI creates subfolders: `win/`, `osx/`, `linux/`, `html5/` |
| `constants/` | Static data | Constant declarations |
| `doodads/` | Passive objects | Packed scenes/scripts for non-interactive or minimal-behavior objects |
| `lib/` | Shared code | Custom classes, types, structs |
| `resources/` | Editor assets | Non-scene Godot files: themes, fonts, materials |
| `scenes/` | Game levels | Complete packed scenes for gameplay stages or testing |
| `shaders/` | GPU programs | Shader files |
| `sprites/` | Images | In-game and UI graphics |

### scripts/

Scripts organized by architectural role:

| Subfolder | Purpose |
|-----------|---------|
| `classes/` | Core functionality and state; usually paired with `actors/` packed scenes |
| `controllers/` | Per-scene singletons for game state, services, entity tracking (not autoloads) |

### views/

UI control scripts (must extend `Control`):

| Subfolder | Purpose |
|-----------|---------|
| `components/` | Instantiated UI pieces: dynamic/selectable items like buildings, units |

## Examples

- A script that parses player input and sends commands to a child node would be `PlayerController.cs`, and go in `scripts/controllers/`
- A script that sets up a class for a `Ship` that implements a state machine and external API for use by a controller, to be instantiated many times in a scene, would be `Ship.cs` and go in `scripts/classes/`
- A script that creates a self-removing timed floating text effect would be `FloatingText.cs` and go in `doodads/`
- A script that controls the functionality and state for a main menu would be `MainMenu.cs` and go in `views/`
- A script that hydrates and handles interaction for an item in a scrollable list of Ships would be `ShipComponent.cs` and go in `views/components/`
