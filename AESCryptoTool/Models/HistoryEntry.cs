using System;

namespace AESCryptoTool.Models
{
    public class HistoryEntry : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Operation { get; set; } = string.Empty; // "encrypt" or "decrypt"
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        
        private bool _isFavorite;
        public bool IsFavorite 
        { 
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(nameof(IsFavorite)); }
        }

        private bool _isSelected;
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSelected 
        { 
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}



