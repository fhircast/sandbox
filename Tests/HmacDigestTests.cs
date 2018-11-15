using System;
using Xunit;

namespace FHIRcastSandbox.Rules {
    public class HmacDigestTests {
        [Fact]
        public void CreateDigest_KeyAndPayload_CreateCorrectDigest_Test() {
            // Arange
            var key = "key";
            var payload = "The quick brown fox jumps over the lazy dog";

            // Act
            var result = new HmacDigest().CreateDigest(key, payload);

            // Assert
            Assert.Equal("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8", result);
        }

        [Fact]
        public void CreateHubSignature_KeyAndPayload_CreateSignatureForSha256_Test() {
            // Arange
            var key = "key";
            var payload = "The quick brown fox jumps over the lazy dog";

            // Act
            var result = new HmacDigest().CreateHubSignature(key, payload);

            // Assert
            Assert.Equal("sha256=f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8", result);
        }
    }
}
