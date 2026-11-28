namespace FinanceTracker.Infrastructure.Data;

/// <summary>Cuando toca el reinicio diario de la cuenta demo.</summary>
public static class DemoResetSchedule
{
    /// <summary>Hora del reinicio, en UTC: de madrugada en Europa, con poco uso.</summary>
    public static readonly TimeSpan ResetTimeUtc = TimeSpan.FromHours(3);

    /// <summary>Cuanto falta, desde <paramref name="nowUtc"/>, para el proximo reinicio.</summary>
    public static TimeSpan DelayUntilNextReset(DateTime nowUtc)
    {
        var next = nowUtc.Date + ResetTimeUtc;
        if (next <= nowUtc)
        {
            next = next.AddDays(1);
        }

        return next - nowUtc;
    }
}
