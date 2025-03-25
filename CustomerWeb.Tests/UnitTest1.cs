using Microsoft.VisualStudio.TestTools.UnitTesting;
using CustomerWeb.Services;
using CustomerWeb.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace CustomerWeb.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task GetProductsAsync_WithSearchTerm_ShouldReturnFilteredProducts()
        {
            // Arrange
            var expectedProducts = new List<ProductViewModel>
            {
                new ProductViewModel 
                { 
                    Id = 1, 
                    Name = "Test Product", 
                    Description = "Search match" 
                }
            };

            // Create a test HttpMessageHandler that returns our expected products
            var testHttpMessageHandler = new TestHttpMessageHandler(expectedProducts);

            // Create HttpClient with test handler
            var httpClient = new HttpClient(testHttpMessageHandler)
            {
                BaseAddress = new Uri("https://test.api/")
            };

            // Create mock logger and cache
            var logger = new Mock<ILogger<ProductApiService>>();
            var cache = new Mock<IMemoryCache>();

            // Setup cache to allow setting and getting values
            var cacheEntry = new Mock<ICacheEntry>();
            cache
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntry.Object);

            // Setup TryGetValue to return false (not in cache)
            object cachedValue = null;
            cache
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            // Create service
            var service = new ProductApiService(
                httpClient, 
                logger.Object, 
                cache.Object
            );

            // Act
            var results = await service.GetProductsAsync("Test");

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("Test Product", results.First().Name);
        }

        [TestMethod]
        public async Task GetProductByIdAsync_ExistingProduct_ShouldReturnProduct()
        {
            // Arrange
            var expectedProduct = new ProductViewModel 
            { 
                Id = 1, 
                Name = "Specific Product" 
            };

            // Create a test HttpMessageHandler that returns our expected product
            var testHttpMessageHandler = new TestHttpMessageHandler(expectedProduct);

            // Create HttpClient with test handler
            var httpClient = new HttpClient(testHttpMessageHandler)
            {
                BaseAddress = new Uri("https://test.api/")
            };

            // Create mock logger and cache
            var logger = new Mock<ILogger<ProductApiService>>();
            var cache = new Mock<IMemoryCache>();

            // Setup cache to allow setting and getting values
            var cacheEntry = new Mock<ICacheEntry>();
            cache
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntry.Object);

            // Setup TryGetValue to return false (not in cache)
            object cachedValue = null;
            cache
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            // Create service
            var service = new ProductApiService(
                httpClient, 
                logger.Object, 
                cache.Object
            );

            // Act
            var result = await service.GetProductByIdAsync(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Specific Product", result.Name);
        }

        // Custom HttpMessageHandler for testing
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly object _responseContent;

            public TestHttpMessageHandler(object responseContent)
            {
                _responseContent = responseContent;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(_responseContent)
                };
            }
        }
    }
}