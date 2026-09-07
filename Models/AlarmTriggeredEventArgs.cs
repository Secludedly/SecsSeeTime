using System;

namespace SecSeeTime.Models
{
    public class AlarmTriggeredEventArgs : EventArgs
    {
        public AlarmTriggeredEventArgs(Alarm alarm)
        {
            Alarm = alarm;
            TriggeredAt = DateTime.Now;
        }


        public Alarm Alarm { get; }


        public DateTime TriggeredAt { get; }
    }
}