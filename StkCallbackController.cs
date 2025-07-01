using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Linq;

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
            var resultDesc = body?["ResultDesc"]?.ToString();
            var metadataItems = body?["CallbackMetadata"]?["Item"] as JArray;

            // Extract metadata
            string? phone = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "PhoneNumber")?["Value"]?.ToString();

            string? mpesaReceipt = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "MpesaReceiptNumber")?["Value"]?.ToString();

            string? firstName = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "FirstName")?["Value"]?.ToString();

            string? lastName = metadataItems?
                .FirstOrDefault(i => i["Name"]?.ToString() == "LastName")?["Value"]?.ToString();

            string fullName = $"{firstName} {lastName}".Trim();

            string message = $"Payment Successful!\n" +
                             $"Name: {fullName}\n" +
                             $"Phone: {phone}\n" +
                             $"Receipt: {mpesaReceipt}\n" +
                             $"Status: {resultDesc}";

            // Save using phone as key if available
            if (!string.IsNullOrEmpty(phone))
            {
                PaymentStatuses[phone] = message;

                // ✅ Save to polling queue for WinForms polling
                PollingController.SaveCallbackMessage(message);
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
