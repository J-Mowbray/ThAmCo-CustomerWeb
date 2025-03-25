using Polly;
using Polly.Extensions.Http;
using CustomerWeb.BackgroundServices;
using CustomerWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add memory cache
builder.Services.AddMemoryCache();

// Add Auth0 Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    // Auth0 settings for config
    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];

    // Set response type to code
    options.ResponseType = "code";

    // Configure the scope
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Set the callback path
    options.CallbackPath = new PathString("/callback");

    // Configure the Claims Issuer to be Auth0
    options.ClaimsIssuer = "Auth0";
    options.SaveTokens = true;

    options.Events = new OpenIdConnectEvents
    {
        // handle the logout redirection
        OnRedirectToIdentityProviderForSignOut = (context) =>
        {
            var logoutUri = $"https://{builder.Configuration["Auth0:Domain"]}/v2/logout?client_id={builder.Configuration["Auth0:ClientId"]}";

            var postLogoutUri = context.Properties.RedirectUri;
            if (!string.IsNullOrEmpty(postLogoutUri))
            {
                if (postLogoutUri.StartsWith("/"))
                {
                    var request = context.Request;
                    postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                }
                logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
            }

            context.Response.Redirect(logoutUri);
            context.HandleResponse();

            return Task.CompletedTask;
        }
    };
});

// Register services
builder.Services.AddHttpClient<IProductApiService, ProductApiService>(client =>
{
    // In development, use localhost; in production, use Azure URL
    var baseUrl = builder.Environment.IsDevelopment()
        ? "http://localhost:5252/"
        : builder.Configuration["ApiSettings:BaseUrl"];

    client.BaseAddress = new Uri(baseUrl ?? "http://localhost:5252/");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Register the background service for stock updates
builder.Services.AddHostedService<StockUpdateService>();

var app = builder.Build();

// In development, trigger initial product sync
if (app.Environment.IsDevelopment())
{
    // Use a scope to resolve services
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var productService = scope.ServiceProvider.GetRequiredService<IProductApiService>();

        logger.LogInformation("Development environment detected - triggering initial product sync");

        try
        {
            // Run this without awaiting to avoid blocking startup
            // This is for development convenience only
            Task.Run(async () =>
            {
                try
                {
                    await productService.TriggerProductSyncAsync();
                    logger.LogInformation("Initial product sync completed successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during initial product sync");
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting up initial product sync");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add these two lines in this order
app.UseAuthentication(); // Add this line before UseAuthorization
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