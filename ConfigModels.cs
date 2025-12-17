using System;
using System.Collections.Generic;

namespace BeepScheduler
{
    public class AppConfig
    {
        public List<DaySchedule> Schedules { get; set; } = new List<DaySchedule>();
    }

    public class DaySchedule
    {
        public List<DayOfWeek> Days { get; set; } = new List<DayOfWeek>();
        public string Times { get; set; } = "12:00"; // Semicolon separated: "08:00; 14:30"
        public AudioSettings Audio { get; set; } = new AudioSettings();
    }

    public class AudioSettings
    {
        public bool UseCustomSound { get; set; } = false;
        public string CustomSoundPath { get; set; } = "";
        public int Volume { get; set; } = 100; // 0 to 100
    }
}
