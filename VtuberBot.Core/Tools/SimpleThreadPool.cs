using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VtuberBot.Core.Tools
{
    public class SimpleThreadPool
    {
        public int CompletedAction { get; private set; }

        public int MaxThread { get; set; } = 3;

        public Queue<Action> Actions { get; } = new Queue<Action>();



        public void Run()
        {
            var maxThread = Math.Min(Actions.Count, MaxThread);
            var count = Actions.Count;
            var waitEvent = new ManualResetEvent(false);

            for (var i = 0; i < maxThread; i++)
            {
                var action = Actions.Dequeue();
                new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            action?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error("Thread error.", ex: ex);
                        }

                        CompletedAction++;
                        if (Actions.Count == 0)
                        {
                            waitEvent.Set();
                            break;
                        }
                        else
                        {
                            action = Actions.Dequeue();
                        }
                    }
                }).Start();
            }
            waitEvent.WaitOne(1000 * 60);

        }

    }
}
