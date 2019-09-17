#!/bin/sh

# set -e

which butler

echo "Checking application versions..."
echo "-----------------------------"
cat ~/.local/share/godot/templates/3.1.1.stable/version.txt
godot --version
butler -V
echo "-----------------------------"

mkdir build/
mkdir build/linux/
mkdir build/osx/
mkdir build/win/

echo "EXPORTING FOR LINUX"
echo "-----------------------------"
godot --export "Linux/X11" build/linux/farming-td.x86_64 -v
echo "EXPORTING FOR OSX"
echo "-----------------------------"
godot --export "Mac OSX" build/osx/farming-td.dmg -v
echo "EXPORTING FOR WINDOZE"
echo "-----------------------------"
godot --export "Windows Desktop" build/win/farming-td.exe -v
echo "-----------------------------"

ls -al
ls -al build/
ls -al build/linux/
ls -al build/osx/
ls -al build/win/

echo "Logging in to Butler"
echo "-----------------------------"
butler login

echo "Pushing builds with Butler"
echo "-----------------------------"
butler push build/linux/ synsugarstudio/farming-td:linux-alpha
butler push build/osx/ synsugarstudio/farming-td:osx-alpha
butler push build/win/ synsugarstudio/farming-td:win-alpha
