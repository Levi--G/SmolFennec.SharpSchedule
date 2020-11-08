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
                    await Task.Delay(Precision, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Stops processing new jobs.
        /// </summary>
        public void Stop()
        {
            Stopping = true;
        }
    }
}