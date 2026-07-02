# Arcade Leaderboard Integration Tests

## Overview

Comprehensive test suites have been created to validate the arcade leaderboard system across three layers:
1. **Application Layer** — Business logic handlers
2. **API Layer** — HTTP endpoints
3. **Integration** — End-to-end flows

---

## Application Layer Tests

### `SubmitArcadeScoreHandlerTests.cs`

Tests the `SubmitArcadeScore` mediator command handler.

#### Test Cases

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Handle_WithNonexistentPlayer_ReturnsFalse` | Submit score for non-existent player | Returns false, no entry created |
| `Handle_WithExistingPlayer_CreatesNewEntry` | First score for a player | Creates new ArcadeScoreEntry |
| `Handle_WithHigherScore_UpdatesEntry` | New score beats personal best | Updates score and duration |
| `Handle_WithLowerScore_DoesNotUpdate` | New score is worse than PB | Returns false, no update |
| `Handle_SameScoreDifferentDuration_UpdatesIfFaster` | Same score but faster completion | Updates to faster time |
| `Handle_MultipleGamesAndDifficulties` | Submit across game/difficulty combos | All entries stored independently |

**Coverage:** Score comparison logic, upsert behavior, database isolation by game+difficulty

---

### `GetArcadeLeaderboardHandlerTests.cs`

Tests the `GetArcadeLeaderboard` mediator query handler.

#### Test Cases

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Handle_EmptyLeaderboard_ReturnsEmptyList` | No scores for game/difficulty | Returns empty items, total=0 |
| `Handle_WithScores_ReturnsSortedByScoreThenDuration` | Multiple scores | Sorted score DESC, duration ASC |
| `Handle_WithScores_ReturnsCorrectRanks` | Scores with ranks | Ranks 1, 2, 3, ... assigned correctly |
| `Handle_WithPagination_ReturnsCorrectPage` | Page 1, 2, 3 requests (pageSize=10) | Correct items per page, no overlap |
| `Handle_WithAuthenticatedPlayer_ReturnsPlayerRank` | Player on current page | MyRank and MyScore populated |
| `Handle_WithAuthenticatedPlayerNotOnPage_ComputesRank` | Player ranked lower, off first page | Rank computed correctly (e.g., rank 16) |
| `Handle_DifferentGamesAndDifficulties_ReturnsSeparateLeaderboards` | Query for different games | Each game/difficulty isolated |
| `Handle_PageSizeExceedsMax_ClampsToMax` | PageSize=1000 (max is 100) | PageSize clamped to 100 |

**Coverage:** Sorting logic, pagination, rank computation, player identification, size validation

---

## API Endpoint Tests

### `ArcadeLeaderboardEndpointsTests.cs`

Tests HTTP endpoints end-to-end with real API factory.

#### POST `/api/v1/leaderboards/arcade/submit` — Submit Score

| Test | Scenario | Expected Status |
|------|----------|------------------|
| `SubmitScore_WithValidRequest_ReturnsSuccess` | Valid auth + valid request | 200 OK, success=true |
| `SubmitScore_WithoutAuth_ReturnsUnauthorized` | No token | 401 Unauthorized |
| `SubmitScore_WithInvalidGameId_ReturnsBadRequest` | Empty GameId | 400 Bad Request |
| `SubmitScore_WithNegativeScore_ReturnsBadRequest` | Score < 0 | 400 Bad Request |
| `SubmitScore_HigherScoreThanPrevious_UpdatesEntry` | New PB, verify in DB | 200 OK, DB reflects new values |
| `SubmitScore_LowerScoreThanPrevious_DoesNotUpdate` | Not a PB, verify in DB | 200 OK, success=false, DB unchanged |

**Coverage:** Authorization checks, input validation, business logic flow, database persistence

---

#### GET `/api/v1/leaderboards/arcade/{gameId}/{difficulty}` — Fetch Leaderboard

| Test | Scenario | Expected Status |
|------|----------|------------------|
| `GetLeaderboard_ReturnsTopScores` | Valid game/difficulty with scores | 200 OK, sorted entries with ranks |
| `GetLeaderboard_WithAuthentication_ReturnsPlayerRank` | Authenticated user, rank on page | 200 OK, myRank and myScore set |
| `GetLeaderboard_WithPagination_ReturnsCorrectPage` | Page 1 (10 items), Page 2 (10 items) | Correct items, page numbers, ranks 1-10 vs 11-20 |
| `GetLeaderboard_EmptyLeaderboard_ReturnsEmptyList` | No scores for that game/difficulty | 200 OK, items=[], total=0 |
| `GetLeaderboard_SameSortsCorrectlyByDuration` | Three entries, same score, different durations | Sorted by duration ascending (faster = higher rank) |

