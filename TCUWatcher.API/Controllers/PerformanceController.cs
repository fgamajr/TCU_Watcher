using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace TCUWatcher.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PerformanceController : ControllerBase
    {
        [HttpGet("performance")]
        [AllowAnonymous]
        public IActionResult GetPerformanceSnapshot()
        {
            var process = Process.GetCurrentProcess();

            var metrics = new
            {
                UtcNow = DateTime.UtcNow,
                CpuTimeMs = process.TotalProcessorTime.TotalMilliseconds,
                MemoryUsedMb = process.WorkingSet64 / 1024 / 1024,
                ThreadCount = process.Threads.Count,
                GcGen0 = GC.CollectionCount(0),
                GcGen1 = GC.CollectionCount(1),
                GcGen2 = GC.CollectionCount(2)
            };

            return Ok(metrics);
        }
    }
}
