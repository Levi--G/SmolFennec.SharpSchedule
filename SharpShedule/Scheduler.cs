using System.Threading;

namespace SharpSchedule
{
    public class Scheduler : ScheduleBase
    {
        private Thread Waiter;
        private bool Stopping = true;

        public void Start()
        {
            StopAndBlock();
            Stopping = false;
            Waiter = new Thread(Wait);
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

        public void Stop()
        {
            Stopping = true;
        }

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