namespace PowderDetector.Models
{
    public class ObservableEntity<T> : ObservableObject
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (object.Equals(value, _value))
                    return;

                _value = value;
                OnPropertyChanged();
            }
        }

        public ObservableEntity(T value = default)
        {
            Value = value;
        }
    }
}