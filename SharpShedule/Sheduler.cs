using System.Threading;

namespace SharpShedule
{
    public class Sheduler : SheduleBase
    {
        private Thread Waiter;
        private bool Stopping = true;

        public void Start()
        {
            Stop();
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
            if (Waiter != null && Waiter.IsAlive)
            {
                Waiter.Join();
            }
            Waiter = null;
        }
    }
}