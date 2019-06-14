using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace VtuberBot.Core
{
    public class LogHelper
    {
        #region 参数
        public static string LogDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public static string LogFile => Path.Combine(LogDir, $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}.log");

        public static string LogFileError => Path.Combine(LogDir, $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-ERROR.log");

        #endregion

        private static readonly object Lockobj = new object();

        /// <summary>
        /// 写入错误日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="ex"></param>
        public static void Error(string log, bool logToFile = true, Exception ex = null)
        {
            Init();
            lock (Lockobj)
            {
                using (var sw = new StreamWriter(LogFileError, true))
                {
                    var logStr = $"[{DateTime.Now:T}] [Thread:{Thread.CurrentThread.ManagedThreadId}] [ERROR]: {log} " + ((ex == null) ? string.Empty : "\r\n" + ex.ToString());
                    Console.WriteLine(logStr);
                    if (logToFile)
                        sw.WriteLine(logStr);
                }
            }
        }

        /// <summary>
        /// 写入普通日志
        /// </summary>
        /// <param name="log"></param>
        public static void Info(string log, bool logToFile = true)
        {
            Init();
            lock (Lockobj)
            {
                using (var sw = new StreamWriter(LogFile, true))
                {
                    string logStr = $"[{DateTime.Now:T}] [Thread:{Thread.CurrentThread.ManagedThreadId}] [INFO]: {log}";
                    Console.WriteLine(logStr);
                    if (logToFile)
                        sw.WriteLine(logStr);
                }
            }
        }

        public static void Debug(string log, bool logToFile = true, bool outputConsole = true)
        {
            Init();
            lock (Lockobj)
            {
                using (var sw = new StreamWriter(LogFile, true))
                {
                    string logStr = $"[{DateTime.Now:T}] [Thread:{Thread.CurrentThread.ManagedThreadId}] [DEBUG]: {log}";
                    if (outputConsole)
                        Console.WriteLine(logStr);
                    if (logToFile)
                        sw.WriteLine(logStr);
                }
            }
        }



        public static void Init()
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }
        }
    }
}
