using System.IO;
using System.Runtime.InteropServices;

namespace NexusWinForms;

/// <summary>Generates and plays the original ambient NEXUS score with Windows MM audio.</summary>
internal static class NexusSoundtrack
{
    private const uint PlayAsync = 0x0001, PlayLoop = 0x0008, PlayFileName = 0x00020000;
    private static readonly object Gate = new();
    private static string trackPath;
    internal static bool Enabled { get; private set; } = true;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "PlaySoundW")]
    private static extern bool PlaySound(string sound, IntPtr module, uint flags);

    internal static void Start()
    {
        lock (Gate)
        {
            trackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NEXUS", "Audio", "nexus-ambient.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(trackPath)!);
            if (!File.Exists(trackPath)) GenerateOriginalTrack(trackPath);
            Enabled = true;
            PlaySound(trackPath, IntPtr.Zero, PlayAsync | PlayLoop | PlayFileName);
        }
    }

    internal static bool Toggle()
    {
        lock (Gate)
        {
            Enabled = !Enabled;
            if (Enabled && !string.IsNullOrWhiteSpace(trackPath)) PlaySound(trackPath, IntPtr.Zero, PlayAsync | PlayLoop | PlayFileName);
            else PlaySound((string)null, IntPtr.Zero, 0);
            return Enabled;
        }
    }

    internal static void Stop()
    {
        lock (Gate) { Enabled = false; PlaySound((string)null, IntPtr.Zero, 0); }
    }

    private static void GenerateOriginalTrack(string path)
    {
        const int sampleRate = 22050, seconds = 32;
        int samples = sampleRate * seconds;
        int[] roots = [146, 110, 130, 98];
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVE"u8);
        writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(samples * 2);
        for (int i = 0; i < samples; i++)
        {
            double t = i / (double)sampleRate;
            int chord = (int)(t / 4) % roots.Length;
            double beat = t % 2;
            double chordTime = t % 4;
            int root = roots[chord];
            double pad = Math.Sin(Tau * root * t) * .115
                + Math.Sin(Tau * root * 1.5 * t + .2) * .050
                + Math.Sin(Tau * root * 2 * t + .4) * .027
                + Math.Sin(Tau * root * .5 * t) * .055;
            int noteIndex = (int)(t * 2) % 4;
            double notePhase = (t * 2) % 1;
            double noteFrequency = noteIndex switch { 0 => root * 2, 1 => root * 5 / 2, 2 => root * 3, _ => root * 4 };
            double pluck = Math.Sin(Tau * noteFrequency * notePhase) * Math.Exp(-4.5 * notePhase) * .18;
            double pulse = Math.Sin(Tau * (t * 1.5 % 1)) * Math.Exp(-9 * (t * 1.5 % 1)) * .018;
            double swell = .90 + .10 * Math.Sin(Tau * chordTime / 4);
            double edgeFade = Math.Min(1, Math.Min(t / .5, (seconds - t) / .5));
            double value = Math.Clamp((pad + pluck + pulse) * swell * Math.Max(0, edgeFade), -.75, .75);
            writer.Write((short)(value * short.MaxValue));
        }
    }

    private const double Tau = Math.PI * 2;
}

