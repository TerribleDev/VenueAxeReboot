using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueAxe.Data;
using VenueAxe.Domain.Enums;
using VenueAxe.Web.Hubs;

namespace VenueAxe.Web.BackgroundServices;

public class SessionLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionLifecycleBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

    public SessionLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionLifecycleBackgroundService started with check interval {CheckIntervalSeconds}s", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing session lifecycles in background worker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SessionLifecycleBackgroundService stopped");
    }

    private async Task ProcessExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VenueAxeDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<LaneHub, ILaneClient>>();

        var now = DateTimeOffset.UtcNow;

        // Query active sessions where ExpiresAt has passed.
        // If an in-progress match is actively being played, allow a 3-minute grace period (ExpiresAt.AddMinutes(3) <= now)
        // so throwers in Round 9 or 10 are not abruptly cut off mid-throw.
        var candidateSessions = await db.LaneSessions
            .IgnoreQueryFilters()
            .Include(s => s.Lane)
            .Include(s => s.Matches)
            .Where(s => s.Status == SessionStatus.Active && s.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        var expiredSessions = candidateSessions.Where(s =>
        {
            var hasActiveMatch = s.Matches.Any(m => m.Status == MatchStatus.InProgress);
            return !hasActiveMatch || s.ExpiresAt.AddMinutes(3) <= now;
        }).ToList();

        if (expiredSessions.Count == 0) return;

        foreach (var session in expiredSessions)
        {
            session.Status = SessionStatus.Completed;
            session.EndedAt = now;

            if (session.Lane != null)
            {
                session.Lane.CurrentStatus = LaneStatus.Turnaround;

                _logger.LogInformation("Session {SessionId} on Lane {LaneNumber} expired. Moving to Turnaround",
                    session.Id, session.Lane.LaneNumber);

                await hub.Clients.Group(LaneHub.GetLaneGroupName(session.LaneId))
                    .OnLaneStateChanged(session.LaneId, LaneStatus.Turnaround.ToString());

                await hub.Clients.Group("admin")
                    .OnLaneStateChanged(session.LaneId, LaneStatus.Turnaround.ToString());
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
