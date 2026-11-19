# Paquete de trabajo — NEXUS Hito 4: Operación Memoria

Este paquete contiene el primer entregable técnico preparatorio para incorporar SQL Server a NEXUS.

## Contenido
- `database/01_create_nexus_db.sql`: crea `NEXUS_DB`, 10 tablas relacionadas, restricciones, índices y catálogos iniciales.
- `docs/modelo-relacional.md`: entidades, relaciones y atributos persistentes.
- `docs/arquitectura-integracion.md`: estado actual, arquitectura objetivo y pasos de integración.
- `presentacion/NEXUS_Hito4_Operacion_Memoria.pptx`: presentación técnica de 7 diapositivas.

## Ejecución inicial
1. Abrir SQL Server Management Studio.
2. Conectarse a una instancia local de SQL Server.
3. Abrir y ejecutar `database/01_create_nexus_db.sql`.
4. Actualizar el explorador de objetos y comprobar la base `NEXUS_DB`, las 10 tablas y sus relaciones.

## Estado honesto
Este paquete establece el diseño y crea el esquema SQL. No contiene todavía la integración C# funcional, los formularios Windows Forms ni una prueba de supervivencia ejecutada. Esos pasos requieren desarrollar y probar la aplicación en un entorno con SQL Server y el SDK .NET correspondiente.