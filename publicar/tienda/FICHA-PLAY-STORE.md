# Saiyan Online para Android — ficha de tienda

Todo lo que pide Google Play Console (y sirve igual para Galaxy Store, AppGallery, Uptodown…).

## Archivos

| Qué | Archivo |
|---|---|
| Paquete para Google Play | `android/SaiyanAndroid/bin/Release/net8.0-android/com.saiyanonline.android-Signed.aab` |
| APK para webs y otras tiendas | `android/SaiyanAndroid/bin/Release/net8.0-android/com.saiyanonline.android-Signed.apk` |
| Icono 512×512 | `publicar/tienda/icono-512.png` |
| Gráfico destacado 1024×500 | `publicar/tienda/grafico-destacado-1024x500.png` |
| Capturas (proporción 2:1) | `publicar/tienda/captura-1.png` … `captura-8.png` |
| Recursos que descarga la app | `publicar/coffre.zip` (va en la release de GitHub) |

- Nombre del paquete: `com.saiyanonline.android` (no se puede cambiar después de publicar).
- Versión: 0.1.0 (código de versión 1). Cada actualización debe subir `ApplicationVersion` en el `.csproj`.
- Firma: `publicar/firma/` — **haz copia de seguridad**; sin ella no se puede actualizar la app.
  En Play Console, al activar "Firma de aplicaciones de Google Play", sube el AAB y deja que Google gestione la clave de distribución.

## Textos

**Nombre:** Saiyan Online

**Descripción breve (máx. 80):**
MMORPG 2D de artes marciales: entrena, lucha y explora con otros jugadores.

**Descripción completa:**
Saiyan Online es un MMORPG 2D en línea. Crea tu guerrero, entrena tu ki, aprende técnicas
y lucha junto a cientos de jugadores en un mundo persistente.

• Combate en tiempo real con combos, bloqueos, esquivas y técnicas de ki.
• Misiones de historia por capítulos, profesiones, mercado entre jugadores y clasificaciones.
• Ciclo de día y noche, clima y un mundo que se recorre a pie o volando.
• Chat, grupos, amigos y gremios.
• Controles táctiles: joystick, botones de acción y barra de técnicas.

Es la versión para Android de Saiyan Online: usa la misma cuenta y los mismos servidores que
la versión de ordenador. La primera vez descarga los recursos del juego (unos 520 MB);
se recomienda wifi.

Juego gratuito. Requiere conexión a internet.

**Categoría:** Juegos → Rol
**Etiquetas:** MMORPG, rol, acción, multijugador
**Precio:** Gratis

## Clasificación de contenido (cuestionario IARC)

- Violencia: sí, de dibujos animados/fantasía, sin sangre realista.
- Interacción entre usuarios: sí (chat sin moderar en tiempo real, intercambio entre jugadores).
- Compras dentro de la app: no.
- Comparte la ubicación: no.
Resultado esperado: PEGI 7–12 / Everyone 10+ / Teen según región.

## Seguridad de los datos (formulario de Play Console)

- Datos recogidos: nombre de usuario y contraseña de la cuenta (para iniciar sesión), mensajes
  del chat y datos de partida. Los gestiona el servidor de Saiyan Online.
- Se envían cifrados: sí (conexión cifrada del juego).
- Se comparten con terceros: no.
- Analítica, publicidad o ubicación: no.
- Los jugadores pueden pedir que se borre su cuenta al equipo de Saiyan Online (indicar contacto).

**Confírmalo con el equipo de Saiyan Online antes de enviarlo**: son ellos quienes saben qué guarda el servidor.

## Política de privacidad

Play exige una dirección web. Borrador en `publicar/tienda/privacidad.md`; se puede publicar en
GitHub Pages del repositorio. Debe revisarla el equipo de Saiyan Online.

## Avisos antes de enviar

- Google revisa las apps con personajes, nombres o imágenes de franquicias conocidas. Si el
  juego usa material de Dragon Ball sin licencia de sus propietarios, Google puede rechazarla o
  retirarla y penalizar la cuenta de desarrollador.
- Cuenta de desarrollador de Google Play: pago único de 25 $ y verificación de identidad; para
  cuentas personales nuevas, Google exige una prueba cerrada con al menos 12 personas durante
  14 días antes de poder publicar en producción.
- Tiendas sin coste de alta: Samsung Galaxy Store, Huawei AppGallery, Uptodown, itch.io (sube la APK).
