using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;

namespace WhatsappCrmIA.Application.UseCases.BulkMessages;

internal static class BulkAudienceResolver
{
    public static async Task<List<Contact>> ResolveAsync(
        IApplicationDbContext db, BulkAudienceFilters filters, CancellationToken ct)
    {
        var query = db.Contacts.AsQueryable();

        if (filters.ExcludeBlocked)
            query = query.Where(c => !c.IsBlocked);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var term = filters.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                (c.Name != null && c.Name.ToLower().Contains(term)) ||
                c.PhoneNumber.Contains(term));
        }

        var contacts = await query.ToListAsync(ct);

        if (filters.NoAppointmentInLastDays is { } appointmentDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-appointmentDays);
            var contactIds = contacts.Select(c => c.Id).ToList();

            var contactsWithRecentAppointment = await db.Appointments
                .Where(a => contactIds.Contains(a.ContactId) && a.CreatedAtUtc >= cutoff)
                .Select(a => a.ContactId)
                .Distinct()
                .ToListAsync(ct);

            contacts = contacts.Where(c => !contactsWithRecentAppointment.Contains(c.Id)).ToList();
        }

        if (filters.NoConversationInLastDays is { } conversationDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-conversationDays);
            var contactIds = contacts.Select(c => c.Id).ToList();

            var contactsWithRecentConversation = await db.Conversations
                .Where(c => contactIds.Contains(c.ContactId) && c.LastMessageAtUtc >= cutoff)
                .Select(c => c.ContactId)
                .Distinct()
                .ToListAsync(ct);

            contacts = contacts.Where(c => !contactsWithRecentConversation.Contains(c.Id)).ToList();
        }

        return contacts;
    }
}
