using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OGS.Core.Common;

public static class ProcessHelper
{
    private static readonly Log Log = LogManager.GetLogger(typeof(ProcessHelper));
    
    public static bool TryFindOldestProcess(string processName, [NotNullWhen(true)] out Process? process)
    {
        ArgumentNullException.ThrowIfNull(processName);
        
        process = default;
        
        try
        {
            Process[] procs =  Process.GetProcessesByName(processName);
            if (procs.Length == 0)
                return false;
            
            var oldest = procs[0];

            foreach (Process p in procs)
            {
                if (p.StartTime < oldest.StartTime)
                    oldest = p;
            }

            process = oldest;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to find Oldest process named '{processName}'", ex);
            return false;
        }
    }
}