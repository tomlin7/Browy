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
        public CoreWebView2DownloadOperation Operation { get; }

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
                }
            }
        }

        public bool IsInProgress => State == CoreWebView2DownloadState.InProgress;
        public bool IsCompleted => State == CoreWebView2DownloadState.Completed;
        public bool IsFailedOrCancelled => State == CoreWebView2DownloadState.Interrupted;

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
            UpdateProgress();

            operation.BytesReceivedChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    BytesReceived = operation.BytesReceived;
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

        private void UpdateProgress()
        {
            if (State == CoreWebView2DownloadState.Completed)
            {
                PercentComplete = 100;
                StatusText = $"{FormatBytes(BytesReceived)} · Completed";
                return;
            }

            if (State == CoreWebView2DownloadState.Interrupted)
            {
                StatusText = $"Cancelled / Interrupted";
                return;
            }

            if (TotalBytes.HasValue && TotalBytes.Value > 0)
            {
                PercentComplete = Math.Clamp((double)BytesReceived / TotalBytes.Value * 100.0, 0, 100);
                StatusText = $"{FormatBytes(BytesReceived)} of {FormatBytes(TotalBytes.Value)} ({PercentComplete:0}%)";
            }
            else
            {
                PercentComplete = 0;
                StatusText = $"{FormatBytes(BytesReceived)} downloaded";
            }
        }

        public void Cancel()
        {
            try
            {
                if (IsInProgress)
                {
                    Operation.Cancel();
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

        private static string FormatBytes(long bytes)
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
