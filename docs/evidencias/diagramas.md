# Diagramas de NEXUS

Estos diagramas acompañan la propuesta documental y no representan pantallas ya implementadas.

## Ciclo de partida

```mermaid
flowchart LR
 Menu --> Seleccion[Explorador y realidad] --> Mapa[Explorar / IRIS]
 Mapa --> Recurso[Inventario]
 Mapa --> Amenaza[Encuentro]
 Recurso --> Estado[Revisar estado]
 Amenaza --> Estado
 Estado -->|Continuar| Mapa
 Estado -->|Objetivos completos| Extraccion --> Resumen[Guardar progreso]
```

## Arquitectura

```mermaid
flowchart LR
 UI[WinForms] --> APP[Servicios de aplicación]
 APP --> DOM[Dominio NEXUS]
 APP --> REPO[Repositorios]
 REPO --> SQL[(SQL Server)]
 DOM --> APP --> UI
```

Ver las definiciones y descripciones completas en [diseño del juego](../diseno-del-juego.md) y [arquitectura propuesta](../arquitectura-propuesta.md).
