# resources/

Godot `.tres` resource files defining game content. These are loaded by code or assigned in scenes.

Group resources into subdirectories by type (e.g. `enemies/`, `items/`, `levels/`).

## C# Custom Resource .tres Format

Godot's runtime resource loader resolves types via ClassDB, which only knows native types — not C# classes. Every `.tres` file backed by a C# `Resource` subclass **must** include an explicit `script` reference, or the resource will fail to load at runtime with `Cannot get class 'X'`.

Required pattern for top-level resources:

```
[gd_resource type="Resource" script_class="MyClass" format=3]

[ext_resource type="Script" uid="uid://..." path="res://scripts/data/MyClass.cs" id="1_myscript"]

[resource]
script = ExtResource("1_myscript")
MyProperty = ...
```

Required pattern for inline sub-resources (e.g. an `AttackData` embedded in a unit resource):

```
[ext_resource type="Script" uid="uid://..." path="res://scripts/data/AttackData.cs" id="2_attackscript"]

[sub_resource type="Resource" id="AttackData_1"]
script = ExtResource("2_attackscript")
Cooldown = ...
```

Script UIDs are found in the corresponding `.cs.uid` sidecar files under `scripts/data/`.
