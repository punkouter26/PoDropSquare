# 🚀 HTTP Testing Quick Start

> **5-minute guide** to testing PoDropSquare API with automated assertions

## ✅ Prerequisites

1. **Install VS Code REST Client**:
   ```bash
   code --install-extension humao.rest-client
   ```

2. **Start API**:
   ```bash
   dotnet run --project backend/src/Po.PoDropSquare.Api
   ```

3. **Open test file**: `PoDropSquare.http`

## 🎯 Quick Test (30 seconds)

1. Open `PoDropSquare.http` in VS Code
2. Find this section:
   ```http
   ### Get full health status
   # @name healthCheck
   GET {{baseUrl}}/api/health
   ```
3. Click **"Send Request"** above the `###` comment
4. View results in right pane ➡️
5. Check **Output** panel (bottom) → **REST Client** channel for assertions

**Expected output**:
```
✓ Status should be 200 OK
✓ Response should be JSON
✓ Response should contain status
✓ Response time should be under 500ms

4/4 tests passed
```

## 📊 Test Categories

| Category | Location | What It Tests |
|----------|----------|---------------|
| **Health** | Lines 30-90 | API availability, response times |
| **Scores** | Lines 95-240 | Submission, validation, error handling |
| **Leaderboard** | Lines 245-330 | Ordering, pagination, caching |
| **Player Rank** | Lines 335-395 | Rank lookup, missing players |
| **Logging** | Lines 400-495 | Client log ingestion |
| **Performance** | Lines 540-575 | Response time baselines |
| **CORS** | Lines 580-620 | Cross-origin headers |
| **Errors** | Lines 625-680 | 404, 415, malformed JSON |
| **Batch** | Lines 685-785 | End-to-end workflow |

## 🏃 Common Scenarios

### Scenario 1: Submit a Score

```http
### Submit a valid score
POST {{baseUrl}}/api/scores
Content-Type: application/json

{
  "playerInitials": "ABC",
  "survivalTime": 15.75,
  "sessionSignature": "sha256_hash_example_12345",
  "clientTimestamp": "{{$datetime iso8601}}"
}
```

**Click "Send Request"** → See assertions:
- ✓ Status 200/201
- ✓ Response is JSON
- ✓ Contains `accepted` field
- ✓ Contains `calculatedScore`
- ✓ Response <1 second

### Scenario 2: Get Leaderboard

```http
### Get top 10 leaderboard
GET {{baseUrl}}/api/scores/top10
Accept: application/json
```

**Click "Send Request"** → See assertions:
- ✓ Status 200
- ✓ Array with ≤10 entries
- ✓ Ordered by score (descending)
- ✓ Sequential ranks (1, 2, 3...)
- ✓ All required fields present
- ✓ Response <500ms

### Scenario 3: Test Error Handling

```http
### Test validation - Invalid player initials (should fail)
POST {{baseUrl}}/api/scores
Content-Type: application/json

{
  "playerInitials": "TOOLONG",
  "survivalTime": 10.5,
  "sessionSignature": "sha256_hash_invalid",
  "clientTimestamp": "{{$datetime iso8601}}"
}
```

**Click "Send Request"** → See assertions:
- ✓ Status 400 Bad Request
- ✓ Response is Problem Details JSON
- ✓ Error mentions "initials" validation

### Scenario 4: Run Batch Workflow

**Navigate to "Batch Testing" section** (line 685), then run these **in order**:

1. ✅ Check health
2. ✅ Get current leaderboard (stores count)
3. ✅ Submit score (stores score/rank)
4. ✅ Get updated leaderboard (verifies score appears)
5. ✅ Check player rank (confirms rank matches)

Each step shares state via `client.global.set()` and validates consistency.

## 🎨 Understanding Results

### ✅ Passed Test
```
✓ Status should be 200 OK
✓ Response should be JSON
```

### ❌ Failed Test
```
✗ Status should be 200 OK
  Expected 200, got 429
```

### ⚡ Performance Log
```
⚡ Health check response time: 45ms
⚡ Leaderboard response time: 187ms
📊 Entries returned: 8
```

## 🔧 Troubleshooting

### Problem: "Connection refused"
**Solution**: Start the API first
```bash
dotnet run --project backend/src/Po.PoDropSquare.Api
```

### Problem: "429 Too Many Requests"
**Solution**: Wait 60 seconds between rapid test runs (rate limiting)

### Problem: "404 Not Found"
**Solution**: Check API is running on `http://localhost:5000`

### Problem: No assertions shown
**Solution**: Open **Output** panel → Select **REST Client** from dropdown

## 📈 Performance Baselines

| Endpoint | Target | Why |
|----------|--------|-----|
| Health (simple) | <100ms | Load balancer checks |
| Health (full) | <500ms | Monitoring/diagnostics |
| Submit score | <1000ms | User feedback |
| Leaderboard | <300ms | User-facing UI |
| Client logging | <300ms | Non-blocking |

## 🎯 Daily Workflow

### Before Starting Work
```http
### 1. Check health
GET {{baseUrl}}/api/health
```
**Send Request** → Verify API is running

### During Development
After changing an endpoint, test it:
1. Find relevant request in `PoDropSquare.http`
2. Send request
3. Check assertions pass
4. Fix any failures

### Before Committing
Run key tests:
1. Health check ✅
2. Submit score ✅
3. Get leaderboard ✅
4. Run batch workflow ✅

## 📚 Full Documentation

See **`docs/PHASE3.4-HTTP-ASSERTIONS-GUIDE.md`** for:
- Complete assertion catalog (111 assertions)
- Technical implementation details
- Response test script syntax
- State management examples
- Maintenance guide

## 💡 Pro Tips

1. **Use `# @name`** to reference responses:
   ```http
   # @name myRequest
   GET {{baseUrl}}/api/scores
   
   ### Use previous response
   GET {{baseUrl}}/api/scores/player/{{myRequest.response.body[0].playerInitials}}/rank
   ```

2. **Log custom messages**:
   ```javascript
   > {%
     client.log("🎯 Score: " + response.body.calculatedScore);
     client.log("🏆 Rank: " + response.body.rank);
   %}
   ```

3. **Share state between requests**:
   ```javascript
   // Request 1
   > {%
     client.global.set("playerId", response.body.playerInitials);
   %}
   
   // Request 2
   > {%
     const playerId = client.global.get("playerId");
     client.log("Testing player: " + playerId);
   %}
   ```

4. **Run all tests**: Click **"Send All Requests"** at top of file

## 🎉 Success!

You now have **instant API testing** without rebuilding the solution!

- ⚡ **Fast**: 1-2 seconds per request
- 🎯 **Interactive**: Click → result
- 🔍 **Detailed**: 111 automated assertions
- 🐛 **Debugging-friendly**: Edit and retry instantly

---

**Next**: [Phase 4 - CI/CD Pipeline](./PHASE4-CICD-GUIDE.md) (coming soon)
