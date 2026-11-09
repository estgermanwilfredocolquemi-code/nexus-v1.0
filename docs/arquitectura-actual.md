# Arquitectura actual de H3

## Inspección del código

La fuente principal es `Program.cs` (4,000+ líneas), compilada por `nexus 3.csproj` como aplicación de consola `net10.0`, con tipos en el mismo archivo. La solución se llama `nexus v1.0.slnx`. El programa configura la consola, ejecuta alta y selección, elige realidad, inicializa misión y entra a un bucle de comandos.

## Tipos y responsabilidades observables

| Tipo | Funcionalidad actual | Límite para evolución |
|---|---|---|
| `Program` | Flujo, estado global, entradas/salidas, mapas, encuentros, IRIS, extracción, presentación y reglas | Acoplamiento alto; métodos estáticos y `Console` mezclan presentación y dominio |
| `Personaje` | Base abstracta con salud, ataque, defensa, escudo, amenaza, hostilidad, daño y ataque | `AplicarEstadisticas` vacío por defecto; no hay persistencia ni interfaz desacoplada |
| `Humano`, `Robot` | Perfiles y atributos especializados | Reglas de equilibrio embebidas en constructores |
| `Enemigo`, `EnemigoRobot` | Actores hostiles y reacciones de combate | Combate invoca directamente salida de consola |
| `TipoObjeto`, `Objeto` | Tipo, nombre y cantidad de ítem | Sin identificador estable, definición/catálogo separado ni reglas de apilamiento configurable |
| `Inventario` | Agregar, buscar, eliminar, contar | Lista mutable expuesta mediante `Items`; no guarda cambios |
| `Mision` | Cuatro hitos, completada y experiencia | Una sola misión inicial; booleanos limitan campañas extensibles |
| `EventoNexus` | Tipo, descripción y hora en memoria | Historial temporal solo de sesión |

## Funcionalidad existente

El estado incluye explorador, realidad, energía, conexión, peligro, investigaciones IRIS, recursos, modo conectado, posición de mapa y eventos. Las realidades tienen matrices `char[,]`. El menú numérico ofrece explorar, analizar IRIS, estado, recuperar energía, escanear realidad, mapa, comunicación, narrativa y desconexión. Hay encuentros y extracción; el inventario tiene consumibles y objetos de misión. La misión predeterminada es “LA ANOMALÍA PERDIDA”.

## Limitaciones comprobables

- No existen formularios Windows Forms: el `.csproj` es `OutputType=Exe`, `TargetFramework=net10.0`, sin `UseWindowsForms` ni paquetes SQL.
- No hay repositorios, servicios, entidades persistibles ni scripts de base de datos en el código inspeccionado.
- Los datos viven en campos estáticos/listas durante la ejecución; `EventoNexus` usa `DateTime.Now`; no hay guardado/carga ni identidad de partidas.
- `Program` combina lectura por consola, narrativa, reglas y estado. Portar línea a línea a controles WinForms replicaría el acoplamiento y bloquearía el hilo de UI si conserva esperas/bucles.
- Mapas se expresan con símbolos y matrices, lo cual facilita un render 2D por celdas, aunque requiere separar los datos del dibujo y especificar coordenadas/acciones.
- Código de dominio escribe con `Console`, por lo que la interfaz gráfica requiere extraer presentación y emitir resultados/estado a la UI.

## Migración desde los artefactos

`Program.cs` es la fuente autoritativa para las reglas. Mantener inicialmente `Humano`, `Robot`, enemigos, inventario, misión, eventos, realidades, objetivos y valores. Convertir estado global a `GameState`/`Expedition`; mover lógica de acciones a servicios; traducir cada método de presentación a eventos/vistas WinForms. La propuesta de capas está en [arquitectura-propuesta.md](arquitectura-propuesta.md).
