using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient; // ✅ Your preferred modern driver
using System.Linq;

namespace MpesaIntegration.Controllers
{
    [ApiController]
    [Route("api/stkcallback")]
    public class StkCallbackController : ControllerBase
    {
        private static ConcurrentDictionary<string, string> PaymentStatuses = new();

        [HttpPost]
        public IActionResult Post([FromBody] JObject callbackData)
        {
            Console.WriteLine("✅ STK Callback Received:");
            Console.WriteLine(callbackData.ToString());

            var body = callbackData["Body"]?["stkCallback"];
            var resultDesc = body?["ResultDesc"]?.ToString();
            var metadataItems = body?["CallbackMetadata"]?["Item"] as JArray;

            string? phone = metadataItems?.FirstOrDefault(i => i["Name"]?.ToString() == "PhoneNumber")?["Value"]?.ToString();
            string? mpesaReceipt = metadataItems?.FirstOrDefault(i => i["Name"]?.ToString() == "MpesaReceiptNumber")?["Value"]?.ToString();
            string? firstName = metadataItems?.FirstOrDefault(i => i["Name"]?.ToString() == "FirstName")?["Value"]?.ToString();
            string? lastName = metadataItems?.FirstOrDefault(i => i["Name"]?.ToString() == "LastName")?["Value"]?.ToString();

            if (!string.IsNullOrEmpty(phone))
            {
                // Save to SQL Server
                try
                {
                    string connectionString = "Server=localhost;Database=KMMS;Trusted_Connection=True;";
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO MpesaTransactions (PhoneNumber, MpesaReceiptNumber, FirstName, LastName, ResultDesc)
                            VALUES (@PhoneNumber, @MpesaReceiptNumber, @FirstName, @LastName, @ResultDesc)", conn))
                        {
                            cmd.Parameters.AddWithValue("@PhoneNumber", phone ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MpesaReceiptNumber", mpesaReceipt ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FirstName", firstName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LastName", lastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ResultDesc", resultDesc ?? (object)DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Also save to memory cache for polling
                    string fullName = $"{firstName} {lastName}".Trim();
                    string message = $"Payment Successful!\n" +
                                     $"Name: {fullName}\n" +
                                     $"Phone: {phone}\n" +
                                     $"Receipt: {mpesaReceipt}\n" +
                                     $"Status: {resultDesc}";

                    PaymentStatuses[phone!] = message; // Fix: Use null-forgiving operator (!) to ensure phone is not null.
                    PollingController.SaveCallbackMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error inserting into database: " + ex.Message);
                }
            }

            return Ok();
        }

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
