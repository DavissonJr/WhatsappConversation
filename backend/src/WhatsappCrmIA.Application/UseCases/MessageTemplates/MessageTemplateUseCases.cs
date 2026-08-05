using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.MessageTemplates;

public record GetMessageTemplatesQuery : IRequest<IReadOnlyList<MessageTemplateDto>>;

public class GetMessageTemplatesHandler
    : IRequestHandler<GetMessageTemplatesQuery, IReadOnlyList<MessageTemplateDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMessageTemplatesHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MessageTemplateDto>> Handle(
        GetMessageTemplatesQuery request, CancellationToken ct)
    {
        return await _db.MessageTemplates
            .OrderBy(t => t.Scope).ThenBy(t => t.Name)
            .Select(t => new MessageTemplateDto(t.Id, t.Name, t.Scope.ToString(), t.Content, t.IsActive))
            .ToListAsync(ct);
    }
}

public record CreateMessageTemplateCommand(string Name, TemplateScope Scope, string Content)
    : IRequest<MessageTemplateDto>;

public class CreateMessageTemplateHandler
    : IRequestHandler<CreateMessageTemplateCommand, MessageTemplateDto>
{
    private readonly IApplicationDbContext _db;
    public CreateMessageTemplateHandler(IApplicationDbContext db) => _db = db;

    public async Task<MessageTemplateDto> Handle(CreateMessageTemplateCommand request, CancellationToken ct)
    {
        var template = new MessageTemplate
        {
            Name = request.Name,
            Scope = request.Scope,
            Content = request.Content,
            IsActive = true
        };
        _db.MessageTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return new MessageTemplateDto(template.Id, template.Name, template.Scope.ToString(), template.Content, template.IsActive);
    }
}

public record UpdateMessageTemplateCommand(Guid Id, string Name, TemplateScope Scope, string Content, bool IsActive)
    : IRequest<bool>;

public class UpdateMessageTemplateHandler : IRequestHandler<UpdateMessageTemplateCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public UpdateMessageTemplateHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateMessageTemplateCommand request, CancellationToken ct)
    {
        var template = await _db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (template is null) return false;

        template.Name = request.Name;
        template.Scope = request.Scope;
        template.Content = request.Content;
        template.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public record DeleteMessageTemplateCommand(Guid Id) : IRequest<bool>;

public class DeleteMessageTemplateHandler : IRequestHandler<DeleteMessageTemplateCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public DeleteMessageTemplateHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteMessageTemplateCommand request, CancellationToken ct)
    {
        var template = await _db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (template is null) return false;

        _db.MessageTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
