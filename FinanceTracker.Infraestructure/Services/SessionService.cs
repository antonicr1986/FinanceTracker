using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsActiveAsync(int userId, DateTime issuedAt)
    {
        var user = await _context.Users
            .Where(user => user.Id == userId)
            .Select(user => new { user.SessionsRevokedAt })
            .FirstOrDefaultAsync();

        // Usuario borrado: su token ya no representa a nadie.
        if (user is null)
        {
            return false;
        }

        return user.SessionsRevokedAt is null || issuedAt >= user.SessionsRevokedAt;
    }

    public async Task RevokeAllAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
        {
            return;
        }

        Revoke(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Marca el momento de la revocacion sin guardar (lo guarda quien llama).
    ///
    /// "iat" va en segundos enteros, asi que se trunca igual: los tokens de
    /// segundos anteriores dejan de valer y los emitidos en este mismo segundo
    /// o despues siguen valiendo. Asi el token que se entrega justo despues
    /// (por ejemplo, al vincular con Google) no nace revocado.
    /// </summary>
    public static void Revoke(User user)
    {
        var now = DateTime.UtcNow;
        user.SessionsRevokedAt = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
