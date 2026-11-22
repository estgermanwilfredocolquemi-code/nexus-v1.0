using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NexusWinForms;

// Voz local opcional mediante Microsoft SAPI instalado en Windows; no usa red ni claves.
internal static class IrisSpeechService
{
    private static readonly BlockingCollection<string> Queue = new();
    private static readonly Thread Worker;
    private static volatile bool enabled;

    static IrisSpeechService()
    {
        Worker = new Thread(SpeechLoop) { IsBackground = true, Name = "NEXUS IRIS speech" };
        Worker.SetApartmentState(ApartmentState.STA);
        Worker.Start();
    }

    internal static bool Enabled => enabled;

    internal static bool Toggle() => enabled = !enabled;

    internal static void Speak(string text)
    {
        if (!enabled || string.IsNullOrWhiteSpace(text)) return;
        try { Queue.Add(text); } catch (InvalidOperationException) { }
    }

    private static void SpeechLoop()
    {
        object voice = null;
        try
        {
            Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice", throwOnError: false);
            if (voiceType == null) return;
            voice = Activator.CreateInstance(voiceType);
            foreach (string message in Queue.GetConsumingEnumerable())
                voiceType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, [message, 0]);
        }
        catch
        {
            // Si Windows no tiene SAPI o una voz disponible, la salida textual de IRIS sigue funcionando.
        }
        finally
        {
            if (voice != null && Marshal.IsComObject(voice)) Marshal.FinalReleaseComObject(voice);
        }
    }
}
