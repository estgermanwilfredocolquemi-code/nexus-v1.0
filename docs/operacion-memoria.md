# Operación Memoria: guardado SQL de NEXUS

## Alcance implementado

Windows Forms conserva el motor de `Program.cs` y añade un adaptador de persistencia con Microsoft.Data.SqlClient. Un guardado registra el perfil, la realidad, el estado de la expedición y del personaje, la misión y sus objetivos, inventario, eventos, posición y matriz de exploración. La matriz serializada permite continuar sin regenerar el terreno. El JSON versionable conserva el estado completo del motor; las tablas relacionales permiten consultar y verificar sus datos principales.

Desde el menú principal, **Configuración SQL** crea la base `NEXUS` si no existe y aplica `WindowsForms/database/002-schema.sql`. Durante la partida, **Guardar partida** crea o actualiza una ranura con nombre. **Continuar / partidas** lista guardados, recupera el estado o elimina una ranura; **Estadísticas** consulta el total y objetivos completados. Las escrituras de expedición, inventario, objetivos y eventos ocurren dentro de una transacción.

Durante el juego se puede activar **Voz IRIS**. La aplicación usa SAPI y las voces instaladas localmente en Windows. La salida escrita no depende de que SAPI esté disponible. No se incluyó una banda sonora: Final02 y el repositorio de NEXUS no contienen recursos de música/audio para integrar.

## Requisitos y conexión

- Windows con .NET 10 SDK y carga de trabajo de Visual Studio **Desarrollo de escritorio de .NET**.
- SQL Server Express instalado y servicio de instancia iniciado. La cuenta de Windows debe poder crear la base durante la primera configuración.
- Al compilar, Visual Studio restaura `Microsoft.Data.SqlClient` desde NuGet.
- Predeterminado: `Server=localhost\SQLEXPRESS;Database=NEXUS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True`.
- Para otra instancia, define `NEXUS_SQL_CONNECTION_STRING` en el entorno de inicio de Visual Studio. No incluyas contraseñas en el repositorio.

También puedes ejecutar en SSMS `001-create-database.sql` y después `002-schema.sql`. Ambos scripts son idempotentes para la creación inicial y el esquema.

## Verificación de recuperación (supervivencia del guardado)

1. Inicia SQL Server Express y abre `nexus v1.0.slnx` en Visual Studio.
2. Selecciona el proyecto **NEXUS Windows Forms** como inicio y ejecuta **Configuración SQL** desde el menú.
3. Inicia una expedición, entra a explorar, camina, recoge algún recurso o completa un objetivo y vuelve al menú de juego con `Q`.
4. Pulsa **Guardar partida** y asigna un nombre reconocible.
5. Cierra la aplicación por completo y vuelve a ejecutarla. Desde **Continuar / partidas**, recupera el guardado: deben reaparecer realidad, energía, misión, inventario, posición, mapa y eventos.
6. Verifica **Estadísticas**; elimina solo la ranura de prueba cuando ya no la necesites.
7. En una carpeta vacía, clona el repositorio publicado, abre la solución y compílala. Configura SQL Server en esa máquina, inicia la aplicación y crea un guardado allí. El esquema y el código provienen del repositorio; los datos de partida pertenecen a la instancia SQL configurada.

El proyecto no guarda contraseñas ni depende de una base creada en la carpeta local. Para usar guardados entre computadoras, cada instalación necesita acceso a la misma instancia SQL Server o una instancia compartida configurada de forma segura.

## Entidades

`ExplorerProfile`, `Reality`, `MissionDefinition`, `Expedition`, `ItemDefinition`, `InventoryItem`, `ObjectiveProgress` y `GameEvent` normalizan los elementos principales. `Expedition.SnapshotJson` conserva los campos y el mapa completo para rehidratar las clases actuales del juego. El borrado de una expedición elimina sus registros dependientes mediante claves foráneas `ON DELETE CASCADE`.
