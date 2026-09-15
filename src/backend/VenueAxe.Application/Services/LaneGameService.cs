using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Services;
public class LaneGameService : ILaneGameService
{
    private readonly IUnitOfWork _uow;

    public LaneGameService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ActiveSessionSummaryDto?> StartSessionAsync(Guid laneId, StartSessionRequest request)
    {
        var lane = await _uow.Lanes.GetByIdIgnoreQueryFiltersAsync(laneId);
        if (lane == null) return null;

        var existingSession = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (existingSession != null)
        {
            existingSession.Status = SessionStatus.Completed;
            existingSession.EndedAt = DateTimeOffset.UtcNow;
            await _uow.LaneSessions.UpdateAsync(existingSession);
        }

        var sessionId = UuidV7.NewGuid();
        var roster = request.InitialRoster.Count > 0 ? request.InitialRoster : new List<GamePlayer>
        {
            new() { Id = UuidV7.NewGuid().ToString(), Name = "Player 1", AvatarColor = "#f59e0b" },
            new() { Id = UuidV7.NewGuid().ToString(), Name = "Player 2", AvatarColor = "#06b6d4" }
        };

        foreach (var p in roster)
        {
            if (string.IsNullOrWhiteSpace(p.Id))
            {
                p.Id = UuidV7.NewGuid().ToString();
            }
        }

        var session = new LaneSession
        {
            Id = sessionId,
            TenantId = lane.TenantId,
            VenueId = lane.VenueId,
            LaneId = lane.Id,
            BookingId = request.BookingId,
            SessionTitle = request.SessionTitle,
            Status = SessionStatus.Active,
            StartedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(request.DurationMinutes),
            ActiveRosterJson = JsonSerializer.Serialize(roster)
        };

        var matchId = UuidV7.NewGuid();
        var gameTypeId = string.IsNullOrWhiteSpace(request.GameTypeId) ? "watl_standard" : request.GameTypeId;
        var engine = GameEngineRegistry.GetEngine(gameTypeId);
        var initialGameState = engine.Initialize(matchId, roster);

        var match = new GameMatch
        {
            Id = matchId,
            SessionId = sessionId,
            GameTypeId = gameTypeId,
            GameConfigJson = JsonSerializer.Serialize(new GameConfig()),
            Status = MatchStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };

        session.Matches.Add(match);
        lane.CurrentStatus = LaneStatus.Active;

        await _uow.Lanes.UpdateAsync(lane);
        await _uow.LaneSessions.AddAsync(session);
        await _uow.SaveChangesAsync();

        return new ActiveSessionSummaryDto(
            session.Id,
            session.SessionTitle,
            session.StartedAt,
            session.ExpiresAt,
            request.DurationMinutes,
            session.ActiveRosterJson,
            initialGameState
        );
    }

    public async Task<GameStateSnapshot?> RecordThrowAsync(Guid laneId, ThrowInputDto input)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return null;

        var match = session.Matches.FirstOrDefault(m => m.Status == MatchStatus.InProgress);
        if (match == null) return null;

        var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
        var engine = GameEngineRegistry.GetEngine(match.GameTypeId);

        var state = engine.Initialize(match.Id, players);
        foreach (var t in match.Throws.OrderBy(x => x.TotalThrowSequence))
        {
            state = engine.RecordThrow(state, t.NormalizedX, t.NormalizedY, t.TargetZone, t.IsClutchCalled);
        }

        var updatedState = engine.RecordThrow(state, input.X, input.Y, input.ManualZone, input.IsClutchCalled);

        if (updatedState.LastThrow != null)
        {
            var throwEntity = new MatchThrow
            {
                Id = UuidV7.NewGuid(),
                MatchId = match.Id,
                PlayerId = updatedState.LastThrow.PlayerId,
                RoundNumber = updatedState.CurrentRound,
                ThrowNumberInRound = 1,
                TotalThrowSequence = match.Throws.Count + 1,
                TargetZone = updatedState.LastThrow.Zone,
                NormalizedX = input.X,
                NormalizedY = input.Y,
                IsClutchCalled = input.IsClutchCalled,
                PointsAwarded = updatedState.LastThrow.PointsAwarded,
                ThrownAtUtc = DateTimeOffset.UtcNow
            };

            await _uow.LaneSessions.AddMatchThrowAsync(throwEntity);

            if (updatedState.Status == MatchStatus.Finished)
            {
                match.Status = MatchStatus.Finished;
                match.WinnerPlayerId = updatedState.WinnerPlayerId;
                match.CompletedAt = DateTimeOffset.UtcNow;
            }

            await _uow.SaveChangesAsync();
        }

