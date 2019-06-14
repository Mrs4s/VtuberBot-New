using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VtuberBot.Core.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> Retry<T>(this Task<T> task, int macCount, int delay = 100, Action<Exception> handle = null)
        {
            var count = 0;
            Exception lastException = null;
            while (count++ < macCount)
            {
                try
                {
                    return await task;
                }
                catch (Exception e)
                {
                    lastException = e;
                    LogHelper.Error("Retry: got exception.", ex: e);
                }

                await Task.Delay(delay);
            }
            handle?.Invoke(lastException);
            return default(T);
        }

        public static async Task Retry(this Task task, int macCount, int delay = 100, Action<Exception> handle = null)
        {
            var count = 0;
            Exception lastException = null;
            while (count++ < macCount)
            {
                try
                {
                    await task;
                    return;
                }
                catch (Exception e)
                {
                    lastException = e;
                }

                await Task.Delay(delay);
            }
            handle?.Invoke(lastException);
        }
    }
}
