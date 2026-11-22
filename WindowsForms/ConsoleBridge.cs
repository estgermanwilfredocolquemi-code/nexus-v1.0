using System;

// Adaptador sencillo para reutilizar íntegra la lógica de consola dentro de la ventana.
// La interfaz captura las entradas y muestra las salidas del juego.
internal static class Console
{
    public static ConsoleColor BackgroundColor { get; set; }
    public static ConsoleColor ForegroundColor { get; set; }
    public static bool CursorVisible { get; set; }
    public static string Title { get; set; } = "NEXUS SYSTEM v1.0";
    public static void SetWindowSize(int width, int height) { }
    public static void SetBufferSize(int width, int height) { }
    public static void ResetColor() { }
    public static void Clear() => NexusWinForms.UiConsole.Clear();
    public static void Write(string value) => NexusWinForms.UiConsole.Write(value);
    public static void Write(char value) => NexusWinForms.UiConsole.Write(value.ToString());
    public static void Write(object value) => NexusWinForms.UiConsole.Write(value?.ToString() ?? "");
    public static void WriteLine() => NexusWinForms.UiConsole.WriteLine();
    public static void WriteLine(string value) => NexusWinForms.UiConsole.WriteLine(value);
    public static void WriteLine(object value) => NexusWinForms.UiConsole.WriteLine(value?.ToString() ?? "");
    public static string ReadLine() => NexusWinForms.UiConsole.ReadLine();
    public static ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        string value = NexusWinForms.UiConsole.ReadLine();
        char key = string.IsNullOrEmpty(value) ? '\r' : char.ToUpperInvariant(value[0]);
        return new ConsoleKeyInfo(key, Enum.TryParse(key.ToString(), out ConsoleKey parsed) ? parsed : ConsoleKey.Enter, false, false, false);
    }
}
