using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Rules {
    public class Notifications<T> : INotifications<HttpResponseMessage> {
        private ILogger<Notifications<HttpResponseMessage>> logger;

        public Notifications(ILogger<Notifications<HttpResponseMessage>> logger) {
            this.logger = logger;
        }

        public async Task<HttpResponseMessage> SendNotification(Notification notification, Subscription subscription) {
            // Create the JSON body
            string body = JsonConvert.SerializeObject(notification, Formatting.Indented);
            HttpContent httpContent = new StringContent(body);

            // Add the headers
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.Add("X-Hub-Signature", XHubSignature(subscription, body));

            this.logger.LogInformation($"Sending notification: " +
                                        $"{httpContent.Headers.ToString()}" +
                                        $"{body}");

            // Send notification
            HttpClient client = new HttpClient();
            var response = await client.PostAsync(subscription.Callback, httpContent);

            this.logger.LogDebug($"Got response from posting notification:{Environment.NewLine}{response}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}.");
            return response;
        }

        /// <summary>
        /// Calculates and returns the X-Hub-Signature header. Currently uses sha256
        /// </summary>
        /// <param name="subscription">Subscription to get the secret from</param>
        /// <param name="body">Body used to calculate the signature</param>
        /// <returns>The sha256 hash of the body using the subscription's secret</returns>
        private string XHubSignature(Subscription subscription, string body)
        {
            using (HMACSHA256 sha256 = new HMACSHA256(Encoding.ASCII.GetBytes(subscription.Secret)))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

                byte[] hash = sha256.ComputeHash(bodyBytes);
                StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    stringBuilder.AppendFormat("{0:x2}", b);
                }

                return "sha256=" + stringBuilder.ToString(); //Encoding.UTF8.GetString(hash);
            }
        }
    }
}
