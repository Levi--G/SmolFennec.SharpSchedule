using System.Threading;
using System.Threading.Tasks;

namespace SharpShedule
{
    public class AsyncSheduler : SheduleBase
    {
        private bool Stopping = true;

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

        public void Stop()
        {
            Stopping = true;
        }
    }
}