using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class Ping
    {
        private int _id;
        private DateTime _startTime;
        private TaskCompletionSource<TimeSpan> _tcs;

        public Ping(int id)
        {
            _id = id;
            _startTime = DateTime.Now;
            _tcs = new TaskCompletionSource<TimeSpan>();
        }

        public int Id
        {
            get { return _id; }
        }

        public Task<TimeSpan> Task
        {
            get { return _tcs.Task; }
        }

        public void Complete()
        {
            TimeSpan elapsed = DateTime.Now - _startTime;
            // Invoke the callback async.
            System.Threading.Tasks.Task.Run(() => _tcs.TrySetResult(elapsed));
        }

        public void Cancel()
        {
            _tcs.TrySetCanceled();
        }
    }
}
