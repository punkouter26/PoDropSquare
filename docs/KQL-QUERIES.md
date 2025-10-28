# Application Insights KQL Queries for PoDropSquare

This document contains Kusto Query Language (KQL) queries for monitoring and analyzing the PoDropSquare application in Azure Application Insights.

## 📊 How to Use These Queries

1. Open **Azure Portal** → **Application Insights** → **Logs**
2. Copy and paste any query below
3. Adjust the time range as needed (default: last 24 hours)
4. Click **Run** to execute

---

## 🎯 User Activity & Engagement

### 1. Active Users by Hour
```kql
// Shows unique users per hour over the last 24 hours
requests
| where timestamp > ago(24h)
| where name !contains "health" and name !contains "swagger"
| summarize UserCount = dcount(user_Id) by bin(timestamp, 1h)
| render timechart
```

### 2. Most Popular Pages
```kql
// Top 10 most visited pages in the last 7 days
pageViews
| where timestamp > ago(7d)
| summarize PageViews = count() by name
| top 10 by PageViews desc
| render barchart
```

### 3. User Sessions Analysis
```kql
// Average session duration and page views per session
pageViews
| where timestamp > ago(7d)
| summarize 
    PageViewsPerSession = count(),
    SessionDuration = max(timestamp) - min(timestamp)
    by session_Id
| summarize 
    AvgPageViews = avg(PageViewsPerSession),
    AvgDuration = avg(SessionDuration),
    TotalSessions = count()
```

### 4. Geographic Distribution
```kql
// Users by country/region
requests
| where timestamp > ago(7d)
| summarize RequestCount = count() by client_CountryOrRegion
| order by RequestCount desc
| render piechart
```

---

## 🚀 Performance Monitoring

### 5. Top 10 Slowest API Requests
```kql
// Identify performance bottlenecks
requests
| where timestamp > ago(24h)
| where name !contains "health"
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    P99Duration = percentile(duration, 99),
    Count = count()
    by name
| order by P95Duration desc
| take 10
```

### 6. Request Performance Over Time
```kql
// Track API performance trends
requests
| where timestamp > ago(24h)
| summarize 
    AvgDuration = avg(duration),
    P95 = percentile(duration, 95)
    by bin(timestamp, 1h), name
| render timechart
```

### 7. Slow Database Queries (>100ms)
```kql
// Monitor Table Storage operations
dependencies
| where timestamp > ago(24h)
| where type == "Azure table"
| where duration > 100
| summarize 
    SlowQueryCount = count(),
    AvgDuration = avg(duration),
    MaxDuration = max(duration)
    by name, target
| order by SlowQueryCount desc
```

### 8. Client-Side Performance
```kql
// Browser page load times
pageViews
| where timestamp > ago(24h)
| summarize 
    AvgLoadTime = avg(duration),
    P95LoadTime = percentile(duration, 95),
    Count = count()
    by name
| order by AvgLoadTime desc
```

---

## ❌ Error Tracking & Diagnostics

### 9. Error Rate by Hour
```kql
// Monitor application health
requests
| where timestamp > ago(24h)
| summarize 
    TotalRequests = count(),
    FailedRequests = countif(success == false)
    by bin(timestamp, 1h)
| extend ErrorRate = (FailedRequests * 100.0) / TotalRequests
| render timechart
```

### 10. Top Exceptions
```kql
// Most common errors in the last 7 days
exceptions
| where timestamp > ago(7d)
| summarize Count = count() by type, outerMessage
| order by Count desc
| take 20
```

### 11. Failed Requests Details
```kql
// Investigate specific failures
requests
| where timestamp > ago(24h)
| where success == false
| project 
    timestamp,
    name,
    resultCode,
    duration,
    url,
    client_IP,
    customDimensions
| order by timestamp desc
| take 50
```

### 12. JavaScript Errors
```kql
// Client-side errors from Blazor app
traces
| where timestamp > ago(24h)
| where customDimensions.CategoryName contains "ClientError"
| project 
    timestamp,
    message,
    severityLevel,
    customDimensions
| order by timestamp desc
| take 100
```

