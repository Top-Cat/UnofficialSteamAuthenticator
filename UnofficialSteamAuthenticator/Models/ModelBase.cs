using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnofficialSteamAuthenticator.Annotations;

namespace UnofficialSteamAuthenticator.Models
{
    public class ModelBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged()
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }

        #endregion INotifyPropertyChanged
    }
}