**Coverage:** API response shape, pagination controls, optional authentication, sorting edge cases

---

## Test Data Fixtures

### Test Players Created Per Test
- Named `test_player_N` for unit tests
- Named `leaderboard_player_N` for leaderboard tests
- Named `rank_test_player` for rank-specific tests

### Test Scores Schema
```
ArcadeScoreEntry {
  PlayerId: Guid,
  GameId: string (e.g., "patternSprint", "memoryFlip", "quickMathRush"),
  Difficulty: string (e.g., "easy", "normal", "hard", "insane"),
  Score: int,
  DurationMs: int,
  AchievedAtUtc: DateTimeOffset
}
```

---

## Running the Tests

### Prerequisites
```bash
dotnet add package xunit
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

### Run All Tests
```bash
dotnet test Synaptix.Backend.Api.Tests
dotnet test Synaptix.Backend.Application.Tests
```

### Run Specific Test Class
```bash
dotnet test --filter ClassName=ArcadeLeaderboardEndpointsTests
dotnet test --filter ClassName=SubmitArcadeScoreHandlerTests
dotnet test --filter ClassName=GetArcadeLeaderboardHandlerTests
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~SubmitScore_HigherScoreThanPrevious_UpdatesEntry"
```

---

## Coverage Summary

### Tested Scenarios

✅ **Authentication**
- Authorized requests accept scores
- Unauthorized requests rejected (401)

✅ **Input Validation**
- Empty GameId rejected (400)
- Negative score rejected (400)
- Negative duration rejected (400)

✅ **Score Comparison Logic**
- Higher score → update ✓
- Lower score → no update ✓
- Same score, faster time → update ✓
- Same score, slower time → no update ✓

✅ **Ranking & Sorting**
- Scores ranked by score DESC, duration ASC
- Pagination maintains correct ranks across pages
- Player rank computed correctly when off-page

✅ **Leaderboard Isolation**
- Game+Difficulty combinations independent
- Multiple players per game/difficulty
- Separate leaderboards per game version

✅ **Edge Cases**
- Empty leaderboards (0 scores)
- Single entry leaderboards
- Max pagination (pageSize clamped to 100)
- Same score with different durations

### Not Tested (Out of Scope)

- ❌ Database connection failures (mock-based tests use in-memory)
- ❌ Rate limiting (not implemented)
- ❌ Concurrent writes (single-threaded tests)
- ❌ Performance benchmarks (unit/integration focus)

---

## CI/CD Integration

These tests are designed to run in CI pipelines:

```yaml
# Example: .github/workflows/test.yml
- name: Run Application Tests
  run: dotnet test Synaptix.Backend.Application.Tests --logger:"console;verbosity=normal"

- name: Run API Integration Tests
  run: dotnet test Synaptix.Backend.Api.Tests --logger:"console;verbosity=normal"
```

---

## Future Enhancements

1. **Concurrency Tests** — Use multiple tasks to test simultaneous score submissions
2. **Performance Tests** — Benchmark query times with 100K+ entries
3. **Load Tests** — Simulate peak leaderboard traffic
4. **Contract Tests** — Verify API response schema against OpenAPI spec
5. **Snapshot Tests** — Compare leaderboard rankings against golden snapshots

---

## Test Maintenance

### When to Update Tests

- ✏️ **Business Logic Changes** — Update comparison rules in handler tests
- ✏️ **API Contract Changes** — Update endpoint tests (status codes, response shape)
- ✏️ **Database Schema Changes** — Update fixture creation in integration tests
- ✏️ **New Features** — Add corresponding test cases before implementing feature

### Common Issues

| Issue | Solution |
|-------|----------|
| In-memory DB doesn't match production | Read the test comments; InMemory is intentional for unit tests. Integration tests use real DB in CI. |
| Test fails in CI but passes locally | Check database state (migrations ran), timezone handling (use UTC), network/auth config |
| Flaky pagination tests | Ensure test data has consistent ordering (use DateTimeOffset, not DateTime) |

---

## Related Documentation

- [Arcade Leaderboard Architecture](./ARCADE_LEADERBOARD_ARCHITECTURE.md)
- [API Endpoint Reference](./API_ENDPOINTS.md)
- [Database Schema](./DATABASE_SCHEMA.md)
