#!/bin/bash
# Instala Saiyan Online en el móvil conectado por USB (no en el emulador) y copia los recursos.
# Requiere la depuración USB activada y autorizada en el móvil.
set -e
cd "$(dirname "$0")"
ADB=~/Library/Android/sdk/platform-tools/adb
APP=com.saiyanonline.android
MOVIL=$($ADB devices | awk 'NR>1 && $2=="device" && $1 !~ /^emulator-/ {print $1; exit}')
if [ -z "$MOVIL" ]; then
  echo "No hay ningún móvil autorizado. Estado de adb:"; $ADB devices; exit 1
fi
echo "Móvil: $($ADB -s $MOVIL shell getprop ro.product.model | tr -d '\r') ($MOVIL), Android $($ADB -s $MOVIL shell getprop ro.build.version.release | tr -d '\r')"
APK=android/SaiyanAndroid/bin/Release/net8.0-android/$APP-Signed.apk
if ! SALIDA=$($ADB -s $MOVIL install -r $APK 2>&1); then
  if echo "$SALIDA" | grep -q "INSTALL_FAILED_UPDATE_INCOMPATIBLE"; then
    # Instalada con otra firma (la de pruebas): hay que desinstalarla; los recursos se copian de nuevo abajo.
    echo "Firma distinta: desinstalando la versión anterior..."
    $ADB -s $MOVIL uninstall $APP
    $ADB -s $MOVIL install $APK
  else
    echo "$SALIDA"; exit 1
  fi
else
  echo "$SALIDA" | tail -1
fi
DESTINO=/sdcard/Android/data/$APP/files/resources
$ADB -s $MOVIL shell mkdir -p $DESTINO
if [ "$($ADB -s $MOVIL shell "ls $DESTINO/coffre 2>/dev/null | wc -l" | tr -d '\r ')" -lt 250 ]; then
  echo "Copiando recursos (595 MB)..."
  $ADB -s $MOVIL push original/resources/coffre $DESTINO/ | tail -1
fi
# Lo copiado por adb pertenece al usuario shell y la app no podría leerlo.
$ADB -s $MOVIL shell "chmod -R a+rwX $DESTINO/coffre 2>/dev/null; true"
$ADB -s $MOVIL shell am force-stop $APP
$ADB -s $MOVIL shell monkey -p $APP -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1
echo "Juego abierto en el móvil."
