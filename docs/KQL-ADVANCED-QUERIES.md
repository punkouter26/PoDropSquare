# Advanced KQL Query Library for PoDropSquare

> **Comprehensive Kusto Query Language (KQL) queries** for monitoring, debugging, and business intelligence in Application Insights

## 📋 Table of Contents

1. [Custom Telemetry Queries](#custom-telemetry-queries)
2. [Distributed Tracing](#distributed-tracing)
3. [Custom Metrics](#custom-metrics)
4. [Performance Analysis](#performance-analysis)
5. [Business Intelligence](#business-intelligence)
6. [Alerting Queries](#alerting-queries)
7. [Debugging & Troubleshooting](#debugging--troubleshooting)

---

## 🎯 Custom Telemetry Queries

### 1. View Custom Activity Tags

```kql
dependencies
| where timestamp > ago(24h)
| where cloud_RoleName == "PoDropSquare.Api"
| extend playerInitials = tostring(customDimensions["game.player.initials"])
| extend score = toint(customDimensions["game.score"])
| extend survivalTime = todouble(customDimensions["game.survival_time"])
| where isnotempty(playerInitials)
| project timestamp, playerInitials, score, survivalTime, duration, success
| order by timestamp desc
| take 100
```

### 2. Score Submission Activity Traces

```kql
traces
| where timestamp > ago(24h)
| where cloud_RoleName == "PoDropSquare.Api"
| where customDimensions has "score.submitted"
| extend playerInitials = tostring(customDimensions["game.player.initials"])
| extend calculatedScore = toint(customDimensions["game.score"])
| extend survivalTime = todouble(customDimensions["game.survival_time"])
| project timestamp, playerInitials, calculatedScore, survivalTime, message
| order by timestamp desc
```

### 3. Validation Errors by Field

```kql
traces
| where timestamp > ago(24h)
| where customDimensions has "validation.error"
| extend field = tostring(customDimensions["validation.field"])
| extend error = tostring(customDimensions["validation.error"])
| summarize ErrorCount = count() by field, error
| order by ErrorCount desc
```

### 4. Cache Hit/Miss Ratio

```kql
traces
| where timestamp > ago(1h)
| where customDimensions has_any ("cache.hit", "cache.miss")
| extend cacheKey = tostring(customDimensions["cache.key"])
| extend eventType = case(
    customDimensions has "cache.hit", "Hit",
    customDimensions has "cache.miss", "Miss",
    "Unknown")
| summarize Hits = countif(eventType == "Hit"), 
            Misses = countif(eventType == "Miss") by bin(timestamp, 5m)
| extend HitRatio = (Hits * 100.0) / (Hits + Misses)
| render timechart
```

---

## 🔗 Distributed Tracing

### 5. End-to-End Request Traces with Custom Spans

```kql
requests
| where timestamp > ago(1h)
| where cloud_RoleName == "PoDropSquare.Api"
| where name contains "POST /api/scores"
| extend requestId = operation_Id
| join kind=leftouter (
    dependencies
    | where timestamp > ago(1h)
    | extend requestId = operation_Id
) on requestId
| project timestamp, 
          RequestName = name, 
          RequestDuration = duration, 
          DependencyName = name1, 
          DependencyDuration = duration1,
          Success = success,
          StatusCode = resultCode
| order by timestamp desc
| take 50
```

### 6. Slow Operations with Custom Tags

```kql
dependencies
| where timestamp > ago(24h)
| where cloud_RoleName == "PoDropSquare.Api"
| where duration > 1000  // Slower than 1 second
| extend operation = tostring(customDimensions["game.operation"])
| extend playerInitials = tostring(customDimensions["game.player.initials"])
| project timestamp, 
          operation, 
          playerInitials, 
          duration, 
          target, 
          resultCode
| order by duration desc
| take 50
```

### 7. Distributed Trace Visualization

```kql
// Get full trace for a specific operation
let operationId = "YOUR_OPERATION_ID"; // Replace with actual operation_Id
union requests, dependencies, traces, exceptions
| where operation_Id == operationId
| project timestamp, 
          itemType, 
          name, 
          message, 
          duration, 
          success, 
          customDimensions
| order by timestamp asc
```

---

## 📊 Custom Metrics

### 8. Scores Submitted Over Time

```kql
customMetrics
| where timestamp > ago(24h)
| where name == "game.scores.submitted"
| summarize SubmittedScores = sum(value) by bin(timestamp, 1h)
| render timechart
```

### 9. Score Distribution Histogram

```kql
customMetrics
| where timestamp > ago(24h)
| where name == "game.score.value"
| summarize count() by ScoreRange = bin(value, 100)
| order by ScoreRange asc
| render columnchart
```

### 10. Survival Time Distribution

```kql
customMetrics
| where timestamp > ago(24h)
| where name == "game.survival_time"
| summarize count() by TimeRange = bin(value, 1.0)  // 1-second bins
| order by TimeRange asc
| render columnchart with (title="Survival Time Distribution", xtitle="Seconds", ytitle="Player Count")
```

### 11. Cache Efficiency Metrics

```kql
customMetrics
| where timestamp > ago(24h)
| where name in ("cache.hits", "cache.misses")
| summarize Hits = sumif(value, name == "cache.hits"),
            Misses = sumif(value, name == "cache.misses") by bin(timestamp, 1h)
| extend HitRatio = (Hits * 100.0) / (Hits + Misses)
| project timestamp, Hits, Misses, HitRatio
| render timechart
```

### 12. Validation Errors Rate

```kql
customMetrics
| where timestamp > ago(24h)
| where name == "validation.errors"
| summarize ErrorsPerHour = sum(value) by bin(timestamp, 1h)
| render timechart with (title="Validation Errors Over Time")
```

### 13. Request Duration by Endpoint

```kql
customMetrics
| where timestamp > ago(24h)
| where name == "http.request.duration"
| extend endpoint = tostring(customDimensions["http.endpoint"])
| extend statusCode = toint(customDimensions["http.status_code"])
| summarize P50 = percentile(value, 50),
            P95 = percentile(value, 95),
            P99 = percentile(value, 99),
            Count = count() by endpoint, statusCode
| order by P95 desc
```

---

## ⚡ Performance Analysis

### 14. Slow API Requests with Custom Context

```kql
requests
| where timestamp > ago(24h)
| where duration > 500  // Slower than 500ms
| extend Application = tostring(customDimensions["Application"])
| extend ServiceVersion = tostring(customDimensions["ServiceVersion"])
| project timestamp, 
          name, 
          duration, 
          resultCode, 
          Application, 
          ServiceVersion,
          url
| order by duration desc
| take 100
```

### 15. Performance by Deployment Version

```kql
requests
| where timestamp > ago(7d)
| extend ServiceVersion = tostring(customDimensions["ServiceVersion"])
| extend BuildNumber = tostring(customDimensions["BuildNumber"])
| summarize P95Duration = percentile(duration, 95),
            AvgDuration = avg(duration),
            RequestCount = count(),
            ErrorRate = (countif(success == false) * 100.0) / count()
  by ServiceVersion, BuildNumber
| order by timestamp desc
```

### 16. Memory Usage Correlation with Performance

```kql
performanceCounters
| where timestamp > ago(24h)
| where name == "% Processor Time" or name == "Available Bytes"
| extend ProcessorCount = toint(customDimensions["ProcessorCount"])
| join kind=inner (
    requests
    | where timestamp > ago(24h)
    | summarize AvgDuration = avg(duration) by bin(timestamp, 5m)
) on $left.timestamp == $right.timestamp
| project timestamp, name, value, AvgDuration
| render timechart
```

---

## 💼 Business Intelligence

### 17. Player Activity Patterns

```kql
requests
| where timestamp > ago(7d)
| where name contains "/api/scores"
| summarize PlayerSessions = dcount(user_Id), 
            TotalRequests = count() 
  by bin(timestamp, 1h), DayOfWeek = dayofweek(timestamp)
| order by timestamp asc
| render timechart
```

### 18. Geographic Distribution of Players

```kql
requests
| where timestamp > ago(24h)
| where name contains "/api/scores"
| extend Country = tostring(customDimensions["ai.location.country"])
| extend City = tostring(customDimensions["ai.location.city"])
| summarize PlayerCount = dcount(user_Id), 
            RequestCount = count() 
  by Country, City
| order by PlayerCount desc
| take 20
```

### 19. Top Players by Score Submissions

```kql
traces
| where timestamp > ago(7d)
| where customDimensions has "score.submitted"
| extend playerInitials = tostring(customDimensions["game.player.initials"])
| extend score = toint(customDimensions["game.score"])
| where isnotempty(playerInitials)
| summarize HighScore = max(score),
            SubmissionCount = count(),
            LastSeen = max(timestamp)
  by playerInitials
| order by HighScore desc
| take 100
```

### 20. Leaderboard Request Patterns

```kql
customMetrics
| where timestamp > ago(7d)
| where name == "game.leaderboard.requests"
| extend leaderboardSize = toint(customDimensions["leaderboard.size"])
| summarize RequestCount = sum(value) by leaderboardSize, bin(timestamp, 1d)
| render barchart
```

### 21. Player Retention Analysis

```kql
let firstVisit = requests
    | where timestamp > ago(30d)
    | where name contains "/api/scores"
    | summarize FirstVisit = min(timestamp) by user_Id;
requests
| where timestamp > ago(30d)
| join kind=inner firstVisit on user_Id
| extend DaysSinceFirst = datetime_diff('day', timestamp, FirstVisit)
| summarize ReturnedPlayers = dcount(user_Id) by DaysSinceFirst
| order by DaysSinceFirst asc
| render linechart with (title="Player Retention Curve")
```

---

## 🚨 Alerting Queries

### 22. High Error Rate Alert

```kql
requests
| where timestamp > ago(5m)
| summarize TotalRequests = count(),
            FailedRequests = countif(success == false)
| extend ErrorRate = (FailedRequests * 100.0) / TotalRequests
| where ErrorRate > 5  // Alert if error rate > 5%
| project ErrorRate, TotalRequests, FailedRequests
```

### 23. Slow Performance Alert

```kql
requests
| where timestamp > ago(5m)
| where name contains "/api/scores"
| summarize P95Duration = percentile(duration, 95)
| where P95Duration > 1000  // Alert if P95 > 1 second
| project P95Duration
```

### 24. Dependency Failure Alert

```kql
dependencies
| where timestamp > ago(5m)
| where type == "Azure table"
| summarize FailureCount = countif(success == false),
            TotalCount = count()
| extend FailureRate = (FailureCount * 100.0) / TotalCount
| where FailureRate > 10  // Alert if >10% failures
| project FailureRate, FailureCount, TotalCount
```

### 25. Abnormal Traffic Pattern

```kql
requests
| where timestamp > ago(5m)
| summarize CurrentRate = count()
| extend BaselineRate = toscalar(
    requests
    | where timestamp between(ago(1h) .. ago(5m))
    | summarize avg_count = avg(itemCount))
| extend Deviation = (CurrentRate - BaselineRate) / BaselineRate * 100
| where abs(Deviation) > 50  // Alert if traffic deviates >50%
| project CurrentRate, BaselineRate, Deviation
```

---

## 🐛 Debugging & Troubleshooting

### 26. Recent Exceptions with Custom Context

```kql
exceptions
| where timestamp > ago(1h)
| extend Application = tostring(customDimensions["Application"])
| extend ServiceVersion = tostring(customDimensions["ServiceVersion"])
| extend Environment = tostring(customDimensions["Environment"])
| project timestamp,
          type,
          message,
          outerMessage,
          Application,
          ServiceVersion,
          Environment,
          operation_Name,
          problemId
| order by timestamp desc
| take 50
```

### 27. Failed Requests by User

```kql
requests
| where timestamp > ago(24h)
| where success == false
| extend user = tostring(customDimensions["ai.user.id"])
| summarize FailureCount = count(),
            ErrorCodes = make_set(resultCode)
  by user, name
| order by FailureCount desc
| take 50
```

### 28. Request Timeline for Specific Player

```kql
let playerInitials = "ABC";  // Replace with actual initials
union requests, traces, exceptions
| where timestamp > ago(24h)
| where customDimensions has playerInitials
| extend playerInitials = tostring(customDimensions["game.player.initials"])
| where playerInitials == playerInitials
| project timestamp,
          itemType,
          name,
          message,
          duration,
          success,
          resultCode
| order by timestamp asc
```

### 29. Telemetry Enrichment Verification

```kql
requests
| where timestamp > ago(1h)
| extend Application = tostring(customDimensions["Application"])
| extend Environment = tostring(customDimensions["Environment"])
| extend ServiceVersion = tostring(customDimensions["ServiceVersion"])
| extend RuntimeVersion = tostring(customDimensions["RuntimeVersion"])
| extend MachineName = tostring(customDimensions["MachineName"])
| extend ProcessorCount = tostring(customDimensions["ProcessorCount"])
| project timestamp,
          cloud_RoleName,
          cloud_RoleInstance,
          Application,
          Environment,
          ServiceVersion,
          RuntimeVersion,
          MachineName,
          ProcessorCount
| take 10
```

### 30. Snapshot Debugger Availability

```kql
// Check if Snapshot Debugger is collecting snapshots
exceptions
| where timestamp > ago(7d)
| where customDimensions has "Snapshot"
| extend SnapshotId = tostring(customDimensions["ai.snapshot.id"])
| where isnotempty(SnapshotId)
| project timestamp,
          type,
          message,
          SnapshotId,
          problemId,
          operation_Name
| order by timestamp desc
| take 50
```

---

## 📝 Query Usage Tips

### Best Practices

1. **Time Ranges**: Always specify appropriate time ranges with `ago()` to improve query performance
2. **Filters First**: Apply `where` filters early in the query before expensive operations
3. **Summarize Wisely**: Use `summarize` with appropriate time bins (5m, 1h, 1d)
4. **Limit Results**: Use `take` or `top` to limit result sets for faster queries
5. **Custom Dimensions**: Extract custom dimensions with `tostring()`, `toint()`, `todouble()`

### Common Aggregations

```kql
// Count occurrences
| summarize count()

// Percentiles
| summarize P50 = percentile(duration, 50), P95 = percentile(duration, 95)

// Unique count
| summarize UniqueUsers = dcount(user_Id)

// Average
| summarize AvgDuration = avg(duration)

// Time bins
| summarize ... by bin(timestamp, 1h)

// Multiple aggregations
| summarize Total = count(), Failures = countif(success == false) by ...
```

### Exporting Results

```kql
// Export to Power BI
requests
| where timestamp > ago(7d)
| summarize count() by bin(timestamp, 1h)
// Use "Export to Power BI" button in Application Insights

// Export to CSV
// Run query → Click "Export" → "To CSV"
```

---

## 🔗 Related Documentation

- **KQL Reference**: https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/
- **Application Insights Schema**: https://docs.microsoft.com/en-us/azure/azure-monitor/app/data-model
- **Custom Telemetry**: `/backend/src/Po.PoDropSquare.Api/Telemetry/`
- **Distributed Tracing**: OpenTelemetry W3C Trace Context

---

**Last Updated**: Phase 5 (Advanced Telemetry Implementation)
