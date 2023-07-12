namespace MapMaven.RankedMapUpdater
{
    public class TimerInfo
    {
        public ScheduleStatus ScheduleStatus { get; set; }
        public bool IsPastDue { get; set; }
    }

    public class ScheduleStatus
    {
        public DateTime Last { get; set; }
        public DateTime Next { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
