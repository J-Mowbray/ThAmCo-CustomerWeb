using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CustomerWeb.Tests.Helpers
{
    /// <summary>
    /// Base test helper class with common mocking utilities
    /// </summary>
    public abstract class BaseTestHelper
    {
        /// <summary>
        /// Creates a mock HttpMessageHandler with predefined response
        /// </summary>
        /// <typeparam name="T">Type of response object</typeparam>
        /// <param name="responseObject">Object to serialize as response</param>
        /// <param name="statusCode">HTTP status code (default: OK)</param>
        /// <returns>Configured mock HttpMessageHandler</returns>
        protected Mock<HttpMessageHandler> CreateMockHttpMessageHandler<T>(
            T responseObject, 
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(
                        JsonSerializer.Serialize(responseObject), 
                        System.Text.Encoding.UTF8, 
                        "application/json")
                });

            return mockHandler;
        }

        /// <summary>
        /// Creates a mock logger for testing
        /// </summary>
        /// <typeparam name="T">Type of class being logged</typeparam>
        /// <returns>Mock ILogger</returns>
        protected Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock memory cache
        /// </summary>
        /// <returns>Mock IMemoryCache</returns>
        protected Mock<IMemoryCache> CreateMockMemoryCache()
        {
            var mockCache = new Mock<IMemoryCache>();
            
            // Setup cache to allow setting and getting values
            object cachedValue = null;
            mockCache
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            return mockCache;
        }

        /// <summary>
        /// Creates a HttpClient with a mocked message handler
        /// </summary>
        /// <param name="mockHandler">Mocked HttpMessageHandler</param>
        /// <param name="baseAddress">Optional base address</param>
        /// <returns>Configured HttpClient</returns>
        protected HttpClient CreateHttpClientWithMockHandler(
            Mock<HttpMessageHandler> mockHandler, 
            string baseAddress = "https://test.api/")
        {
            return new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri(baseAddress)
            };
        }
    }
}