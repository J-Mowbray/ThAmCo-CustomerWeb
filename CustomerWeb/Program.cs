using Polly;
using Polly.Extensions.Http;
using ThAmCo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure ProductApiService with Polly for resilience
builder.Services.AddHttpClient<ProductApiService>(client =>
{
    // In development, use localhost; in production, use Azure URL
    var baseUrl = builder.Environment.IsDevelopment() 
        ? "http://localhost:5252/"
        : builder.Configuration["ApiSettings:BaseUrl"];
        
    client.BaseAddress = new Uri(baseUrl ?? "http://localhost:5252/");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Helper methods for Polly policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // HttpRequestException, 5XX, and 408 responses
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // Handle 404s with retry
        .WaitAndRetryAsync(
            3, // Number of retries
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                // You can add logging here
                Console.WriteLine($"Retrying API request. Attempt {retryAttempt}");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5, // Number of exceptions before breaking circuit
            TimeSpan.FromSeconds(30), // Time circuit stays open before resetting
            onBreak: (outcome, timespan) =>
            {
                Console.WriteLine("Circuit breaker tripped! API requests suspended.");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker reset. API requests resumed.");
            });
}