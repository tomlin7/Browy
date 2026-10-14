using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Browy.Services
{
    public class ToastNotificationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Origin { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Action? OnClick { get; set; }
    }

    public static class NotificationService
    {
        public static event Action<ToastNotificationItem>? ToastReceived;

        public static bool IsOriginAllowed(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin)) return true;
            if (BrowserSettings.Current.BlockedNotificationOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return false;
            return true;
        }

        public static void SetOriginPermission(string origin, bool allowed)
        {
            if (string.IsNullOrWhiteSpace(origin)) return;

            BrowserSettings.Current.AllowedNotificationOrigins.RemoveAll(o => o.Equals(origin, StringComparison.OrdinalIgnoreCase));
            BrowserSettings.Current.BlockedNotificationOrigins.RemoveAll(o => o.Equals(origin, StringComparison.OrdinalIgnoreCase));

            if (allowed)
            {
                BrowserSettings.Current.AllowedNotificationOrigins.Add(origin);
            }
            else
            {
                BrowserSettings.Current.BlockedNotificationOrigins.Add(origin);
            }

            BrowserSettings.Save();
        }

        public static void ShowToast(string title, string message, string? origin = null, Action? onClick = null)
        {
            if (!string.IsNullOrWhiteSpace(origin) && !IsOriginAllowed(origin))
            {
                return;
            }

            var item = new ToastNotificationItem
            {
                Title = title,
                Message = message,
                Origin = origin,
                OnClick = onClick
            };

            // 1. Fire in-app listener
            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    ToastReceived?.Invoke(item);
                });
            }
            catch { }

            // 2. Dispatch native Windows toast in background
            Task.Run(() =>
            {
                try
                {
                    DispatchWindowsToast(title, message);
                }
                catch { }
            });
        }

        private static void DispatchWindowsToast(string title, string message)
        {
            string safeTitle = title.Replace("\"", "`\"").Replace("'", "''");
            string safeMsg = message.Replace("\"", "`\"").Replace("'", "''");

            string script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
$textNodes = $template.GetElementsByTagName('text')
$textNodes.Item(0).AppendChild($template.CreateTextNode('{safeTitle}')) > $null
$textNodes.Item(1).AppendChild($template.CreateTextNode('{safeMsg}')) > $null
$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Browy')
$notification = [Windows.UI.Notifications.ToastNotification]::new($template)
$notifier.Show($notification)
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{script.Replace("\r\n", " ").Replace("\n", " ")}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
    }
}
