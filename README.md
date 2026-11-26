# NEXUS v1.0 — Aventura integrada

NEXUS conserva su juego original en C# y añade una aventura gráfica de cuatro realidades dentro de Windows Forms. El modo nuevo usa mapas ilustrados con movimiento libre y guardado local. El modo **NEXUS v1.0 clásico** mantiene disponible el flujo original, sus sistemas IRIS y sus expediciones guardadas en SQL Server.

## Ejecutar

- Para abrirlo directamente en Windows, ejecuta `NEXUS.exe` dentro de la carpeta `ejecutable` del paquete publicado.
- El ejecutable necesita **.NET 10 Desktop Runtime para Windows**. La aventura no requiere configurar SQL Server.
- Para abrir el código en Visual Studio, usa `nexus v1.0.slnx` y selecciona el perfil `NEXUS Windows Forms`.
- Para iniciar desde terminal con .NET 10 instalado: `dotnet run --project "WindowsForms/NEXUS-WindowsForms.csproj"`.
- El modo clásico de consola se ejecuta con `dotnet run --project "nexus 3.csproj"`.

## Contenido de la aventura

- **TIERRA-01:** bosque, refugios, ciudad pequeña, robots, cofres y mazmorra.
- **TIERRA-02:** zona zombi, edificios abandonados, autómatas infectados y mazmorra.
- **TIERRA-07:** frontera criogénica, ruinas, centinelas de hielo y mazmorra.
- **NEXUS-09:** ciudad digital, robots de la brecha, mazmorra y jefe final.
- Al comenzar una aventura, puedes escoger cualquiera de las cuatro dimensiones. Cada una tiene una distribución y terreno propios; los portales permiten avanzar a las siguientes.
- Movimiento con **WASD** o flechas; **E** interactúa; **I** abre el inventario; **M** muestra misiones; **Esc** abre pausa.
- Combate por turnos: **1** atacar, **2** usar objeto, **3** retirarse de enemigos comunes.
- Cofres entregan créditos, pociones, armas y protección. El inventario permite usar curaciones y equipar armas o armaduras.
- Enemigos y jefes entregan experiencia. Los niveles aumentan la salud y el ataque.
- Cada mundo tiene una misión principal, un encargo secundario y una mazmorra. Derrotar al jefe activa el portal al siguiente mundo.
- Las partidas de aventura se guardan como archivos JSON en `%LOCALAPPDATA%\NEXUS\Partidas`; no necesitan SQL Server.

## Sistemas originales conservados

`Program.cs` permanece como el juego original de consola y el modo **NEXUS v1.0 clásico** sigue disponible desde el menú de Windows Forms. Incluye sus cuatro realidades, exploración, IRIS, energía, conexión, peligro, estadísticas, objetivos y eventos. Las partidas clásicas siguen usando SQL Server Express y el menú **Expediciones SQL** permite continuar o eliminar esos guardados. La opción **Configuración SQL** inicializa la base cuando se usa por primera vez.

Por defecto, el juego clásico usa `Server=localhost\\SQLEXPRESS;Database=NEXUS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True`. Se puede configurar otra instancia con `NEXUS_SQL_CONNECTION_STRING`. La aventura gráfica no depende de esa base.

## Pendiente para una etapa posterior

Música y efectos de sonido originales, y habilidades especiales de combate.
<<<<<<< HEAD
=======

>>>>>>> a92c8774b8e317281b29c03b3bb90af0d8e400cc
