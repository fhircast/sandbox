using System;

namespace FHIRcastSandbox.Rules {
    public class HmacDigest {
        public string CreateDigest(string key, string payload) {
            var byteKey = System.Text.Encoding.UTF8.GetBytes(key);
            using (var hmacHasher = new System.Security.Cryptography.HMACSHA256(byteKey)) {
                var digest = hmacHasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(digest).Replace("-", "").ToLower();
            }
        }

        public string CreateHubSignature(string key, string payload) {
            return $"sha256={this.CreateDigest(key, payload)}";
        }

        public bool VerifyHubSignature(string key, string payload, string signature) {
            return false;
            /* return this.CreateDigest(key, payload) == signature; */
        }
    }
}
