namespace OGS.Core.Platform;

/// <summary>
/// A platform specific timer that allows for precise timing.
/// </summary>
public interface IPreciseTimer : IDisposable
{
    void WaitForNext();
}