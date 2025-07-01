using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace MpesaIntegration.Controllers
{
    [ApiController]
    [Route("api/stkcallback")]
    public class StkCallbackController : ControllerBase
    {
        // In-memory status storage (thread-safe)
        private static ConcurrentDictionary<string, string> PaymentStatuses = new();

        [HttpPost]
        public IActionResult Post([FromBody] JObject callbackData)
        {
            Console.WriteLine("✅ STK Callback Received:");
            Console.WriteLine(callbackData.ToString());

            var body = callbackData["Body"]?["stkCallback"];
            var resultCode = body?["ResultCode"]?.ToString();
            var resultDesc = body?["ResultDesc"]?.ToString();
            var metadataItems = body?["CallbackMetadata"]?["Item"] as JArray;

            // Extract phone number (if available)
            string? phone = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "PhoneNumber")?["Value"]?.ToString();

            string? mpesaReceipt = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "MpesaReceiptNumber")?["Value"]?.ToString();

            string message = $"Result: {resultDesc}, Receipt: {mpesaReceipt}";

            // Save using phone as key if available
            if (!string.IsNullOrEmpty(phone))
            {
                PaymentStatuses[phone] = message;
            }

            return Ok(); // Always send 200 OK to Safaricom
        }

        // Polling endpoint for desktop client
        [HttpGet("/api/paymentstatus")]
        public IActionResult GetStatus([FromQuery] string phone)
        {
            if (PaymentStatuses.TryGetValue(phone, out string? status))
            {
                return Ok(new { phone, status });
            }

            return NotFound(new { message = "No status found for this number yet." });
        }
    }
}
