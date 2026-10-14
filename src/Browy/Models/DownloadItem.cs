using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace Browy.Models
{
    public class DownloadItem : INotifyPropertyChanged
    {
        public CoreWebView2DownloadOperation? Operation { get; }

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private string _resultFilePath = string.Empty;
        public string ResultFilePath
        {
            get => _resultFilePath;
            set => SetProperty(ref _resultFilePath, value);
        }

        private long _bytesReceived;
        public long BytesReceived
        {
            get => _bytesReceived;
            set
            {
                if (SetProperty(ref _bytesReceived, value))
                {
                    UpdateProgress();
                }
            }
        }

        private long? _totalBytes;
        public long? TotalBytes
        {
            get => _totalBytes;
            set
            {
                if (SetProperty(ref _totalBytes, value))
                {
                    UpdateProgress();
                }
            }
        }

        private double _percentComplete;
        public double PercentComplete
        {
            get => _percentComplete;
            set => SetProperty(ref _percentComplete, value);
        }

        private string _speedText = string.Empty;
        public string SpeedText
        {
            get => _speedText;
            set => SetProperty(ref _speedText, value);
        }

        private string _etaText = string.Empty;
        public string EtaText
        {
            get => _etaText;
            set => SetProperty(ref _etaText, value);
        }

        private string _statusText = "Starting...";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private CoreWebView2DownloadState _state;
        public CoreWebView2DownloadState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(IsInProgress));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(IsFailedOrCancelled));
                    OnPropertyChanged(nameof(CanPause));
                    OnPropertyChanged(nameof(IsPaused));
                }
            }
        }

        public bool IsInProgress => State == CoreWebView2DownloadState.InProgress;
        public bool IsCompleted => State == CoreWebView2DownloadState.Completed;
        public bool IsFailedOrCancelled => State == CoreWebView2DownloadState.Interrupted && !IsPaused;
        public bool CanPause => Operation?.CanResume == true || IsInProgress;
        public bool IsPaused => State == CoreWebView2DownloadState.Interrupted && Operation?.CanResume == true;

        public string IconGlyph
        {
            get
            {
                string ext = Path.GetExtension(ResultFilePath).ToLowerInvariant();
                return ext switch
                {
                    ".exe" or ".msi" => "\uE7B5",
                    ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "\uF012",
                    ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" => "\uEB9F",
                    ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" => "\uE714",
                    ".mp3" or ".wav" or ".flac" or ".aac" => "\uE8D6",
                    ".pdf" or ".doc" or ".docx" or ".txt" or ".md" => "\uE8A5",
                    _ => "\uE896"
                };
            }
        }

        private long _lastBytes;
        private DateTime _lastTime = DateTime.UtcNow;

        public DownloadItem()
        {
        }

        public DownloadItem(CoreWebView2DownloadOperation operation)
        {
            Operation = operation;
            ResultFilePath = operation.ResultFilePath;
            FileName = Path.GetFileName(operation.ResultFilePath);
            if (string.IsNullOrWhiteSpace(FileName))
            {
                FileName = "download";
            }
            TotalBytes = (long?)operation.TotalBytesToReceive;
            BytesReceived = operation.BytesReceived;
            State = operation.State;
            _lastBytes = BytesReceived;
            _lastTime = DateTime.UtcNow;

            UpdateProgress();

            operation.BytesReceivedChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    long currentBytes = operation.BytesReceived;
                    DateTime now = DateTime.UtcNow;
                    double elapsed = (now - _lastTime).TotalSeconds;

                    if (elapsed >= 0.5)
                    {
                        long bytesDiff = currentBytes - _lastBytes;
                        double speedBps = bytesDiff / elapsed;
                        if (speedBps > 0)
                        {
                            SpeedText = $"{FormatBytes((long)speedBps)}/s";

                            if (TotalBytes.HasValue && TotalBytes.Value > currentBytes)
                            {
                                long remainingBytes = TotalBytes.Value - currentBytes;
                                double secondsLeft = remainingBytes / speedBps;
                                EtaText = secondsLeft < 60
                                    ? $"{Math.Ceiling(secondsLeft)}s remaining"
                                    : $"{TimeSpan.FromSeconds(secondsLeft):m\\:ss} remaining";
                            }
                        }

                        _lastBytes = currentBytes;
                        _lastTime = now;
                    }

                    BytesReceived = currentBytes;
                    TotalBytes = (long?)operation.TotalBytesToReceive;
                    UpdateProgress();
                });
            };

            operation.StateChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    State = operation.State;
                    UpdateProgress();
                });
            };
        }

        public void UpdateProgress()
        {
            if (State == CoreWebView2DownloadState.Completed)
            {
                PercentComplete = 100;
                SpeedText = string.Empty;
                EtaText = string.Empty;
                StatusText = $"{FormatBytes(BytesReceived)} · Completed";
                return;
            }

            if (IsPaused)
            {
                StatusText = $"Paused · {FormatBytes(BytesReceived)}";
                SpeedText = string.Empty;
                EtaText = string.Empty;
                return;
            }

            if (State == CoreWebView2DownloadState.Interrupted)
            {
                StatusText = $"Cancelled / Interrupted";
                SpeedText = string.Empty;
                EtaText = string.Empty;
                return;
            }

            if (TotalBytes.HasValue && TotalBytes.Value > 0)
            {
                PercentComplete = Math.Clamp((double)BytesReceived / TotalBytes.Value * 100.0, 0, 100);
                string speedPart = !string.IsNullOrEmpty(SpeedText) ? $" • {SpeedText}" : "";
                string etaPart = !string.IsNullOrEmpty(EtaText) ? $" • {EtaText}" : "";
                StatusText = $"{FormatBytes(BytesReceived)} of {FormatBytes(TotalBytes.Value)} ({PercentComplete:0}%){speedPart}{etaPart}";
            }
            else
            {
                PercentComplete = 0;
                string speedPart = !string.IsNullOrEmpty(SpeedText) ? $" • {SpeedText}" : "";
                StatusText = $"{FormatBytes(BytesReceived)} downloaded{speedPart}";
            }
        }

        public void Pause()
        {
            try
            {
                if (CanPause && IsInProgress)
                {
                    Operation?.Pause();
                    UpdateProgress();
                }
            }
            catch { }
        }

        public void Resume()
        {
            try
            {
                if (CanPause)
                {
                    Operation?.Resume();
                    UpdateProgress();
                }
            }
            catch { }
        }

        public void Cancel()
        {
            try
            {
                if (IsInProgress || IsPaused)
                {
                    Operation?.Cancel();
                }
            }
            catch { }
        }

        public void OpenFile()
        {
            try
            {
                if (File.Exists(ResultFilePath))
                {
                    Process.Start(new ProcessStartInfo(ResultFilePath) { UseShellExecute = true });
                }
            }
            catch { }
        }

        public void OpenFolder()
        {
            try
            {
                if (File.Exists(ResultFilePath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{ResultFilePath}\"") { UseShellExecute = true });
                }
                else
                {
                    string? dir = Path.GetDirectoryName(ResultFilePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                    }
                }
            }
            catch { }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
