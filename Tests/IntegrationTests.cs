using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FHIRcastSandbox {
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>{
        private WebApplicationFactory<Startup> factory;

        public IntegrationTests(WebApplicationFactory<Startup> factory) {
            this.factory = factory;
        }

        [Fact]
        public async Task Test1() {
          // Arrange
          var client = this.factory.CreateClient();

          // Act
          var result = await client.GetAsync("/api/hub");

          // Assert
          result.EnsureSuccessStatusCode();
        }
    }
}

