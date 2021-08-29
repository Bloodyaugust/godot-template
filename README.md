# godot-template

A template for Godot projects.

## Modifying for a new game

Change all instances of `godot-template` to `your-game-name` in:
- `export_presets.cfg`
- `tools/build-and-deploy.sh`

## Folder structure

### Actors

Packed scenes for gameplay objects that need to be manually placed or (usually) instantiated in scripts.

### Addons

Godot editor addon scripts.

### Autoloads

Like controllers, but autoloaded and marked as singletons in the project menu of the editor.

### Build

This is where builds of your game go. In CI, we create subfolders for win, osx, linux, and html5.

### Constants

Scripts that containt `const` variables in constant format. Usually autoload-ed.

### Data

Contains JSON, YAML, and other file-as-database files. Ships with a CastleDB JSON flatfile.

### Doodads

Packed scenes and scripts for game objects that implement no behavior or very minimal, non-player-interactive behavior.

### Lib

Like addons, but generally not for the editor. Generic scripts or scenes for things like data structures and cameras.

### Resources

Non packed scene Godot editor files. Themes, fonts, materials, and the like go here. This template provides a simple UI theme from Kenney assets.

### Scenes

Packed scenes that represent complete collections for some stage of gameplay or testing (like Scenes in Unity).

### Scripts

Contains subfolders for most kinds of gdscripts.

#### Behaviors

These scripts implement a shared interface, such as for tracking health and taking damage. Usually paired with a packed scene in the root `Behaviors` folder.

#### Classes

These scripts implement the basis for large collections of functionality and state. Usually paired with a packed scene in the root `Actors` folder.

#### Controllers

These scripts are for those one-off bits of functionality like tracking state for game win/over, providing services like tracking all enemies, etc.

### Shaders

Yupp.

### Sprites

Images for all your in-game and UI needs.

### Tools

Scripts for creating builds, doing other CI things, or mutating/generating assets.

### Views

Scripts that control UI, usually heavily tied into the `Store`. Also generally placed on the root of a tree of `Control` nodes. If you need to instantiate your UI, the packed scene also goes here.

#### Components

Scripts and packed scenes for instantiated pieces of your UI, like dynamic, selectable items such as buildings, units, etc.

## Dockerfile

To build:
`docker build .circleci/images/godot/ -t greysonr/godot_butler:<version number>`

To push:
`docker push greysonr/godot_butler:<tag name>`