        return updatedState;
    }

    public async Task<GameStateSnapshot?> UndoLastThrowAsync(Guid laneId)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return null;

        var match = session.Matches.FirstOrDefault(m => m.Status == MatchStatus.InProgress)
                    ?? session.Matches.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        if (match == null || match.Throws.Count == 0) return null;

        var lastThrow = match.Throws.OrderByDescending(t => t.TotalThrowSequence).FirstOrDefault();
        if (lastThrow != null)
        {
            match.Throws.Remove(lastThrow);
            await _uow.LaneSessions.RemoveMatchThrowAsync(lastThrow);

            if (match.Status == MatchStatus.Finished)
            {
                match.Status = MatchStatus.InProgress;
                match.WinnerPlayerId = null;
                match.CompletedAt = null;
            }

            await _uow.SaveChangesAsync();
        }

        var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
        var engine = GameEngineRegistry.GetEngine(match.GameTypeId);
        var state = engine.Initialize(match.Id, players);
        foreach (var t in match.Throws.OrderBy(x => x.TotalThrowSequence))
        {
            state = engine.RecordThrow(state, t.NormalizedX, t.NormalizedY, t.TargetZone, t.IsClutchCalled);
        }

        return state;
    }

    public async Task<GameStateSnapshot?> SkipTurnAsync(Guid laneId)
    {
        return await RecordThrowAsync(laneId, new ThrowInputDto(null, null, TargetZone.Miss, false));
    }

    public async Task<bool> ExtendSessionAsync(Guid laneId, int extraMinutes)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return false;

        session.ExpiresAt = session.ExpiresAt.AddMinutes(extraMinutes);
        await _uow.LaneSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<ActiveSessionSummaryDto?> GetActiveSessionAsync(Guid laneId)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return null;

        var match = session.Matches.FirstOrDefault(m => m.Status == MatchStatus.InProgress)
                    ?? session.Matches.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        GameStateSnapshot? currentGameState = null;
        if (match != null)
        {
            var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
            var engine = GameEngineRegistry.GetEngine(match.GameTypeId);
            currentGameState = engine.Initialize(match.Id, players);
            foreach (var t in match.Throws.OrderBy(x => x.TotalThrowSequence))
            {
                currentGameState = engine.RecordThrow(currentGameState, t.NormalizedX, t.NormalizedY, t.TargetZone, t.IsClutchCalled);
            }
        }

        var durationMinutes = (int)Math.Max(1, (session.ExpiresAt - session.StartedAt).TotalMinutes);

        return new ActiveSessionSummaryDto(
            session.Id,
            session.SessionTitle,
            session.StartedAt,
            session.ExpiresAt,
            durationMinutes,
            session.ActiveRosterJson,
            currentGameState
        );
    }

    public async Task<GameStateSnapshot?> StartRematchAsync(Guid laneId)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return null;

        // Finish any in-progress match without modifying the session expiration or booking
        foreach (var m in session.Matches.Where(m => m.Status == MatchStatus.InProgress))
        {
            m.Status = MatchStatus.Finished;
            m.CompletedAt = DateTimeOffset.UtcNow;
        }

        var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
        // Reset player scores for the rematch
        foreach (var p in players)
        {
            p.Score = 0;
            p.ThrowsTaken = 0;
            p.BullseyesHit = 0;
            p.KillsHit = 0;
            p.KillsRemaining = 2;
            p.KillsCalled = 0;
            p.Streak = 0;
            p.ThrowHistory.Clear();
        }

        var lastMatch = session.Matches.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        var gameTypeId = lastMatch?.GameTypeId ?? "watl_standard";
        var engine = GameEngineRegistry.GetEngine(gameTypeId);

        var matchId = UuidV7.NewGuid();
        var initialGameState = engine.Initialize(matchId, players);

        var newMatch = new GameMatch
        {
            Id = matchId,
            SessionId = session.Id,
            GameTypeId = gameTypeId,
            GameConfigJson = JsonSerializer.Serialize(new GameConfig()),
            Status = MatchStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };

        await _uow.LaneSessions.AddMatchAsync(newMatch);
        await _uow.SaveChangesAsync();

        return initialGameState;
    }

    public async Task<GameStateSnapshot?> SwitchGameAsync(Guid laneId, string newGameTypeId)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return null;

        // Finish current in-progress match without altering session duration
        foreach (var m in session.Matches.Where(m => m.Status == MatchStatus.InProgress))
        {
            m.Status = MatchStatus.Finished;
            m.CompletedAt = DateTimeOffset.UtcNow;
        }

        var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
        // Reset player scores for the new game mode
        foreach (var p in players)
        {
            p.Score = 0;
            p.ThrowsTaken = 0;
            p.BullseyesHit = 0;
            p.KillsHit = 0;
            p.KillsRemaining = 2;
            p.KillsCalled = 0;
            p.Streak = 0;
            p.ThrowHistory.Clear();
        }

        var gameId = string.IsNullOrWhiteSpace(newGameTypeId) ? "watl_standard" : newGameTypeId;
        var engine = GameEngineRegistry.GetEngine(gameId);

        var matchId = UuidV7.NewGuid();
        var initialGameState = engine.Initialize(matchId, players);

        var newMatch = new GameMatch
        {
            Id = matchId,
            SessionId = session.Id,
            GameTypeId = gameId,
            GameConfigJson = JsonSerializer.Serialize(new GameConfig()),
            Status = MatchStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };

        await _uow.LaneSessions.AddMatchAsync(newMatch);
        await _uow.SaveChangesAsync();

        return initialGameState;
    }

    public async Task<bool> EndSessionAsync(Guid laneId)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        var lane = await _uow.Lanes.GetByIdIgnoreQueryFiltersAsync(laneId);
        if (lane == null) return false;

        if (session != null)
        {
            session.Status = SessionStatus.Completed;
            session.EndedAt = DateTimeOffset.UtcNow;
            foreach (var m in session.Matches.Where(m => m.Status == MatchStatus.InProgress))
            {
                m.Status = MatchStatus.Finished;
                m.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        lane.CurrentStatus = LaneStatus.Turnaround;
        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubstitutePlayerAsync(Guid laneId, string playerId, string newName, string? newAvatarColor)
    {
        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return false;

        var players = JsonSerializer.Deserialize<List<GamePlayer>>(session.ActiveRosterJson) ?? new();
        var target = players.FirstOrDefault(p => p.Id == playerId);
        if (target == null) return false;

        target.Name = newName;
        if (!string.IsNullOrWhiteSpace(newAvatarColor))
        {
            target.AvatarColor = newAvatarColor;
        }

        session.ActiveRosterJson = JsonSerializer.Serialize(players);
        await _uow.LaneSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TransferLaneAsync(Guid sourceLaneId, Guid targetLaneId)
    {
        var sourceLane = await _uow.Lanes.GetByIdAsync(sourceLaneId);
        var targetLane = await _uow.Lanes.GetByIdAsync(targetLaneId);
        if (sourceLane == null || targetLane == null) return false;

        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(sourceLaneId);
        if (session == null) return false;

        // Reassign active session to target lane
        session.LaneId = targetLaneId;
        sourceLane.CurrentStatus = LaneStatus.Available;
        targetLane.CurrentStatus = LaneStatus.Active;

        await _uow.Lanes.UpdateAsync(sourceLane);
        await _uow.Lanes.UpdateAsync(targetLane);
        await _uow.LaneSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSessionTitleAsync(Guid laneId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return false;

        var session = await _uow.LaneSessions.GetActiveSessionForLaneAsync(laneId);
        if (session == null) return false;

        session.SessionTitle = newTitle.Trim();
        await _uow.LaneSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return true;
    }
}
