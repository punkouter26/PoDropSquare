# Data Model: PoDropSquare

**Date**: September 13, 2025  
**Feature**: Physics-Based Tower Building Game  
**Status**: Complete

## Entity Overview

This document defines the core data entities for PoDropSquare, including their properties, relationships, validation rules, and state transitions based on the functional requirements from the feature specification. Entities are designed for Azure Table Storage with appropriate partition and row key strategies.

## Core Entities

### GameSession
Represents a single 20-second survival attempt with current timer state, block positions, and danger status.

**Properties**:
- `SessionId`: Unique identifier for the game session (GUID)
- `StartTime`: Timestamp when the 20-second timer began (DateTimeOffset)
- `CurrentTime`: Elapsed time in the current session (TimeSpan, precision to 0.01 seconds)
- `IsActive`: Whether the session is currently running (boolean)
- `IsDangerCountdownActive`: Whether the 2-second danger countdown is running (boolean)
- `DangerCountdownStart`: Timestamp when danger countdown began (DateTimeOffset, nullable)
- `GameResult`: Outcome of the session (enum: InProgress, Victory, Defeat)
- `FinalSurvivalTime`: Final time achieved if session completed (TimeSpan, nullable)

**Validation Rules**:
- `CurrentTime` must not exceed 20.00 seconds for active sessions
- `DangerCountdownStart` must be set when `IsDangerCountdownActive` is true
- `FinalSurvivalTime` must match `CurrentTime` when session ends in victory
- `SessionId` must be unique across all sessions

**State Transitions**:
- Initial: `IsActive=true, GameResult=InProgress, CurrentTime=0.00`
- Danger Triggered: `IsDangerCountdownActive=true, DangerCountdownStart=now`
- Danger Cleared: `IsDangerCountdownActive=false, DangerCountdownStart=null`
- Victory: `IsActive=false, GameResult=Victory, FinalSurvivalTime=20.00`
- Defeat: `IsActive=false, GameResult=Defeat, FinalSurvivalTime=CurrentTime`

### Block
Individual physics object with position, color, rotation, and collision properties.

