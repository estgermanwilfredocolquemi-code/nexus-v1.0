# NEXUS v1.0 — Programación II

NEXUS es una aventura de consola de exploración multiversal. El jugador registra un explorador, elige perfil y realidad, investiga anomalías con IRIS, administra energía e inventario, afronta entidades y busca extraerse para completar “LA ANOMALÍA PERDIDA”. Este repositorio documenta la evolución desde el prototipo H3 hacia una experiencia gráfica de escritorio C# con Windows Forms y persistencia SQL Server.

## Funcionalidades implementadas

La versión del código fuente actual incluye flujo de registro y selección, perfiles Humano/Robot y entidades, cuatro realidades con mapas `char[,]`, exploración, encuentros, escaneo/IRIS, energía, conexión y peligro, inventario con tipos de objeto, objetivos de misión, eventos de sesión y extracción. Es una aplicación de consola; no incluye interfaz WinForms ni base SQL ni guardado persistente.

## Tecnologías y ejecución

- C#/.NET; proyecto `nexus 3.csproj` con `net10.0`, salida de consola.
- Visual Studio compatible con el SDK .NET 10 o SDK instalado, o CLI `dotnet`.
- Desde la carpeta del proyecto: `dotnet run --project "nexus 3.csproj"`.

El runtime/SDK requerido debe estar instalado. No se necesita SQL Server para ejecutar el prototipo actual.

## Documentación

- [Diseño del juego](docs/diseno-del-juego.md): visión, ciclo, reglas, wireframes y alcance.
- [Arquitectura actual](docs/arquitectura-actual.md): clases, flujo y limitaciones observadas en H3.
- [Arquitectura propuesta](docs/arquitectura-propuesta.md): capas WinForms, dominio, aplicación e infraestructura.
- [Modelo de datos](docs/modelo-de-datos-propuesto.md): entidades, relaciones y guardado SQL Server.
- [Análisis técnico](docs/analisis-tecnico.md): alternativas, decisiones, fuentes y verificación.
- [Evidencias y diagramas](docs/evidencias/): diagramas Mermaid incluidos en los documentos Markdown.

## Estado y siguiente etapa

La entrega actual es documental y de diseño. La siguiente implementación debe extraer las reglas de `Program.cs`, añadir proyectos/capas WinForms y SQL Server gradualmente y verificar cada recorrido contra el prototipo. Consultar [docs](docs/) antes de iniciar esa etapa.
