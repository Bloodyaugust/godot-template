#!/bin/sh

# set -e

which butler

echo "Checking application versions..."
echo "-----------------------------"
cat ~/.local/share/godot/templates/4.0-rc2/version.txt
godot --version
butler -V
echo "-----------------------------"

mkdir build/
mkdir build/linux/
mkdir build/osx/
mkdir build/win/

echo "EXPORTING FOR LINUX"
echo "-----------------------------"
godot --headless --export-debug "Linux/X11" build/linux/godot-template-4.x86_64 -v
# echo "EXPORTING FOR OSX"
# echo "-----------------------------"
# godot --export "Mac OSX" build/osx/godot-template-4.dmg -v
echo "EXPORTING FOR WINDOZE"
echo "-----------------------------"
godot --headless --export-debug "Windows Desktop" build/win/godot-template-4.exe -v
echo "-----------------------------"

# echo "CHANGING FILETYPE AND CHMOD EXECUTABLE FOR OSX"
# echo "-----------------------------"
# cd build/osx/
# mv godot-template-4.dmg godot-template-4-osx-alpha.zip
# unzip godot-template-4-osx-alpha.zip
# rm godot-template-4-osx-alpha.zip
# chmod +x godot-template-4.app/Contents/MacOS/godot-template-4
# zip -r godot-template-4-osx-alpha.zip godot-template-4.app
# rm -rf godot-template-4.app
# cd ../../

ls -al
ls -al build/
ls -al build/linux/
ls -al build/osx/
ls -al build/win/

echo "ZIPPING FOR WINDOZE"
echo "-----------------------------"
cd build/win/
zip -r godot-template-4-win-alpha.zip godot-template-4.exe godot-template-4.pck
rm -r godot-template-4.exe godot-template-4.pck
cd ../../

echo "ZIPPING FOR LINUX"
echo "-----------------------------"
cd build/linux/
zip -r godot-template-4-linux-alpha.zip godot-template-4.x86_64 godot-template-4.pck
rm -r godot-template-4.x86_64 godot-template-4.pck
cd ../../

echo "Logging in to Butler"
echo "-----------------------------"
butler login

echo "Pushing builds with Butler"
echo "-----------------------------"
butler push build/linux/ synsugarstudio/godot-template:linux-alpha
# butler push build/osx/ synsugarstudio/godot-template:osx-alpha
butler push build/win/ synsugarstudio/godot-template:win-alpha
