# Interfaz y asistente JARVIS

## Qué incluye esta mejora

- Los accesos rápidos de la ventana envían las mismas opciones que el menú original del juego.
- El botón **GUARDAR PARTIDA** conserva el flujo de guardado en SQL Server.
- **JARVIS** activa/desactiva el servicio de voz local de Windows (SAPI) y supervisa el inventario durante la expedición.
- Al activarlo, JARVIS informa nombre del explorador, realidad, energía, salud, misión e inventario.
- Mientras está activo, revisa el inventario cada 1,8 segundos y avisa por texto y voz cuando una cantidad aumenta o disminuye.
- Al desactivarlo, detiene la supervisión y apaga la voz de JARVIS.

## Cómo probarlo

1. Abre `nexus v1.0.slnx` en Visual Studio con el SDK .NET 10 y la carga de trabajo de escritorio .NET.
2. Establece `WindowsForms/NEXUS-WindowsForms.csproj` como proyecto de inicio.
3. Ejecuta la aplicación, registra un explorador y comienza una expedición.
4. En la barra inferior, pulsa **JARVIS: DESACTIVADO** para activarlo. Debe cambiar a **JARVIS: ACTIVADO**.
5. JARVIS leerá el inventario inicial y notificará cambios que ocurran mientras exploras o utilizas objetos.
6. Vuelve a pulsar el botón para desactivarlo.

## Mapa de accesos rápidos

| Botón | Acción original |
|---|---|
| EXPLORAR · 1 | Explorar |
| ANALIZAR IRIS · 2 | Analizar IRIS |
| ESTADO · 3 | Ver estado |
| ENERGÍA · 4 | Recuperar energía |
| ESCANEAR · 5 | Escanear realidad |
| MAPA · 6 | Abrir mapa |
| COMUNICAR · 7 | Enlace IRIS |
| NARRATIVA · 8 | Historia |
| ASISTENTE · 0 | Asistente IRIS |
| SALIR · 9 | Desconectar |

## Requisitos y límites actuales

- La voz usa SAPI instalado en Windows. Si el equipo no tiene una voz configurada, el panel de texto sigue mostrando los avisos.
- JARVIS supervisa el inventario que ya expone el estado guardable de la partida; no es una IA conversacional ni un modelo remoto.
- Los botones ejecutan las opciones existentes del juego. Esta mejora no convierte todavía el renderizado de consola en un mapa 2D de acción ni implementa el combate Zelda descrito en el diseño.
- La compilación debe verificarse en Windows con .NET 10; el entorno de edición no dispone del SDK .NET para ejecutar esa comprobación localmente.
