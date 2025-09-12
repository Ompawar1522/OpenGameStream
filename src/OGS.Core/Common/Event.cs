namespace OGS.Core.Common;

/// <summary>
/// Very basic thread-safe replacement for the built-in C# 'event'
/// </summary>
public class Event
{
    private Action? _handler;

    public void Raise()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        Action? handler = _handler;

        if (handler is not null)
            handler();
    }

    public void Subscribe(Action handler)
    {
        lock (this)
        {
            _handler += handler;
        }
    }

    public void Unsubscribe(Action handler)
    {
        lock (this)
        {
            _handler -= handler;
        }
    }
}

/// <summary>
/// Very basic thread-safe replacement for the built-in C# 'event'
/// </summary>
public class Event<T>
    where T : allows ref struct
{
    private Action<T?>? _handler;

    public void Raise(T? value)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        Action<T?>? handler = _handler;

        if (handler is not null)
            handler(value);
    }

    public void Subscribe(Action<T> handler)
    {
        lock (this)
        {
            _handler += handler;
        }
    }

    public void Unsubscribe(Action<T> handler)
    {
        lock (this)
        {
            _handler -= handler;
        }
    }
}

/// <summary>
/// Very basic thread-safe replacement for the built-in C# 'event'
/// </summary>
public class Event<T1, T2>
    where T1 : allows ref struct
    where T2 : allows ref struct
{
    private Action<T1?, T2?>? _handler;

    public void Raise(T1? value1, T2 value2)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var handler = _handler;

        if (handler is not null)
            handler(value1, value2);
    }

    public void Subscribe(Action<T1, T2> handler)
    {
        lock (this)
        {
            _handler += handler;
        }
    }

    public void Unsubscribe(Action<T1, T2> handler)
    {
        lock (this)
        {
            _handler -= handler;
        }
    }
}