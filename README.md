# NEXUS v1.0 — Aventura de ciencia ficción

NEXUS es una aventura de exploración para Windows escrita en C#. La campaña gráfica ahora muestra el mundo en **3D en tiempo real** mediante OpenGL nativo de Windows. El juego también conserva el modo original de consola y sus expediciones SQL.

## Abrir en Windows

- Ejecuta `NEXUS.exe` desde el paquete publicado, o abre `nexus v1.0.slnx` en Visual Studio y elige `NEXUS Windows Forms`.
- Requiere **.NET 10 Desktop Runtime para Windows**. El render 3D usa `opengl32.dll` que incluye Windows; no hay que instalar un motor ni paquetes adicionales.
- Para iniciar desde terminal: `dotnet run --project "WindowsForms/NEXUS-WindowsForms.csproj"`.
- Para iniciar el modo clásico: `dotnet run --project "nexus 3.csproj"`.

## Mundo y campaña

- Cuatro realidades: Bosque Vivo, Zona Cero, Frontera Criogénica y Ciudad NEXUS.
- El mapa exterior se genera por coordenadas y continúa al explorar en cualquier dirección. Bosques, ruinas, agua, enemigos y suministros vuelven a aparecer en el mismo sitio al regresar.
- Vista en tercera persona, terreno tridimensional, árboles, arbustos, rocas, ruinas, portales, cofres y personajes con extremidades animadas.
- Un prólogo presenta la primera fractura, el misterio de IRIS y el conflicto con el Titán. Cada héroe tiene un origen y una ventaja jugable.
- Mara, Tarek, Niva y Kael ofrecen encargos: puedes aceptarlos, completar su objetivo y volver a reclamar la recompensa.
- Las mazmorras conectan cada capítulo. El Titán final requiere nivel 2 e IRIS.
- El tema ambiental original se genera y se guarda en `%LOCALAPPDATA%\NEXUS\Audio`; el botón **Música** permite silenciarlo o reactivarlo.

## Controles

- **WASD** o **flechas**: moverse.
- **E**: hablar, abrir cofres, entrar a mazmorras y usar portales.
- **I**: inventario; **M**: diario de misiones; **Esc**: pausa.
- En combate: **1** ataque normal, **2** habilidad especial, **3** defensa, **4** objeto y **5** intentar huir.
- El golpe crítico depende del héroe. Las habilidades consumen energía; defenderse reduce el daño y recarga energía. Los enemigos cargan ataques pesados que se pueden bloquear.

## Guardados y modo clásico

Las partidas gráficas se guardan como JSON en `%LOCALAPPDATA%\NEXUS\Partidas`; no necesitan SQL Server. `Program.cs` y el menú **NEXUS v1.0 clásico** conservan el juego original de consola y sus expediciones SQL Server.

