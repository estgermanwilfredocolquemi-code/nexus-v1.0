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
            trackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NEXUS", "Audio", "nexus-ambient-v2.wav");
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
        const int sampleRate = 22050, seconds = 40, channels = 2;
        int samples = sampleRate * seconds;
        int blockAlign = channels * 2;
        int dataBytes = samples * blockAlign;
        int[] roots = [110, 123, 98, 130];
        string temporaryPath = path + ".building";
        using (var stream = File.Create(temporaryPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write("RIFF"u8); writer.Write(36 + dataBytes); writer.Write("WAVE"u8);
            writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)channels);
            writer.Write(sampleRate); writer.Write(sampleRate * blockAlign); writer.Write((short)blockAlign); writer.Write((short)16);
            writer.Write("data"u8); writer.Write(dataBytes);

            int[] melodySteps = [4, 7, 9, 7, 5, 2, 7, 9];
            for (int i = 0; i < samples; i++)
            {
                double t = i / (double)sampleRate;
                int chord = (int)(t / 5) % roots.Length;
                int root = roots[chord];
                double chordTime = t % 5;
                double pad = Math.Sin(Tau * root * .5 * t) * .075
                    + Math.Sin(Tau * root * t) * .080
                    + Math.Sin(Tau * root * 1.25 * t + .14) * .050
                    + Math.Sin(Tau * root * 1.5 * t + .24) * .035
                    + Math.Sin(Tau * root * 2 * t + .38) * .018;
                double beat = (t * 2) % 1;
                int note = melodySteps[(int)(t * 2) % melodySteps.Length];
                double noteFrequency = root * Math.Pow(2, note / 12.0 + 1);
                double pluck = (Math.Sin(Tau * noteFrequency * beat) + .26 * Math.Sin(Tau * noteFrequency * 2 * beat))
                    * Math.Exp(-4.1 * beat) * .115;
                double shimmerBeat = (t % 2);
                double shimmerFreq = root * (chord % 2 == 0 ? 4 : 3);
                double shimmer = Math.Sin(Tau * shimmerFreq * shimmerBeat) * Math.Exp(-2.8 * shimmerBeat) * .045;
                double pulseBeat = t % 1;
                double pulseFreq = 48 + 23 * pulseBeat;
                double pulse = Math.Sin(Tau * pulseFreq * pulseBeat) * Math.Exp(-7 * pulseBeat) * .05;
                double swell = .92 + .08 * Math.Sin(Tau * chordTime / 5);
                double edgeFade = Math.Clamp(Math.Min(t / .8, (seconds - t) / .8), 0, 1);
                double body = (pad + pulse) * swell;
                double left = (body + pluck * .82 + shimmer * .72) * edgeFade;
                double right = (body + pluck * 1.08 + shimmer) * edgeFade;
                writer.Write((short)(Math.Clamp(left, -.7, .7) * short.MaxValue));
                writer.Write((short)(Math.Clamp(right, -.7, .7) * short.MaxValue));
            }
        }
        File.Move(temporaryPath, path, true);
    }

    private const double Tau = Math.PI * 2;
}

