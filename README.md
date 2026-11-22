# NEXUS v1.0 — Programación II

NEXUS es una aventura de exploración multiversal en C#/.NET 10. La solución conserva su versión de consola y añade una interfaz de escritorio Windows Forms que reutiliza el juego existente.

## Funcionalidades implementadas

Ambas interfaces incluyen el flujo de registro y selección, perfiles Humano/Robot y entidades, cuatro realidades con mapas `char[,]`, exploración, encuentros, escaneo/IRIS, energía, conexión y peligro, inventario con tipos de objeto, objetivos de misión, eventos de sesión y extracción. Windows Forms incorpora guardado, consulta, recuperación y eliminación de expediciones en SQL Server. La versión de consola se conserva sin reemplazo.

## Tecnologías y ejecución

- C#/.NET 10; solución `nexus v1.0.slnx` con proyectos de consola y Windows Forms.
- Visual Studio con el SDK .NET 10 y la carga de trabajo **Desarrollo de escritorio de .NET**.
- Desde la carpeta del proyecto: `dotnet run --project "nexus 3.csproj"`.
- Para iniciar la interfaz en Visual Studio, selecciona el perfil compartido `NEXUS Windows Forms` en la barra de inicio.
- El proyecto Windows Forms requiere restaurar el paquete NuGet `Microsoft.Data.SqlClient` (7.1.2).
- Inicia SQL Server Express local y usa **Configuración SQL** en el menú NEXUS para crear la base `NEXUS` y aplicar el esquema. La cuenta integrada de Windows necesita permiso para crear esa base. También puedes ejecutar `WindowsForms/database/001-create-database.sql` y luego `002-schema.sql` en SQL Server Management Studio.
- Por defecto, NEXUS usa `Server=localhost\\SQLEXPRESS;Database=NEXUS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True`. Para otra instancia, define `NEXUS_SQL_CONNECTION_STRING` antes de iniciar Visual Studio. No guardes credenciales en Git.
- En el juego, pulsa **Guardar partida**. En el menú principal usa **Continuar / partidas** para recuperar o eliminar una partida; **Estadísticas** consulta las partidas y objetivos guardados.
- El botón **Voz IRIS** activa respuestas habladas con la voz local de Windows (SAPI); funciona sin conexión y es opcional.

El proyecto de consola se puede ejecutar sin SQL Server. Windows Forms requiere SQL Server para usar las funciones de guardado, consulta, recuperación y eliminación.

## Documentación

- [Diseño del juego](docs/diseno-del-juego.md): visión, ciclo, reglas, wireframes y alcance.
- [Arquitectura actual](docs/arquitectura-actual.md): clases, flujo y limitaciones observadas en H3.
- [Arquitectura propuesta](docs/arquitectura-propuesta.md): capas WinForms, dominio, aplicación e infraestructura.
- [Modelo de datos](docs/modelo-de-datos-propuesto.md): entidades, relaciones y guardado SQL Server.
- [Análisis técnico](docs/analisis-tecnico.md): alternativas, decisiones, fuentes y verificación.
- [Evidencias y diagramas](docs/evidencias/): diagramas Mermaid incluidos en los documentos Markdown.

## Estado y siguiente etapa

La solución comparte `Program.cs` entre las dos interfaces. `WindowsForms/ConsoleBridge.cs` adapta la entrada y salida del juego a los controles gráficos. `WindowsForms/NexusPersistence.cs` implementa la persistencia SQL transaccional y `WindowsForms/database/002-schema.sql` define las tablas relacionales. `WindowsForms/IrisSpeechService.cs` habilita voz SAPI local opcional. No se encontró música ni archivos de audio en Final02 o NEXUS para integrar. Consulta `docs/operacion-memoria.md` para preparar, ejecutar y verificar guardado y recuperación. Las capturas de laboratorio para H4.01 deben documentarse con fecha y hora reales; consulta `docs/evidencias/H4.01-guia-de-capturas.md`.
