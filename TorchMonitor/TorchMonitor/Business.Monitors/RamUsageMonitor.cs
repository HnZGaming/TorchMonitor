using System;
using System.Diagnostics;
using TorchDatabaseIntegration.InfluxDB;

namespace TorchMonitor.Business.Monitors
{
    public class RamUsageMonitor : IIntervalListener
    {
        readonly Process _process;

        public RamUsageMonitor()
        {
            _process = Process.GetCurrentProcess();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 20) return;
            if (intervalsSinceStart % 10 != 0) return;

            const long GB = 1024 * 1024 * 1024;

            _process.Refresh();

            // https://docs.microsoft.com/en-us/dotnet/api/system.gc.gettotalmemory
            // [...] the best available approximation of the number of bytes currently allocated in managed memory.
            var heap = (float) GC.GetTotalMemory(false) / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.privatememorysize64
            //  [...] the current size of memory used by the process, in bytes, that cannot be shared with other processes.
            var privat = (float) _process.PrivateMemorySize64 / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.workingset64
            // [...] the current size of working set memory used by the process.
            // [...] the set of memory pages currently visible to the process in physical RAM memory.
            // [...] includes both shared and private data.
            var workingSet = (float) _process.WorkingSet64 / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.nonpagedsystemmemorysize64
            // [...]  the current size of nonpaged system memory used by the process.
            var nonPagedSys = (float) _process.NonpagedSystemMemorySize64 / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.pagedsystemmemorysize64
            // [...] the current size of pageable system memory used by the process.
            var pagedSys = (float) _process.PagedSystemMemorySize64 / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.pagedmemorysize64
            // [...] the current size of memory in the virtual memory paging file used by the process.
            var pagedVirtual = (float) _process.PagedMemorySize64 / GB;

            // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.virtualmemorysize64
            // [...] the current size of virtual memory used by the process.
            var virt = (float) _process.VirtualMemorySize64 / GB;

            InfluxDbPointFactory
                .Measurement("resource")
                .Field("heap", heap)
                .Field("private", privat)
                .Field("working_set", workingSet)
                .Field("non_paged_sys", nonPagedSys)
                .Field("paged_sys", pagedSys)
                .Field("paged_virtual", pagedVirtual)
                .Field("virtual", virt)
                .Write();
        }
    }
}