---

## 🎮 Game-Specific Metrics

### 13. Score Submissions Analysis
```kql
// Track game score patterns
requests
| where timestamp > ago(7d)
| where name contains "SubmitScore"
| extend score = toint(customDimensions.score)
| summarize 
    TotalSubmissions = count(),
    AvgScore = avg(score),
    MaxScore = max(score),
    MinScore = min(score)
    by bin(timestamp, 1h)
| render timechart
```

### 14. Leaderboard Access Patterns
```kql
// Monitor leaderboard usage
requests
| where timestamp > ago(7d)
| where name contains "Leaderboard" or name contains "GetTop10"
| summarize RequestCount = count() by bin(timestamp, 1h), name
| render timechart
```

### 15. Player Rank Lookups
```kql
// Track player rank queries
requests
| where timestamp > ago(24h)
| where name contains "PlayerRank"
| extend playerInitials = tostring(customDimensions.playerInitials)
| summarize 
    LookupCount = count(),
    UniqueInitials = dcount(playerInitials)
    by bin(timestamp, 1h)
```

---

## 🔍 Availability & Health

### 16. Availability Percentage
```kql
// Overall uptime calculation
requests
| where timestamp > ago(7d)
| summarize 
    Total = count(),
    Successful = countif(success == true)
| extend Availability = (Successful * 100.0) / Total
| project Availability
```

### 17. Health Check Monitoring
```kql
// Track health endpoint status
requests
| where timestamp > ago(24h)
| where name contains "health"
| summarize 
    HealthChecks = count(),
    Failures = countif(success == false)
    by bin(timestamp, 5m), name
| render timechart
```

### 18. Dependency Health
```kql
// Monitor Azure Table Storage availability
dependencies
| where timestamp > ago(24h)
| where type == "Azure table"
| summarize 
    Total = count(),
    Failed = countif(success == false)
    by target
| extend FailureRate = (Failed * 100.0) / Total
| order by FailureRate desc
```

---

## 📈 Business Intelligence

### 19. Daily Active Users (DAU)
```kql
// Track daily unique users
requests
| where timestamp > ago(30d)
| where name !contains "health"
| summarize DAU = dcount(user_Id) by bin(timestamp, 1d)
| render timechart
```

### 20. User Retention
```kql
// Users who returned within 7 days
let firstVisit = requests
    | where timestamp > ago(30d)
    | summarize FirstVisit = min(timestamp) by user_Id;
let returnVisit = requests
    | where timestamp > ago(30d)
    | join kind=inner firstVisit on user_Id
    | where timestamp > FirstVisit + 7d
    | summarize ReturnVisit = count() by user_Id;
returnVisit
| summarize ReturnedUsers = count()
```

### 21. Peak Usage Times
```kql
// Identify highest traffic hours
requests
| where timestamp > ago(7d)
| extend Hour = hourofday(timestamp)
| summarize RequestCount = count() by Hour
| order by RequestCount desc
| render columnchart
```

### 22. Conversion Funnel
```kql
// Track user journey: View → Play → Submit Score
let pageViews = pageViews
    | where timestamp > ago(7d)
    | summarize by user_Id;
let gameStarts = requests
    | where timestamp > ago(7d)
    | where name contains "Game"
    | summarize by user_Id;
let scoreSubmissions = requests
    | where timestamp > ago(7d)
    | where name contains "SubmitScore"
    | summarize by user_Id;
print 
    TotalVisitors = toscalar(pageViews | count),
    GameStarts = toscalar(gameStarts | count),
    ScoreSubmissions = toscalar(scoreSubmissions | count)
```

---

## 🔧 Diagnostics & Troubleshooting

### 23. Request Duration Distribution
```kql
// Histogram of response times
requests
| where timestamp > ago(24h)
| where name !contains "health"
| summarize count() by bin(duration, 50)
| render columnchart
```

