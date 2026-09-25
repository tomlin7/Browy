using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Browy.Models
{
    public class AgentMessage : INotifyPropertyChanged
    {
        private string _sender;
        private string _content;
        private DateTime _timestamp = DateTime.Now;

        public string Sender
        {
            get => _sender;
            set
            {
                if (_sender != value)
                {
                    _sender = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsUser));
                    OnPropertyChanged(nameof(IsAgent));
                }
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedTime));
                }
            }
        }

        public bool IsUser => string.Equals(Sender, "User", StringComparison.OrdinalIgnoreCase);
        public bool IsAgent => !IsUser;

        public string FormattedTime => Timestamp.ToString("HH:mm");

        public AgentMessage(string sender, string content)
        {
            _sender = sender;
            _content = content;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
