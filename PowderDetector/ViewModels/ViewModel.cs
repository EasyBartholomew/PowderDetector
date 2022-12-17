using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PowderDetector.ViewModels
{
    public abstract class ViewModel : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaiseOnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // ReSharper disable once PossibleNullReferenceException
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected ViewModel()
        {
            PropertyChanged += delegate { };
        }
    }
}