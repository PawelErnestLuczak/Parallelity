using System;
using System.ComponentModel;

namespace Parallelity.Logs
{
    class LogController
    {
        public static readonly BindingList<Log> logs = new BindingList<Log>();

        public static void add(LogType type, String text)
        {
            logs.Add(new Log(type, text));
        }
    }
}
