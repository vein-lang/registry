namespace core.services;

/// <summary>
/// A wrapper that allows for unit tests related to system time.
/// </summary>
public class SystemTime
{
    public virtual DateTime UtcNow => DateTime.UtcNow;
}
