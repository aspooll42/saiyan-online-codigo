#!/bin/bash
# Compila la APK, la instala en el emulador/móvil conectado, la lanza y muestra el log del juego.
# Uso: ./probar_android.sh [segundos_de_espera]
set -e
cd "$(dirname "$0")/android/SaiyanAndroid"
export DOTNET_ROOT=~/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
ADB=~/Library/Android/sdk/platform-tools/adb
APP=com.saiyanonline.android
if ! ~/.dotnet/dotnet build -c Release > "$(dirname "$0")/../../logs/android-build.log" 2>&1; then
  grep -E " error " "$(dirname "$0")/../../logs/android-build.log" | sort -u | head -15
  echo "La compilación ha fallado"
  exit 1
fi
echo "Compilación correcta."
$ADB install -r bin/Release/net8.0-android/$APP-Signed.apk | tail -1
$ADB shell am force-stop $APP
$ADB shell "rm -f /data/user/0/$APP/files/logs/*" || true
$ADB logcat -c
$ADB shell monkey -p $APP -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1
perl -e "sleep ${1:-40}"
echo "pid=$($ADB shell pidof $APP || true)"
$ADB shell "cat /data/user/0/$APP/files/logs/app_process64-*.log" | grep -vE "^\s+at (Android|Java)" | tail -40
$ADB logcat -d | grep -E "MonoDroid|F DEBUG|F libc" | grep -vE "^\s*$" | head -15 || true
$ADB exec-out screencap -p > "${CAPTURA:-/dev/null}"
