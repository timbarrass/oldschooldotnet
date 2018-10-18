using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OldSchoolDotNet
{
    class ExampleApp
    {
        private static CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private static ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

        static void Main(string[] args)
        {
            var preamble = new List<string>
            {
                "Hello there ... ctrl-c to quit",
                "------------------------------"
            };

            SimpleSplitScreenConsole.InitConsole(preamble, OnShutDown);

            var cancellationToken = _tokenSource.Token;

            var tasks = new Task[]
            {
                Task.Factory.StartNew(Consume, cancellationToken),
                Task.Factory.StartNew(Produce, cancellationToken)
            };

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException aex)
            {
                SimpleSplitScreenConsole.OnResponse("Exception thrown: "+ aex.Message);
            }
        }

        private static void OnShutDown()
        {
            _tokenSource.Cancel();
        }

        private static void Produce()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                var message = SimpleSplitScreenConsole.FetchInput();

                if (message != "")
                {
                    _messages.Enqueue(message);
                }
            }
        }

        private static void Consume()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                while (_messages.Count() > 0)
                {
                    if (_messages.TryDequeue(out string message))
                    {
                        SimpleSplitScreenConsole.OnResponse(message);
                    }
                }

                Thread.Sleep(500);
            }
        }
    }
}
