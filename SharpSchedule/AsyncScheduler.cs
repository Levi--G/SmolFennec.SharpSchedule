#if NET6_0_OR_GREATER
using DotNext.Threading;
using System;

#endif
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    /// <summary>
    /// A scheduler implementation using tasks allowing automatic sychronisation with the UI thread.
    /// </summary>
    public class AsyncScheduler : ScheduleBase
    {
        private bool Stopping = true;
#if NET6_0_OR_GREATER
        AsyncManualResetEvent NewWorkAvailable = new AsyncManualResetEvent(false);
        public bool InterruptWhenReloading { get; set; } = true;
        public bool UseBetterPrecision { get; set; } = true;
#endif
        /// <summary>
        /// Starts the sheduler and begins processing jobs.
        /// Task returns when the scheduler stops.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task Start(CancellationToken? cancellationToken = null)
        {
            Stopping = false;
            return Wait(cancellationToken ?? CancellationToken.None);
        }

        private async Task Wait(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !Stopping)
            {
                if (!DoSingleCheck())
                {
#if NET6_0_OR_GREATER
                    var delay = Precision;
                    if (UseBetterPrecision)
                    {
                        var i = GetNextItem();
                        if (i != null)
                        {
                            delay = (int)i.NextRun.Subtract(DateTime.Now).TotalMilliseconds - Precision;
                            delay = Math.Min(delay, Precision * 128);
                            delay = Math.Max(delay, Precision / 128);
                        }
                        else
                        {
                            delay = Precision * 128;
                        }
                    }
                    await NewWorkAvailable.WaitAsync(TimeSpan.FromMilliseconds(delay));
                    NewWorkAvailable.Reset();
#else
                    await Task.Delay(Precision, cancellationToken);
#endif
                }
            }
        }

        /// <summary>
        /// Stops processing new jobs.
        /// </summary>
        public void Stop()
        {
            Stopping = true;
#if NET6_0_OR_GREATER
            NewWorkAvailable.Set();
#endif
        }

#if NET6_0_OR_GREATER
        protected override void ReloadScheduleInternal()
        {
            base.ReloadScheduleInternal();
            if (InterruptWhenReloading)
            {
                NewWorkAvailable.Set();
            }
        }
#endif
    }
}