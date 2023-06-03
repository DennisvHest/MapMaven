namespace MapMaven.Core.Models
{
    public class ErrorEvent
    {
        public string Message { get; set; }
        public Exception? Exception { get; set; }
    }
}