# API Contracts: PoDropSquare

**Date**: September 13, 2025  
**Feature**: Physics-Based Tower Building Game  
**Version**: v1.0.0

## Overview

This document defines the REST API contracts for PoDropSquare, including all endpoints, request/response schemas, and validation rules. The API follows RESTful principles and uses JSON for all data exchange.

## Base Configuration

**Base URL**: `https://api.podropsquare.com/api/v1`  
**Content-Type**: `application/json`  
**Rate Limiting**: 60 requests per minute per IP address  
**Authentication**: None required for public endpoints  

## API Endpoints

### 1. Submit Score

Submit a new player score to the leaderboard system.

**Endpoint**: `POST /scores`

**Request Body**:
```json
{
  "playerInitials": "ABC",
  "survivalTime": 18.75,
  "sessionSignature": "sha256_hash_of_session_data",
  "clientTimestamp": "2025-09-13T15:30:45.123Z"
}
```

**Request Schema**:
- `playerInitials` (string, required): Player initials, 1-3 alphanumeric characters
- `survivalTime` (decimal, required): Survival time in seconds, 0.01-20.00 range
- `sessionSignature` (string, required): Client-generated validation hash
- `clientTimestamp` (string, required): ISO 8601 timestamp of game completion

**Response 200 - Score Accepted**:
```json
{
  "success": true,
  "scoreId": "123e4567-e89b-12d3-a456-426614174000",
  "leaderboardPosition": 3,
  "qualifiedForLeaderboard": true,
  "message": "Score submitted successfully"
}
```

**Response 201 - New High Score**:
```json
{
  "success": true,
  "scoreId": "123e4567-e89b-12d3-a456-426614174000",
  "leaderboardPosition": 1,
  "qualifiedForLeaderboard": true,
  "message": "New high score achieved!"
}
```

**Response 400 - Validation Error**:
```json
{
  "success": false,
  "error": "VALIDATION_FAILED",
  "message": "Player initials must be 1-3 alphanumeric characters",
  "details": {
    "field": "playerInitials",
    "value": "TOOLONG",
    "constraint": "length_1_to_3_alphanumeric"
  }
}
```

**Response 429 - Rate Limited**:
```json
{
  "success": false,
  "error": "RATE_LIMITED",
  "message": "Too many score submissions. Please wait before submitting again.",
  "retryAfter": 60
}
```

**Validation Rules**:
- Player initials: 1-3 characters, alphanumeric only, no profanity
- Survival time: Must be realistic (0.01-20.00 seconds)
- Session signature: Must match server-side validation
- Client timestamp: Must be within 5 minutes of server time

---

### 2. Get Leaderboard

Retrieve the current top-10 leaderboard.

**Endpoint**: `GET /scores/top10`

**Query Parameters**: None

**Response 200 - Success**:
```json
{
  "success": true,
  "leaderboard": [
    {
      "position": 1,
      "playerInitials": "PRO",
      "survivalTime": 20.00,
      "achievedAt": "2025-09-13T14:15:30.000Z"
    },
    {
      "position": 2,
      "playerInitials": "ACE",
      "survivalTime": 19.87,
      "achievedAt": "2025-09-13T13:45:22.000Z"
    },
    {
      "position": 3,
      "playerInitials": "TOP",
      "survivalTime": 19.42,
      "achievedAt": "2025-09-13T12:30:15.000Z"
    }
  ],
  "lastUpdated": "2025-09-13T15:30:45.123Z",
  "totalEntries": 3
}
```

**Response 500 - Server Error**:
```json
{
  "success": false,
  "error": "SERVER_ERROR",
  "message": "Unable to retrieve leaderboard at this time"
}
```

**Caching**: 
- Response cached for 5 minutes server-side
- Client should cache and respect `lastUpdated` timestamp
- ETags supported for conditional requests

---

### 3. Health Check

Check API availability and service status.

**Endpoint**: `GET /health`

**Query Parameters**: None

**Response 200 - Healthy**:
```json
{
  "status": "healthy",
  "timestamp": "2025-09-13T15:30:45.123Z",
  "version": "1.0.0",
  "checks": {
    "database": "healthy",
    "cache": "healthy",
    "storage": "healthy"
  }
}
```

**Response 503 - Unhealthy**:
```json
{
  "status": "unhealthy",
  "timestamp": "2025-09-13T15:30:45.123Z",
  "version": "1.0.0",
  "checks": {
    "database": "unhealthy",
    "cache": "healthy",
    "storage": "degraded"
  }
}
```

## Error Handling

### Standard Error Response Format
All error responses follow this structure:

```json
{
  "success": false,
  "error": "ERROR_CODE",
  "message": "Human-readable error description",
  "details": {
    "field": "fieldName",
    "value": "submittedValue",
    "constraint": "validation_rule"
  },
  "timestamp": "2025-09-13T15:30:45.123Z",
  "requestId": "req_123456789"
}
```

### Common Error Codes
- `VALIDATION_FAILED`: Input validation error
- `RATE_LIMITED`: Too many requests
- `SERVER_ERROR`: Internal server error
- `SERVICE_UNAVAILABLE`: Service temporarily unavailable
- `INVALID_SIGNATURE`: Session signature validation failed
- `DUPLICATE_SUBMISSION`: Score already submitted for this session

## Security Measures

### Rate Limiting
- 60 requests per minute per IP address
- Sliding window implementation
- Progressive backoff for repeated violations

### Input Validation
- All inputs sanitized and validated server-side
- SQL injection prevention through parameterized queries
- XSS prevention through output encoding
- Length limits enforced on all string inputs

### Session Signature Validation
- Client generates SHA-256 hash of session data
- Server validates signature against expected game behavior
- Prevents client-side score manipulation
- Includes timing validation to detect automation

### CORS Configuration
```
Access-Control-Allow-Origin: https://podropsquare.com
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type, Accept
Access-Control-Max-Age: 86400
```

## Performance Specifications

### Response Time Targets
- Health check: < 50ms (95th percentile)
- Leaderboard retrieval: < 200ms (95th percentile)
- Score submission: < 500ms (95th percentile)

### Throughput Targets
- Support 1000 concurrent score submissions
- Support 5000 concurrent leaderboard retrievals
- 99.9% uptime SLA

### Caching Strategy
- Leaderboard cached for 5 minutes (Redis)
- Health check results cached for 30 seconds
- Database connection pooling enabled
- Response compression (gzip) enabled

This API specification provides a complete contract for implementing the PoDropSquare backend services, ensuring reliable score submission, leaderboard management, and system monitoring capabilities.