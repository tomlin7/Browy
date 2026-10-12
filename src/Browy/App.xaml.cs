using System;
using System.IO;
using System.Windows;

namespace Browy
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.WriteAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] AppDomain: {e.ExceptionObject}\n");
                }
                catch { }
            };
            DispatcherUnhandledException += (s, e) =>
            {
                try
                {
                    File.WriteAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] Dispatcher: {e.Exception}\n");
                }
                catch { }
            };
        }
    }
}
