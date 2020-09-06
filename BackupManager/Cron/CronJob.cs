using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BackupManager.Cron
{
    public interface ICronJob
    {
        void Execute(DateTime date_time);
        void Abort();
    }

    public class CronJob : ICronJob
    {
        private readonly ICronSchedule _cron_schedule = new CronSchedule();
        private readonly ThreadStart _thread_start;
        private Thread _thread;

        public CronJob(string schedule, ThreadStart thread_start)
        {
            _cron_schedule = new CronSchedule(schedule);
            _thread_start = thread_start;
            _thread = new Thread(thread_start);
        }

        private object _lock = new object();
        public void Execute(DateTime date_time)
        {
            lock (_lock)
            {
                if (!_cron_schedule.IsTime(date_time))
                    return;

                if (_thread.ThreadState == ThreadState.Running)
                    return;

                _thread = new Thread(_thread_start);
                _thread.Start();
            }
        }

        public void Abort()
        {
            _thread.Abort();
        }

    }
}
