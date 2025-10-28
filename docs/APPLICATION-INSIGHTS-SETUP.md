# Application Insights Setup & Configuration Guide

This guide explains how to configure Application Insights telemetry for PoDropSquare and use KQL queries for monitoring.

## 📦 Configuration Overview

Application Insights is already configured in the infrastructure (Bicep) and will be automatically provisioned during deployment.

### Infrastructure Components
- **Log Analytics Workspace**: Centralized log storage
- **Application Insights**: APM and telemetry
- **Connection String**: Auto-configured via Bicep outputs

---

## 🔧 Local Development Setup

### 1. Update appsettings.Development.json

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/",
    "EnableAdaptiveSampling": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### 2. Get Your Connection String

```powershell
# After deploying to Azure
azd env get-values

# Look for: APPLICATIONINSIGHTS_CONNECTION_STRING
```

Or from Azure Portal:
1. Navigate to your Application Insights resource
2. **Overview** → Copy **Connection String**
3. Update `appsettings.json` or set environment variable

---

## 📊 Serilog Configuration

The application uses Serilog with Application Insights sink (already configured in `appsettings.json`):

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/podropsquare-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Key Features:
✅ **Structured Logging** - JSON-formatted logs  
✅ **Correlation IDs** - Track requests across services  
✅ **Exception Tracking** - Automatic stack traces  
✅ **Custom Properties** - User-Agent, RemoteIP, RequestHost  

---

## 🎯 Using KQL Queries

### Access Application Insights Logs

1. **Azure Portal** → **Application Insights** → **Logs**
2. Or use direct link: `https://portal.azure.com/#@YOUR_TENANT/resource/RESOURCE_ID/logs`

### Quick Start Queries

Copy these from `Program.cs` or `/docs/KQL-QUERIES.md`:

#### Performance Monitoring
```kql
// P95 latency by endpoint
requests
| where timestamp > ago(1h)
| summarize P95 = percentile(duration, 95) by name
| render barchart
```

#### Error Tracking
```kql
// Recent exceptions
exceptions
| where timestamp > ago(1h)
| project timestamp, type, outerMessage
| order by timestamp desc
```

#### User Activity
```kql
// Active users by hour
requests
| where timestamp > ago(24h)
| summarize Users = dcount(user_Id) by bin(timestamp, 1h)
| render timechart
```

---

## 🚨 Setting Up Alerts

### 1. High Error Rate Alert

**Query**:
```kql
requests
| where timestamp > ago(5m)
| summarize Total = count(), Errors = countif(success == false)
| extend ErrorRate = (Errors * 100.0) / Total
| where ErrorRate > 5
```

**Steps**:
1. Application Insights → **Alerts** → **New alert rule**
2. **Condition** → Custom log search
3. Paste query above
4. **Threshold** → Greater than 0 results
5. **Evaluation frequency** → Every 5 minutes
6. **Action group** → Email/SMS notification

### 2. Slow Performance Alert

**Query**:
```kql
requests
| where timestamp > ago(5m)
| where name !contains "health"
| summarize P95 = percentile(duration, 95)
| where P95 > 1000
```

### 3. Dependency Failure Alert

**Query**:
```kql
dependencies
| where timestamp > ago(5m)
| where type == "Azure table"
| summarize Failures = countif(success == false)
| where Failures > 0
```

---

## 📈 Creating Dashboards

### 1. Pin Queries to Dashboard

After running a query:
1. Click **Pin to dashboard**
2. Choose existing or create new dashboard
3. Name the tile appropriately

### 2. Recommended Dashboard Tiles

1. **Active Users** (last 24h)
2. **Error Rate** (last 24h)
3. **P95 Latency** by endpoint
4. **Recent Exceptions** (last 1h)
5. **Health Check Status**
6. **Top 10 Pages** by views
7. **Geographic Distribution**
8. **Score Submissions** over time

### 3. Real-Time Monitoring

