# NEXUS v1.0

Proyecto de consola en C# para Programación II. NEXUS simula una misión de exploración multiversal e incluye personajes, inventario, misión y eventos.

## Requisitos

- SDK de .NET 10.

## Ejecutar

Desde la carpeta del repositorio:

```bash
dotnet run --project "nexus 3.csproj"
```

## Estructura

- `Program.cs`: flujo del juego, interfaz y clases del dominio.
- `nexus 3.csproj`: configuración del proyecto .NET.
- `docs/Manual-tecnico-H3.md`: arquitectura, análisis de POO, ejemplos, diagramas y preguntas razonadas para preparar el Hito 3.

## Manual técnico

Consulta [NEXUS — Manual técnico y preparación para el examen H3](docs/Manual-tecnico-H3.md). El manual distingue las características que existen en el código de los ejemplos complementarios de interfaces y LINQ, que no están implementados actualmente.

## Higiene del repositorio

Los archivos de salida de Visual Studio y .NET (`.vs/`, `bin/`, `obj/`) deben permanecer ignorados. No publiques contraseñas, tokens ni otros secretos.
