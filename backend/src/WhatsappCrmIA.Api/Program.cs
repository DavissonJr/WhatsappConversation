using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Application.UseCases.Messaging;
using WhatsappCrmIA.Infrastructure.Persistence;
using WhatsappCrmIA.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Serviços de aplicação ----
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ProcessIncomingMessageCommand).Assembly));

// ---- Persistência ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ---- Tenant atual (resolvido a partir do JWT) ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();

// ---- Integrações externas ----
builder.Services.AddHttpClient<IWhatsAppGateway, EvolutionApiWhatsAppGateway>();
builder.Services.AddHttpClient<IAiAgentService, ClaudeAiAgentService>();

// ---- Jobs agendados (lembretes) ----
builder.Services.AddHangfire(cfg => cfg
    .UsePostgreSqlStorage(opt =>
        opt.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// ---- Auth (JWT) ----
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        // Em dev local sem provedor de identidade configurado, ajuste conforme necessário.
    });
builder.Services.AddAuthorization();

// ---- CORS para o Angular ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/jobs"); // proteger com auth em produção
app.MapControllers();

app.Run();
