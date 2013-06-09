using System;

namespace Parallelity.Logs
{
    enum LogType
    {
        Information,
        Verbose,
        Debug,
        Warning,
        Error
    }

    class Log
    {
        public LogType type { get; private set; }
        public String text { get; private set; }
        public DateTime time { get; private set; }

        public Log(LogType type, String text)
        {
            this.type = type;
            this.text = text;
            this.time = DateTime.Now;
        }
    }
}
