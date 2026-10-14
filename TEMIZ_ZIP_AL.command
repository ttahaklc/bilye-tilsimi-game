#!/bin/bash
# Çift tıkla: Library/Temp gibi gereksiz klasörleri dışarıda bırakıp Downloads'a temiz proje zip'i oluşturur.
cd "$(dirname "$0")" || exit 1
N="BilyeTilsimi-Unity-Proje"
OUT="$HOME/Downloads/$N.zip"
rm -f "$OUT"
zip -qr "$OUT" . -x "Library/*" "Temp/*" "Logs/*" "obj/*" "UserSettings/*" ".vscode/*" "*.csproj" "*.slnx" ".DS_Store" "*/.DS_Store" "TEMIZ_ZIP_AL.command"
echo "Hazır: $OUT"; ls -lh "$OUT"
read -p "Kapatmak için Enter'a bas"
