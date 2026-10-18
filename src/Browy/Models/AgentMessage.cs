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

        private bool _isThinking = false;
        private bool _hasSnapshot = false;
        private string? _personaName;
        private string? _actionDescription;

        public bool IsThinking
        {
            get => _isThinking;
            set
            {
                if (_isThinking != value)
                {
                    _isThinking = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasSnapshot
        {
            get => _hasSnapshot;
            set
            {
                if (_hasSnapshot != value)
                {
                    _hasSnapshot = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? PersonaName
        {
            get => _personaName;
            set
            {
                if (_personaName != value)
                {
                    _personaName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? ActionDescription
        {
            get => _actionDescription;
            set
            {
                if (_actionDescription != value)
                {
                    _actionDescription = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasAction));
                }
            }
        }

        public bool HasAction => !string.IsNullOrWhiteSpace(_actionDescription);

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
