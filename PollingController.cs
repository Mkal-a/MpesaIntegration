using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace MpesaIntegration.Controllers
{
    [ApiController]
    [Route("api/polling")]
    public class PollingController : ControllerBase
    {
        // A basic in-memory store (for sandbox test only)
        public static ConcurrentQueue<string> LatestFeedback = new();

        [HttpGet]
        public IActionResult Get()
        {
            if (LatestFeedback.TryDequeue(out var message))
                return Ok(new { message });

            return Ok(new { message = "No recent M-Pesa feedback yet." });
        }

        // This is called from your STK callback controller
        public static void SaveCallbackMessage(string message)
        {
            LatestFeedback.Enqueue(message);
        }
    }
}