**Properties**:
- `BlockId`: Unique identifier for the block instance (GUID)
- `SessionId`: Reference to the game session (GUID, foreign key)
- `PositionX`: Horizontal position in game coordinates (float)
- `PositionY`: Vertical position in game coordinates (float)
- `Rotation`: Current rotation angle in radians (float)
- `VelocityX`: Horizontal velocity component (float)
- `VelocityY`: Vertical velocity component (float)
- `AngularVelocity`: Rotational velocity (float)
- `Color`: Block color as hex string (string, 7 characters including #)
- `CreatedAt`: Timestamp when block was dropped (DateTimeOffset)
- `IsSettled`: Whether block has stopped moving (boolean)
- `IsAboveGoalLine`: Whether block is currently above the red danger line (boolean)

**Validation Rules**:
- `PositionX` must be within game area boundaries (0 to game width)
- `PositionY` must be non-negative (blocks cannot go below ground)
- `Color` must be valid hex color format (#RRGGBB)
- `SessionId` must reference an existing GameSession
- `Rotation` should be normalized to [-π, π] range

**Relationships**:
- Many-to-one relationship with GameSession
- No direct relationships with other blocks (physics handles collisions)

### ScoreEntry (PoDropSquareScores Table)
Player achievement record containing initials, survival time, and submission timestamp.

**Azure Table Properties**:
- `PartitionKey`: "SCORES" (single partition for leaderboard queries)
- `RowKey`: Inverted timestamp + GUID for time-ordered retrieval (9999999999999 - ticks + guid)
- `PlayerInitials`: Player-entered initials (string, 1-3 characters)
- `SurvivalTime`: Achieved survival time in seconds (double, precision to 0.01)
- `SubmittedAt`: Timestamp when score was submitted (DateTimeOffset)
- `SourceIP`: IP address of submission for rate limiting (string, nullable)
- `IsValidated`: Whether score passed server-side validation (bool)
- `ValidationNotes`: Details about validation process (string, nullable)
- `SessionData`: Serialized game session summary for audit (string, nullable)

**Validation Rules**:
- `PlayerInitials` must be 1-3 alphanumeric characters only
- `SurvivalTime` must be between 0.01 and 20.00 seconds
- `SurvivalTime` must match realistic gameplay bounds (no impossible scores)
- `SubmittedAt` must be within reasonable time of game completion
- `PlayerInitials` cannot contain profanity or reserved words

**Table Storage Strategy**:
- Single partition for atomic queries across all scores
- RowKey design allows efficient time-ordered retrieval
- No secondary indexes needed due to simple query patterns

### Leaderboard (PoDropSquareLeaderboard Table)
Materialized view maintaining top 10 player performances for fast retrieval.

**Azure Table Properties**:
- `PartitionKey`: "TOP10" (single partition for atomic leaderboard queries)
- `RowKey`: Position padded with zeros (e.g., "001", "002", "010")
- `ScoreRowKey`: Reference to the score entry row key (string)
- `PlayerInitials`: Cached player initials for quick display (string)
- `SurvivalTime`: Cached survival time for quick display (double)
- `AchievedAt`: Cached timestamp for quick display (DateTimeOffset)
- `LastUpdated`: When this leaderboard entry was last updated (DateTimeOffset)

**Validation Rules**:
- `RowKey` must be between "001" and "010"
- `ScoreRowKey` must reference a validated ScoreEntry
- Cached values must match referenced ScoreEntry data
- Only one entry per position (enforced by RowKey uniqueness)

**Table Storage Strategy**:
- Single partition enables atomic top-10 queries
- RowKey design maintains natural ranking order
- Materialized view pattern for optimal read performance

## Derived Data Models

### GameState (Client-Side Only)
Runtime game state maintained in browser memory, not persisted.

**Properties**:
- `Session`: Current GameSession entity
- `Blocks`: Collection of active Block entities
- `PhysicsWorld`: Matter.js world instance (JavaScript object)
- `InputState`: Current user input status
- `AudioState`: Sound effect and music status
- `UIState`: Current screen/modal state

### ScoreSubmissionRequest (API Contract)
Data transfer object for score submission API endpoint.

**Properties**:
- `PlayerInitials`: Player-entered initials (string, required)
- `SurvivalTime`: Claimed survival time (decimal, required)
- `SessionSignature`: Client-side validation hash (string, required)
- `ClientTimestamp`: Client-side completion timestamp (DateTimeOffset, required)

### LeaderboardResponse (API Contract)
Data transfer object for leaderboard retrieval API endpoint.

**Properties**:
- `Entries`: Array of leaderboard entries (LeaderboardEntry[])
- `LastUpdated`: When leaderboard was last updated (DateTimeOffset)
- `PlayerRank`: Current player's rank if they have a score (int, nullable)

### LeaderboardEntry (API Contract)
Individual leaderboard entry for API response.

**Properties**:
- `Position`: Ranking position (int)
- `PlayerInitials`: Player initials (string)
- `SurvivalTime`: Survival time in seconds (decimal)
- `AchievedAt`: When score was achieved (DateTimeOffset)

## Data Flow Patterns

### Score Submission Flow
1. Client completes game session and calculates final survival time
2. Client generates session signature for validation
3. Client posts ScoreSubmissionRequest to API
4. Server validates score against realistic bounds and rate limits
5. Server creates ScoreEntry with validation results
6. Server updates Leaderboard if score qualifies for top 10
7. Server returns updated leaderboard position to client

### Leaderboard Retrieval Flow
1. Client requests current leaderboard from API
2. Server queries materialized Leaderboard view
3. Server returns LeaderboardResponse with cached data
4. Client updates UI with current rankings
5. Client optionally caches leaderboard data locally

### Game Session Management Flow
1. Client creates new GameSession entity on game start
2. Client updates session state during gameplay (timers, danger status)
3. Client creates Block entities as user drops blocks
4. Client updates block positions via physics simulation
5. Client monitors goal line breaches and updates danger countdown
6. Client finalizes session state on game completion

## Storage Considerations

### Azure Table Storage Schema
- Primary tables: PoDropSquareScores, PoDropSquareLeaderboard
- Partition strategies optimized for leaderboard queries and rate limiting
- String columns for SessionData to store flexible audit information as JSON
- No foreign key constraints needed due to NoSQL nature

### Client-Side Storage (Browser)
- SessionStorage for current game state (cleared on tab close)
- LocalStorage for offline score cache and user preferences
- IndexedDB consideration for future offline gameplay features
- No sensitive data stored client-side for security

### Caching Strategy
- Server-side: In-memory cache for top-10 leaderboard (5-minute TTL)
- Client-side: Local leaderboard cache with timestamp validation
- Azure Table Storage level: Built-in performance optimizations
- CDN-level: Static asset caching for game resources

This data model provides a comprehensive foundation for implementing all functional requirements while maintaining data integrity, supporting performance optimization, and enabling future feature enhancements.

## Azure Table Storage Specific Considerations

### Partition Strategy
- **PoDropSquareScores**: Single partition "SCORES" for simple leaderboard queries across all scores
- **PoDropSquareLeaderboard**: Single partition "TOP10" for atomic top-10 retrieval
- Future scaling: Could partition scores by date ranges if volume increases

### Row Key Design
- **Scores**: Inverted timestamp + GUID ensures time-ordered retrieval with uniqueness
- **Leaderboard**: Zero-padded position numbers maintain natural sort order
- Query efficiency: RowKey design eliminates need for complex filtering

### Local Development with Azurite
- Azurite emulator provides full Azure Table Storage API compatibility
- Connection string points to local emulator for development
- Same code works in both local and production environments
- Easy integration testing with real Table Storage semantics