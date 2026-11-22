# NEXUS v1.0 — Programación II

NEXUS es una aventura de exploración multiversal en C#/.NET 10. La solución conserva su versión de consola y añade una interfaz de escritorio Windows Forms que reutiliza el juego existente.

## Funcionalidades implementadas

Ambas interfaces incluyen el flujo de registro y selección, perfiles Humano/Robot y entidades, cuatro realidades con mapas `char[,]`, exploración, encuentros, escaneo/IRIS, energía, conexión y peligro, inventario con tipos de objeto, objetivos de misión, eventos de sesión y extracción. No incluye base SQL ni guardado persistente.

## Tecnologías y ejecución

- C#/.NET 10; solución `nexus v1.0.slnx` con proyectos de consola y Windows Forms.
- Visual Studio con el SDK .NET 10 y la carga de trabajo **Desarrollo de escritorio de .NET**.
- Desde la carpeta del proyecto: `dotnet run --project "nexus 3.csproj"`.
- Para iniciar la interfaz en Visual Studio, selecciona el perfil compartido `NEXUS Windows Forms` en la barra de inicio.

El runtime/SDK requerido debe estar instalado. No se necesita SQL Server para ejecutar el prototipo actual.

## Documentación

- [Diseño del juego](docs/diseno-del-juego.md): visión, ciclo, reglas, wireframes y alcance.
- [Arquitectura actual](docs/arquitectura-actual.md): clases, flujo y limitaciones observadas en H3.
- [Arquitectura propuesta](docs/arquitectura-propuesta.md): capas WinForms, dominio, aplicación e infraestructura.
- [Modelo de datos](docs/modelo-de-datos-propuesto.md): entidades, relaciones y guardado SQL Server.
- [Análisis técnico](docs/analisis-tecnico.md): alternativas, decisiones, fuentes y verificación.
- [Evidencias y diagramas](docs/evidencias/): diagramas Mermaid incluidos en los documentos Markdown.

## Estado y siguiente etapa

La solución comparte `Program.cs` entre las dos interfaces. `WindowsForms/ConsoleBridge.cs` adapta la entrada y salida del juego a los controles gráficos. Las capturas de laboratorio para H4.01 deben documentarse con fecha y hora reales; consulta `docs/evidencias/H4.01-guia-de-capturas.md`.
