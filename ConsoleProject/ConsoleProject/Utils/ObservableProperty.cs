

public class ObservableProperty<T> where T : struct
{
    private T _value;

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged?.Invoke(value);
        }
    }

    public event Action<T> OnValueChanged;

    public ObservableProperty(T value = default)
    {
        _value = value;
    }

    public void AddListener(Action<T> action)
    {
        OnValueChanged += action;
    }

    public void RemoveListener(Action<T> action)
    {
        OnValueChanged -= action;
    }

    public void RemoveListenerAll()
    {
        OnValueChanged = null;
    }
}