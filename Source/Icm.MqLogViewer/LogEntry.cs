using System;

namespace Icm.MqLogViewer
{
    internal class LogEntry
    {
        public DateTime Date { get; set; }
        public string Level { get; set; }
        public string AppName { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}