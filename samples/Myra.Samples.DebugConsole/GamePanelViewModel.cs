using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Myra.Samples.DebugConsole
{
    public sealed class GamePanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// This method is called by the Set accessor of each property.
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _buttonsEnabled = true;
        public bool ButtonsEnabled {
            get => _buttonsEnabled;
            set
            {
                if (_buttonsEnabled != value)
                {
                    _buttonsEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
