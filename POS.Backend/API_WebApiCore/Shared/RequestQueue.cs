using System;
using System.Collections.Generic;
using System.Threading;

namespace TCX.WebApiCore
{
    public class RequestQueue
    {
        private static RequestQueue _instance;
        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly object _lock = new object();
        private readonly Thread _workerThread;
        private bool _isProcessing = false;

        private RequestQueue()
        {
            _workerThread = new Thread(ProcessRequests);
            _workerThread.Start();
        }

        public static RequestQueue Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RequestQueue();
                }
                return _instance;
            }
        }

        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _queue.Enqueue(action);
                if (!_isProcessing)
                {
                    Monitor.Pulse(_lock);
                }
            }
        }

        private void ProcessRequests()
        {
            while (true)
            {
                Action action = null;
                lock (_lock)
                {
                    while (_queue.Count == 0)
                    {
                        _isProcessing = false;
                        Monitor.Wait(_lock);
                    }
                    _isProcessing = true;
                    action = _queue.Dequeue();
                }
                // Execute the action here
                action?.Invoke();
            }
        }
    }
}
