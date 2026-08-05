namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Abstração sobre o agendador de jobs (Hangfire por baixo) — a Application
/// não precisa saber que é Hangfire especificamente.
/// </summary>
public interface IReminderScheduler
{
    /// <summary>Agenda o disparo e devolve o id do job (pra poder cancelar depois).</summary>
    string Schedule(Guid reminderId, DateTime sendAtUtc);

    void Cancel(string jobId);
}
