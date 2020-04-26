#!/bin/sh

# set -e

which butler

echo "Checking application versions..."
echo "-----------------------------"
cat ~/.local/share/godot/templates/3.2.stable/version.txt
godot --version
butler -V
echo "-----------------------------"

mkdir build/
mkdir build/linux/
mkdir build/osx/
mkdir build/win/

echo "EXPORTING FOR LINUX"
echo "-----------------------------"
godot --export "Linux/X11" build/linux/godot-template.x86_64 -v
echo "EXPORTING FOR OSX"
echo "-----------------------------"
godot --export "Mac OSX" build/osx/godot-template.dmg -v
echo "EXPORTING FOR WINDOZE"
echo "-----------------------------"
godot --export "Windows Desktop" build/win/godot-template.exe -v
echo "-----------------------------"

echo "CHANGING FILETYPE AND CHMOD EXECUTABLE FOR OSX"
echo "-----------------------------"
cd build/osx/
mv godot-template.dmg godot-template-osx-alpha.zip
unzip godot-template-osx-alpha.zip
rm godot-template-osx-alpha.zip
chmod +x godot-template.app/Contents/MacOS/godot-template
zip -r godot-template-osx-alpha.zip godot-template.app
rm -rf godot-template.app
cd ../../

ls -al
ls -al build/
ls -al build/linux/
ls -al build/osx/
ls -al build/win/

echo "ZIPPING FOR WINDOZE"
echo "-----------------------------"
cd build/win/
zip -r godot-template-win-alpha.zip godot-template.exe godot-template.pck
rm -r godot-template.exe godot-template.pck
cd ../../

echo "ZIPPING FOR LINUX"
echo "-----------------------------"
cd build/linux/
zip -r godot-template-linux-alpha.zip godot-template.x86_64 godot-template.pck
rm -r godot-template.x86_64 godot-template.pck
cd ../../

echo "Logging in to Butler"
echo "-----------------------------"
butler login

echo "Pushing builds with Butler"
echo "-----------------------------"
butler push build/linux/ synsugarstudio/godot-template:linux-alpha
butler push build/osx/ synsugarstudio/godot-template:osx-alpha
butler push build/win/ synsugarstudio/godot-template:win-alpha