Use Live Metrics for real-time monitoring:
1. Application Insights → **Live Metrics**
2. View requests, failures, and server metrics in real-time

---

## 🎮 Game-Specific Telemetry

### Custom Events

Track game-specific events in your code:

```csharp
// In ScoresController.cs
_logger.LogInformation("Score submitted: {Score} by {PlayerInitials}", 
    request.Score, 
    request.PlayerInitials);
```

### Query Custom Events

```kql
traces
| where timestamp > ago(1h)
| where message contains "Score submitted"
| extend score = extract("Score: ([0-9]+)", 1, message)
| summarize AvgScore = avg(toint(score))
```

---

## 🔍 Client-Side Telemetry (Blazor)

### JavaScript Error Tracking

Client errors are sent to `/api/log/error`:

```kql
traces
| where timestamp > ago(24h)
| where customDimensions.CategoryName contains "ClientError"
| project 
    timestamp,
    message,
    customDimensions.errorMessage,
    customDimensions.stackTrace,
    customDimensions.url
| order by timestamp desc
```

### Client Logs

General client logs sent to `/api/log/client`:

```kql
traces
| where timestamp > ago(24h)
| where customDimensions.CategoryName contains "ClientLog"
| summarize count() by tostring(customDimensions.level)
| render piechart
```

---

## 💰 Cost Management

### Sampling Configuration

In production, enable adaptive sampling to reduce costs:

```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "SamplingSettings": {
      "MaxTelemetryItemsPerSecond": 5
    }
  }
}
```

### Data Retention

Configure retention in Azure Portal:
1. Application Insights → **Usage and estimated costs**
2. **Data Retention** → Set to 30, 60, or 90 days
3. Lower retention = lower costs

### Cost Estimates
- **Free tier**: First 5GB/month free
- **Pay-as-you-go**: ~$2.30/GB after free tier
- **Expected**: ~$10-20/month for moderate traffic

---

## 📚 Reference Documentation

### KQL Query Collections
- **Full Query Library**: `/docs/KQL-QUERIES.md` (31 queries)
- **Quick Reference**: `Program.cs` (top 5 queries)

### Categories Covered
1. **User Activity** (4 queries) - Sessions, engagement, geography
2. **Performance** (4 queries) - Latency, dependencies, client-side
3. **Errors** (4 queries) - Exceptions, failures, JS errors
4. **Game Metrics** (3 queries) - Scores, leaderboard, ranks
5. **Availability** (3 queries) - Uptime, health checks
6. **Business Intelligence** (4 queries) - DAU, retention, funnels
7. **Diagnostics** (4 queries) - Tracing, throttling, resources
8. **Dashboards** (2 queries) - Real-time, weekly summary
9. **Alerts** (3 queries) - Errors, performance, dependencies

---

## 🔗 Useful Links

- [Application Insights Overview](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [KQL Reference](https://docs.microsoft.com/en-us/azure/data-explorer/kql-quick-reference)
- [Serilog Application Insights Sink](https://github.com/serilog-contrib/serilog-sinks-applicationinsights)
- [Azure Monitor Best Practices](https://docs.microsoft.com/en-us/azure/azure-monitor/best-practices)

---

## ✅ Validation Checklist

After deployment, verify telemetry is working:

- [ ] Application Insights resource exists in Azure
- [ ] Connection string configured in App Service
- [ ] Logs appearing in Application Insights → Logs
- [ ] Run query: `requests | where timestamp > ago(5m) | take 10`
- [ ] Health checks visible: `requests | where name contains "health"`
- [ ] Exceptions tracked: `exceptions | where timestamp > ago(1h)`
- [ ] Client errors logged: `traces | where customDimensions.CategoryName contains "ClientError"`
- [ ] Custom dashboard created with key metrics
- [ ] Alerts configured for error rate and performance

---

**Last Updated**: October 27, 2025  
**Version**: 1.0  
**Status**: ✅ Phase 4 Complete
