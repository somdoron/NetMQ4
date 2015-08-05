using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Util
{
    class Clock
    {
        static Clock()
        {
            if (Stopwatch.IsHighResolution)
            {
                // We want the equivalant of 1 ms
                MaxCommandDelay = Stopwatch.Frequency/1000;
            }            
        }

        public static long MaxCommandDelay { get; private set; }

        public static long GetTimestamp()
        {
            if (Stopwatch.IsHighResolution)
                return Stopwatch.GetTimestamp();

            return 0;            
        }
    }
}
