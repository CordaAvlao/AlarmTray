using System;
using System.Collections.Generic;
using System.IO;

namespace BeepScheduler
{
    public enum SoundType
    {
        StandardBeep,
        CasioChime,
        Siren,
        DigitalAlarm
    }

    public static class SoundHelper
    {
        public static string GetSoundPath(SoundType type)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            return Path.Combine(folder, type.ToString() + ".wav");
        }

        public static void InitializeIntegratedSounds()
        {
            foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
            {
                string path = GetSoundPath(type);
                if (!File.Exists(path))
                {
                    GenerateWav(path, type);
                }
            }
        }

        private static void GenerateWav(string filePath, SoundType type)
        {
            int sampleRate = 44100;
            short channels = 1;
            short bitsPerSample = 16;
            
            List<short> samples = new List<short>();

            // Generation logic based on type
            switch (type)
            {
                case SoundType.StandardBeep:
                    AddTone(samples, 1000, 1.0, sampleRate);
                    break;
                    
                case SoundType.CasioChime:
                    AddTone(samples, 2000, 0.08, sampleRate);
                    AddSilence(samples, 0.05, sampleRate);
                    AddTone(samples, 2000, 0.08, sampleRate);
                    break;

                case SoundType.Siren:
                    AddSweep(samples, 400, 1500, 1.0, sampleRate);
                    AddSweep(samples, 1500, 400, 1.0, sampleRate);
                    break;

                case SoundType.DigitalAlarm:
                    for (int i = 0; i < 4; i++)
                    {
                        AddTone(samples, 1500, 0.1, sampleRate);
                        AddSilence(samples, 0.1, sampleRate);
                    }
                    break;
            }

            // Write WAV File
            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int dataLength = samples.Count * bitsPerSample / 8;

                // RIFF header
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataLength);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16); // chunk size
                bw.Write((short)1); // PCM
                bw.Write(channels);
                bw.Write(sampleRate);
                bw.Write(sampleRate * channels * bitsPerSample / 8); // byte rate
                bw.Write((short)(channels * bitsPerSample / 8)); // block align
                bw.Write(bitsPerSample);

                // data chunk
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(dataLength);

                foreach (var sample in samples)
                {
                    bw.Write(sample);
                }
            }
        }

        private static void AddTone(List<short> samples, double frequency, double durationSeconds, int sampleRate)
        {
            int samplesCount = (int)(sampleRate * durationSeconds);
            double amplitude = 16000; // Moderate volume to avoid clipping

            for (int i = 0; i < samplesCount; i++)
            {
                double time = (double)i / sampleRate;
                short sample = (short)(amplitude * Math.Sin(2 * Math.PI * frequency * time));
                samples.Add(sample);
            }
        }

        private static void AddSilence(List<short> samples, double durationSeconds, int sampleRate)
        {
            int samplesCount = (int)(sampleRate * durationSeconds);
            for (int i = 0; i < samplesCount; i++)
            {
                samples.Add(0);
            }
        }

        private static void AddSweep(List<short> samples, double startFreq, double endFreq, double durationSeconds, int sampleRate)
        {
            int samplesCount = (int)(sampleRate * durationSeconds);
            double amplitude = 16000;

            for (int i = 0; i < samplesCount; i++)
            {
                double time = (double)i / sampleRate;
                double currentFreq = startFreq + (endFreq - startFreq) * ((double)i / samplesCount);
                // Use phase accumulation for smooth frequency sweep
                // Simplified here: result is acceptable for short beeps
                short sample = (short)(amplitude * Math.Sin(2 * Math.PI * currentFreq * time));
                samples.Add(sample);
            }
        }
    }
}