### 24. Throttling & Rate Limiting
```kql
// Detect rate-limited requests
requests
| where timestamp > ago(24h)
| where resultCode == "429"
| summarize ThrottledRequests = count() by bin(timestamp, 1h), client_IP
| order by ThrottledRequests desc
```

### 25. Memory & Resource Usage
```kql
// Monitor memory consumption
performanceCounters
| where timestamp > ago(24h)
| where name == "% Processor Time" or name == "Available Bytes"
| summarize avg(value) by bin(timestamp, 5m), name
| render timechart
```

### 26. End-to-End Transaction Tracing
```kql
// Trace a specific request through the system
requests
| where timestamp > ago(1h)
| where name contains "SubmitScore"
| take 1
| project operation_Id
| join kind=inner (
    union requests, dependencies, traces, exceptions
    | where timestamp > ago(1h)
) on operation_Id
| project timestamp, itemType, name, duration, success
| order by timestamp asc
```

---

## 🎨 Custom Dashboards

### 27. Real-Time Monitoring Dashboard
```kql
// Combine key metrics for live monitoring
let timeRange = ago(1h);
let requests_summary = requests
    | where timestamp > timeRange
    | summarize 
        TotalRequests = count(),
        FailedRequests = countif(success == false),
        AvgDuration = avg(duration)
    | extend ErrorRate = (FailedRequests * 100.0) / TotalRequests;
let active_users = requests
    | where timestamp > timeRange
    | summarize ActiveUsers = dcount(user_Id);
let exceptions_count = exceptions
    | where timestamp > timeRange
    | summarize ExceptionCount = count();
requests_summary
| extend ActiveUsers = toscalar(active_users)
| extend Exceptions = toscalar(exceptions_count)
| project 
    TotalRequests,
    ErrorRate,
    AvgDuration,
    ActiveUsers,
    Exceptions
```

### 28. Weekly Performance Summary
```kql
// Executive summary of last 7 days
requests
| where timestamp > ago(7d)
| where name !contains "health"
| summarize 
    TotalRequests = count(),
    UniqueUsers = dcount(user_Id),
    AvgResponseTime = avg(duration),
    P95ResponseTime = percentile(duration, 95),
    ErrorRate = (countif(success == false) * 100.0) / count(),
    Availability = (countif(success == true) * 100.0) / count()
```

---

## 🚨 Alerts & Notifications

### 29. High Error Rate Alert
```kql
// Alert when error rate exceeds 5%
requests
| where timestamp > ago(5m)
| summarize 
    Total = count(),
    Errors = countif(success == false)
| extend ErrorRate = (Errors * 100.0) / Total
| where ErrorRate > 5
```

### 30. Slow Performance Alert
```kql
// Alert when P95 latency exceeds 1000ms
requests
| where timestamp > ago(5m)
| where name !contains "health"
| summarize P95Duration = percentile(duration, 95)
| where P95Duration > 1000
```

### 31. Dependency Failure Alert
```kql
// Alert when Table Storage fails
dependencies
| where timestamp > ago(5m)
| where type == "Azure table"
| summarize FailureCount = countif(success == false)
| where FailureCount > 0
```

---

## 💡 Best Practices

### Query Optimization Tips
1. Always use `where timestamp > ago(Xh)` to limit data scanned
2. Use `| take N` to limit results for large datasets
3. Add `| summarize` before `| render` for better visualization
4. Use `bin()` for time-based aggregations
5. Leverage `extend` for calculated fields before filtering

### Alert Configuration
- Set up alerts for queries #29, #30, #31
- Use Action Groups for email/SMS notifications
- Configure severity levels appropriately
- Test alerts before deploying to production

### Dashboard Creation
- Pin frequently used queries to dashboards
- Use query #27 for real-time monitoring
- Create separate dashboards for dev/prod environments
- Share dashboards with team members

---

## 🔗 Additional Resources

- [KQL Quick Reference](https://docs.microsoft.com/en-us/azure/data-explorer/kql-quick-reference)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Azure Monitor Alerts](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview)

---

**Last Updated**: October 27, 2025  
**Application**: PoDropSquare  
**Environment**: Production & Development
