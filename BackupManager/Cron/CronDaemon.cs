using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BackupManager.Cron
{
    public interface ICronDaemon
    {
        void AddJob(string schedule, ThreadStart action);
        void Start();
        void Stop();
    }

    public class CronDaemon : ICronDaemon, IDisposable
    {
        private readonly System.Timers.Timer timer = new System.Timers.Timer(30000);
        private readonly List<ICronJob> cron_jobs = new List<ICronJob>();
        private DateTime _last = DateTime.Now;

        private readonly ILogger<CronDaemon> _logger;

        public CronDaemon(ILogger<CronDaemon> logger)
        {
            timer.AutoReset = true;
            timer.Elapsed += timer_elapsed;

            _logger = logger;
        }

        public void AddJob(string schedule, ThreadStart action)
        {
            var cj = new CronJob(schedule, action);
            cron_jobs.Add(cj);

            _logger.LogDebug("Add new job. CRON: {0}", schedule);
        }

        public void Start()
        {
            timer.Start();
            _logger.LogDebug("Started cron deamon");
        }

        public void Stop()
        {
            timer.Stop();
            _logger.LogDebug("Stoped cron deamon");

            foreach (CronJob job in cron_jobs)
                job.Abort();
        }

        private void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Minute != _last.Minute)
            {
                _last = DateTime.Now;
                Parallel.ForEach(cron_jobs, x => x.Execute(DateTime.Now));
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
