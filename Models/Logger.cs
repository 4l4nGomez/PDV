using System;
using System.IO;

namespace BakeryPOS.Models
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        public static void Log(string message, Exception ex = null)
        {
            try
            {
                lock (_lock)
                {
                    var logPath = Settings.LogFilePath;
                    File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR: {message} {ex?.ToString()}{Environment.NewLine}");
                }
            }
            catch { /* Never let logging break the app */ }
        }

        public static void LogInfo(string message)
        {
            try
            {
                lock (_lock)
                {
                    var logPath = Settings.LogFilePath;
                    File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO: {message}{Environment.NewLine}");
                }
            }
            catch { }
        }
    }
}