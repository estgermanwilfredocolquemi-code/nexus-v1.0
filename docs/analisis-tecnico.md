# Análisis técnico y decisiones

## Alternativas consideradas

| Decisión | Alternativas | Evaluación y recomendación |
|---|---|---|
| Interfaz | Consola actual; Windows Forms; WPF; motor Unity | La consola permite validar reglas y narrativa, pero no satisface el objetivo gráfico. WinForms cumple explícitamente escritorio C# y se adapta a mapa 2D de celdas y paneles; es la opción recomendada por alcance. WPF ofrece enlace de datos/estilos más expresivos, pero cambia el marco exigido. Unity está orientado a juegos con render/animación 3D y añade complejidad que el diseño actual no necesita. |
| Persistencia | Sin guardar; archivo JSON/SQLite; SQL Server | Memoria actual pierde progreso. SQLite/JSON simplifican despliegue local; SQL Server es requisito y permite integridad relacional/transacciones, con coste de instalación y configuración. Recomendación: SQL Server y repositorios aislados; para demo usar una instancia local configurada, no conexión incrustada. |
| Organización | `Program.cs` monolítico; capas/proyectos separados | Monolito refleja prototipo H3 pero une consola, estado y reglas. Capas aumentan archivos iniciales, reducen acoplamiento y permiten sustituir consola por WinForms. Recomendar separar Domain/Application/Infrastructure/WinForms incrementalmente, no reescribir todo a la vez. |
| Mapa | Texto/matriz char; panel de botones; dibujo personalizado | La matriz existente se puede dibujar con un panel de celdas y símbolos/colores. Botones por celda ayudan accesibilidad/selección sencilla; dibujo personalizado escala mejor para mapas grandes. Empezar con mapa pequeño de controles o `Panel` con hit testing según tamaño real y perfilar antes de optimizar. |
| Estado de misión | Cuatro booleanos; objetivos relacionales | Booleanos representan H3 con claridad, pero no escalan. Persistir instancias/objetivos con claves; inicializar los cuatro objetivos actuales para compatibilidad. |

## Decisiones recomendadas

1. Preservar primero todas las reglas, perfiles, cuatro realidades y la misión de H3; extraer lógica antes de remodelar balance.
2. Usar WinForms para interfaz 2D, formularios/paneles y servicio de aplicación. Mantener los controles fuera del dominio.
3. Usar SQL Server vía `Microsoft.Data.SqlClient`/repositorios y parámetros; transacción para cada guardado coherente. El paquete concreto debe fijarse al crear solución según TFM soportado.
4. Guardar expedición, inventario, objetivos y eventos; cargar estado consistente y versionar mapas.
5. Hacer operaciones DB asíncronas desde WinForms, mantener el hilo UI libre y reportar error recuperable.
6. Migrar por funcionalidad y conservar el ejecutable H3 durante la transición como referencia hasta cubrir sus recorridos.

## Viabilidad, riesgos y mitigación

La mayor carga no es dibujar la matriz, sino extraer estado y reglas dispersos entre los métodos de `Program`. Mitigar migrando acciones verticales y comparando el resultado con la versión de consola. La disponibilidad/versionado de .NET 10 y el soporte del entorno objetivo deben verificarse al iniciar la etapa de implementación; el proyecto actual apunta a `net10.0` sin WinForms. SQL Server agrega dependencia de instalación: validar conexión al inicio, proporcionar configuración documentada y transacciones. La interfaz debe conservar mapa legible con símbolos, no solo color; probar escalado y teclado. El modelo `char[,]` puede evolucionar a `WorldMap` sin eliminar la autoría actual.

## Fuentes técnicas consultadas

Fuentes primarias oficiales consultadas el 8 de noviembre de 2026:

- [Microsoft Learn: Windows Forms overview](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/) — Windows Forms como marco de UI de escritorio .NET.
- [Microsoft Learn: Windows Forms project properties](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/?view=netdesktop-10.0) — configuración del proyecto y destino Windows; verificar plantilla/SDK instalado antes de cambiar TFM.
- [Microsoft Learn: Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) — proveedor .NET para SQL Server.
- [Microsoft Learn: Commands and Parameters](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/commands-and-parameters) — parámetros en comandos de datos.
- [Microsoft Learn: Local transactions](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/local-transactions) — atomicidad de operaciones relacionadas.
- [Microsoft Learn: SQL Server rowversion](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql) — control de versión de filas, útil para detectar sobrescrituras concurrentes.

## Verificación de información obtenida con IA

Las afirmaciones del análisis técnico se cotejaron con documentación oficial de Microsoft indicada arriba. La inspección local confirmó el TFM `net10.0`, salida `Exe`, ausencia de `UseWindowsForms`/dependencia SqlClient en `nexus 3.csproj`, y las clases/métodos descritos en `Program.cs`. Por tanto, Windows Forms y SQL Server son recomendaciones futuras, no capacidades implementadas. La documentación de API específica debe revisarse contra la versión de SDK/paquete que se instale en la siguiente fase; no se afirma que un paquete o instalador esté presente en este repositorio.
