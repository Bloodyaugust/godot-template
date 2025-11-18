# godot-template C#

An opinionated template for Godot projects. Currently, this branch is up-to-date with v4.5.1-stable_mono.

Following the structure provided by this template can speed up development, especially for jams or prototypes. It's probably not enough for larger projects, but can serve as a good starting point.

## Dependencies

- Godot v4.5.1-stable_mono
- GDUnit4
- `dotnet` cli

## Getting started

- Clone project
- Change all instances of `godot-template` to `your-game-name` in the project with a find-replace
- Open project in Godot
- Build dotnet package in Godot (top-right left of play button)
- `dotnet restore` to install nuget packages
- Enable running and debugging tests in VSCode by adding `.vscode\settings.json` with `"dotnet.unitTests.runSettingsPath": ".runsettings"`

## Running tests

From within a bash shell (git bash for windows):
`./addons/gdUnit4/runtest.sh -a ./tests`

Tests can also be run from within Godot using the GdUnit panel in the top-left, or within VSCode with debugging.

## Folder structure

### Actors

Packed scenes for gameplay objects that need to be manually placed or (usually) instantiated in scripts.

### Addons

Godot editor addon scripts.

### Autoloads

Any auto-loaded script. Takes precedence over all other subfolders.

### Build

This is where builds of your game go. In CI, we create subfolders for win, osx, linux, and html5.

### Constants

Declare constant data here.

### Doodads

Packed scenes and scripts for game objects that implement no behavior or very minimal, non-player-interactive behavior.

### Lib

Custom classes, types, structs, etc.

### Resources

Non packed scene Godot editor files. Themes, fonts, materials, and the like go here.

### Scenes

Packed scenes that represent complete collections for some stage of gameplay or testing (like Scenes in Unity).

### Scripts

Contains subfolders for different types of scripts.

#### Classes

These scripts implement the basis for large collections of functionality and state. Usually paired with a packed scene in the root `Actors` folder.

#### Controllers

These scripts are for those one-off bits of functionality like tracking state for game win/over, providing services like tracking all enemies, etc. Generally a singleton per-scene, but not an autoload.

### Shaders

Yupp.

### Sprites

Images for all your in-game and UI needs.

### Views

Scripts for controlling UI, always extend `Control`.

#### Components

Scripts and packed scenes for instantiated pieces of your UI, like dynamic, selectable items such as buildings, units, etc.
