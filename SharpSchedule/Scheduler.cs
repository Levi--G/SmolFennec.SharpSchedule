using System.Threading;

namespace SharpSchedule
{
    /// <summary>
    /// A general purpose scheduler with a single backing thread.
    /// </summary>
    public class Scheduler : ScheduleBase
    {
        private Thread Waiter;
        private bool Stopping = true;

        /// <summary>
        /// Starts the scheduler and begins processing jobs.
        /// </summary>
#if NETSTANDARD2_0
        public void Start(ApartmentState apartmentState = ApartmentState.MTA)
#else

        public void Start()
#endif
        {
            StopAndBlock();
            Stopping = false;
            Waiter = new Thread(Wait);
#if NETSTANDARD2_0
            Waiter.SetApartmentState(apartmentState);
#endif
            Waiter.Start();
        }

        private void Wait()
        {
            while (!Stopping)
            {
                if (!DoSingleCheck())
                {
                    Thread.Sleep(Precision);
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

        /// <summary>
        /// Stops processing new jobs and wait until the last one is finished.
        /// </summary>
        public void StopAndBlock()
        {
            Stopping = true;
            if (Waiter != null && Waiter.IsAlive)
            {
                Waiter.Join();
            }
            Waiter = null;
        }
    }
}