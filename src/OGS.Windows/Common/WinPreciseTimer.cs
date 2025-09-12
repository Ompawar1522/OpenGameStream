using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using OGS.Core.Platform;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Common;

public sealed class WinPreciseTimer : IPreciseTimer
{
    private readonly HighResolutionTimer _timer;
    private readonly TimeSpan _interval;
    private int _count = 0;
    private DateTime _startTime;
    
    public WinPreciseTimer(TimeSpan interval)
    {
        _interval = interval;
        Win32Helpers.SetMinimumTimerResolution();
        _timer = new HighResolutionTimer();
        _timer.SetFutureEventTime(_timer.GetCurrentTime() + _interval);

        _startTime = _timer.GetCurrentTime();
    }

    public void WaitForNext()
    {
        var currentTime = _timer.GetCurrentTime();

        for (; ; )
        {
            var nextFrameTime = _startTime.AddTicks(++_count * _interval.Ticks);
            if (nextFrameTime >= currentTime)
            {
                _timer.SetFutureEventTime(nextFrameTime);
                break;
            }
        }

        _timer.WaitHandle.WaitOne();
    }
    
    public void Dispose()
    {
        _timer.Dispose();
    }
    
    private unsafe class HighResolutionTimer : IDisposable
    {
        public EventWaitHandle WaitHandle { get; }

        private readonly HANDLE _handle;

        public HighResolutionTimer()
        {
            //CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 2
            _handle = CreateWaitableTimerEx(null, null, 2, 0x1F0003);

            if (_handle.Value == default)
                throw new Win32Exception("CreateWaitableTimerEx failed");

            WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset)
            {
                SafeWaitHandle = new SafeWaitHandle(_handle, false)
            };
        }

        public DateTime GetCurrentTime()
        {
            unchecked
            {
                FILETIME ft;
                GetSystemTimePreciseAsFileTime(&ft);
                long ticks = (long)ft.dwHighDateTime << 32 | ft.dwLowDateTime;
                return DateTime.FromFileTimeUtc(ticks);
            }
        }

        public void SetFutureEventTime(DateTime eventTime)
        {
            unchecked
            {
                FILETIME fileTime;
                long ticks = eventTime.ToFileTimeUtc();
                fileTime.dwLowDateTime = (uint)(ticks & 0xFFFFFFFF);
                fileTime.dwHighDateTime = (uint)(ticks >> 32);
                if (!SetWaitableTimer(_handle, (LARGE_INTEGER*)&fileTime, 0, null, null, false))
                    throw new Win32Exception("SetWaitableTimer failed");
            }
        }

        public void CancelFutureEventTime()
        {
            CancelWaitableTimer(_handle);
            
        }

        public void Dispose()
        {
            CancelFutureEventTime();
            WaitHandle.Dispose();
        }
    }
}