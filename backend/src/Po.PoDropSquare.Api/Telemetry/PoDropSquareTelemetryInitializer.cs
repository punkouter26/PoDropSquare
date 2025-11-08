using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Po.PoDropSquare.Api.Telemetry;

/// <summary>
/// Enriches all telemetry with custom properties before sending to Application Insights.
/// This runs for every telemetry item (requests, dependencies, traces, exceptions, metrics).
/// </summary>
public class PoDropSquareTelemetryInitializer : ITelemetryInitializer
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public PoDropSquareTelemetryInitializer(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public void Initialize(ITelemetry telemetry)
    {
        // Set cloud role name (service name in distributed tracing)
        telemetry.Context.Cloud.RoleName = PoDropSquareTelemetry.ServiceName;
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;

        // Add custom properties to all telemetry
        telemetry.Context.GlobalProperties["Application"] = "PoDropSquare";
        telemetry.Context.GlobalProperties["Environment"] = _environment.EnvironmentName;
        telemetry.Context.GlobalProperties["ServiceVersion"] = PoDropSquareTelemetry.ServiceVersion;
        telemetry.Context.GlobalProperties["RuntimeVersion"] = Environment.Version.ToString();
        telemetry.Context.GlobalProperties["MachineName"] = Environment.MachineName;
        telemetry.Context.GlobalProperties["ProcessorCount"] = Environment.ProcessorCount.ToString();

        // Add deployment information if available
        var deploymentId = _configuration["DeploymentId"];
        if (!string.IsNullOrEmpty(deploymentId))
        {
            telemetry.Context.GlobalProperties["DeploymentId"] = deploymentId;
        }

        var buildNumber = _configuration["BuildNumber"];
        if (!string.IsNullOrEmpty(buildNumber))
        {
            telemetry.Context.GlobalProperties["BuildNumber"] = buildNumber;
        }

        // Add session and user context for better tracking
        if (telemetry.Context.Session.Id == null)
        {
            telemetry.Context.Session.Id = Guid.NewGuid().ToString();
        }

        // Set component version
        telemetry.Context.Component.Version = PoDropSquareTelemetry.ServiceVersion;
    }
}
