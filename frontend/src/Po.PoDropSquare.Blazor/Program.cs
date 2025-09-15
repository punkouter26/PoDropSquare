using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Po.PoDropSquare.Blazor;
using Po.PoDropSquare.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with correct base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5000/") });

// Register services
builder.Services.AddScoped<PhysicsInteropService>();

// Configure logging with remote logger
builder.Logging.ClearProviders();
// builder.Logging.AddConsole(); // Keep console logging for development - removed due to dependency issues

// Add remote logging provider - register as singleton but create HttpClient factory pattern
builder.Services.AddSingleton<RemoteLoggerProvider>();

// Register the remote logger provider
builder.Logging.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<RemoteLoggerProvider>());

await builder.Build().RunAsync();
