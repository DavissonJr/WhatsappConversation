# WhatsApp CRM com IA

SaaS para pequenas empresas (clínicas, oficinas, advogados, imobiliárias) não perderem
clientes: conecta ao WhatsApp, responde automaticamente com IA, cria propostas,
agenda retornos e envia lembretes.

## Stack

- **Backend**: .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), EF Core + PostgreSQL, MediatR (CQRS), Hangfire (jobs/lembretes), SignalR (tempo real)
- **Frontend**: Angular 18 (standalone components, Signals)
- **WhatsApp**: [Evolution API](https://doc.evolution-api.com) (self-hosted, não-oficial — Baileys). Trocar para WhatsApp Cloud API no futuro exige apenas uma nova implementação de `IWhatsAppGateway`, sem tocar no domínio.
- **IA**: Claude (Anthropic) via `ClaudeAiAgentService`
- **Infra**: Docker Compose (Postgres, Redis, Evolution API, API, Frontend)

## Estrutura

```
whatsapp-crm-ia/
├── backend/
│   ├── WhatsappCrmIA.sln
│   └── src/
│       ├── WhatsappCrmIA.Domain/          # entidades e enums, sem dependências externas
│       ├── WhatsappCrmIA.Application/     # casos de uso (MediatR), interfaces
│       ├── WhatsappCrmIA.Infrastructure/  # EF Core, Evolution API, Claude
│       └── WhatsappCrmIA.Api/             # controllers, Program.cs, Dockerfile
├── frontend/                               # Angular 18 (Inbox, etc.)
├── docker-compose.yml
└── .env.example
```

## Como rodar localmente

### 1. Pré-requisitos
- .NET 8 SDK
- Node.js 20+
- Docker e Docker Compose

### 2. Configurar variáveis de ambiente
```bash
cp .env.example .env
# preencha ANTHROPIC_API_KEY e EVOLUTION_API_KEY
```

### 3. Subir a stack completa
```bash
docker compose up --build
```
- API: http://localhost:5000/swagger
- Frontend: http://localhost:4200
- Evolution API: http://localhost:8081
- Hangfire dashboard: http://localhost:5000/jobs

### 4. Gerar a migration do EF Core (schema mudou — recomeçar do zero)

Como o schema mudou bastante nesta versão (usuários, múltiplos números de WhatsApp,
modelos de mensagem), a forma mais simples é apagar qualquer migration antiga e o
volume do Postgres, e recomeçar do zero:

```bash
cd backend
rm -rf src/WhatsappCrmIA.Infrastructure/Migrations   # se você já tinha gerado antes
dotnet ef migrations add InitialCreate \
  --project src/WhatsappCrmIA.Infrastructure \
  --startup-project src/WhatsappCrmIA.Api
```

No Windows/PowerShell (uma linha só, sem `\`):
```powershell
dotnet ef migrations add InitialCreate --project src/WhatsappCrmIA.Infrastructure --startup-project src/WhatsappCrmIA.Api
```

### 5. Subir a stack (resete o volume do Postgres se já tinha subido antes)
```bash
docker compose down -v
docker compose up --build
```

### 6. Aplicar a migration no banco
```bash
dotnet ef database update \
  --project src/WhatsappCrmIA.Infrastructure \
  --startup-project src/WhatsappCrmIA.Api \
  --connection "Host=localhost;Port=5432;Database=whatsappcrmia;Username=postgres;Password=postgres"
```

### 7. Criar sua conta
Abra http://localhost:4200/register e crie sua empresa (isso já cria o tenant, o
usuário owner e uma config padrão de IA). Depois disso você é redirecionado
direto para o Inbox, já autenticado.

### 8. Conectar um número de WhatsApp
Na tela **Números WhatsApp** do painel, clique em "Conectar número", dê um nome
(ex: "Recepção") e escaneie o QR code que aparece. Depois de conectado, configure
o webhook da instância na Evolution API para apontar para:
```
http://<seu-host>:5000/webhook/evolution/{tenantId}/{instanceName}
```
(o `tenantId` e `instanceName` você pode conferir no Swagger ou no banco por enquanto —
uma tela para copiar isso automaticamente é um próximo passo natural).

## Fluxo principal (já implementado no backend)

```
Cliente manda mensagem no WhatsApp
        │
        ▼
Evolution API dispara webhook → WebhookController
        │
        ▼
ProcessIncomingMessageCommand (MediatR)
   ├─ salva Contact/Conversation/Message
   ├─ chama Claude (system prompt do tenant) → resposta + intenção detectada
   ├─ se precisar aprovação humana → status WaitingHuman, para aqui
   └─ senão → envia resposta via Evolution API + salva Message outbound
```

## Roadmap sugerido (próximos passos)

Já implementado nesta versão: autenticação (registro/login com JWT), múltiplos
números de WhatsApp por tenant, modelos de mensagem por escopo, CRUD de conversas.

1. **Configurar webhook automaticamente** — hoje o `tenantId`/`instanceName` do
   webhook precisam ser copiados manualmente; o backend pode configurar isso
   sozinho ao criar a conexão (chamando a Evolution API para setar o webhook URL)
2. **Usar os modelos de mensagem no Inbox** — botão de "inserir modelo" na caixa
   de resposta do atendente, com substituição de variáveis (`{nome}`, etc.)
3. **Propostas**: endpoint que aciona `IAiAgentService.GenerateProposalDraftAsync`
   e tela de revisão/envio
4. **Agendamentos + lembretes**: criar `Appointment`, agendar `Reminder` como job
   Hangfire que dispara `IWhatsAppGateway.SendTextMessageAsync` na hora certa,
   podendo usar um `MessageTemplate` do escopo "Lembrete"
5. **Billing**: integração com Stripe ou Mercado Pago, limites por `PlanTier`
6. **Tela de configuração do agente de IA** (editar o `SystemPrompt`, ativar/desativar
   auto-resposta, ativar aprovação humana) — hoje só existe um valor padrão criado no registro
7. **SignalR** no Inbox para mensagens chegarem em tempo real, sem precisar recarregar

## Segurança — antes de ir para produção

- Troque `Jwt:Secret` no `appsettings.json` por um valor forte e único (hoje está
  com um placeholder visível)
- O `AUTHENTICATION_API_KEY` da Evolution API também está com valor placeholder —
  troque no `docker-compose.yml`
- Ative HTTPS e configure `Cors:AllowedOrigin` para o domínio real

## Notas de arquitetura

- **Multi-tenancy**: hoje via `TenantId` em cada linha + *global query filter* no EF Core (`AppDbContext.OnModelCreating`). Simples de implementar e suficiente até uma escala considerável; migrar para schema-per-tenant só se necessário.
- **Trocar Evolution API pela Cloud API oficial**: implemente uma nova classe `WhatsAppCloudApiGateway : IWhatsAppGateway` e troque o registro no `Program.cs` — nada no domínio ou nos casos de uso muda.
- **Trocar Claude por outro LLM**: mesma lógica, nova classe implementando `IAiAgentService`.
