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

### 4. Gerar a primeira migration do EF Core
Este ambiente de scaffolding não tinha o SDK do .NET disponível, então **as migrations
ainda não foram geradas**. Rode localmente, dentro de `backend/`:

```bash
dotnet tool install --global dotnet-ef   # se ainda não tiver
cd backend
dotnet ef migrations add InitialCreate \
  --project src/WhatsappCrmIA.Infrastructure \
  --startup-project src/WhatsappCrmIA.Api
dotnet ef database update \
  --project src/WhatsappCrmIA.Infrastructure \
  --startup-project src/WhatsappCrmIA.Api
```

### 5. Conectar um número de WhatsApp (MVP com Evolution API)
1. Crie uma instância: `POST http://localhost:8081/instance/create` com header `apikey`
2. Busque o QR code: `GET http://localhost:8081/instance/connect/{instanceName}`
3. Escaneie com o WhatsApp do cliente
4. Configure o webhook da instância para: `POST http://localhost:5000/webhook/evolution/{tenantId}`

No MVP isso deve virar uma tela no painel Angular (wizard de conexão com QR code
renderizado), hoje é feito via chamadas diretas à Evolution API.

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

1. **Migrations + seed** de um tenant de teste
2. **Autenticação**: endpoint de login/registro de tenant (Identity ou provedor externo tipo Auth0/Keycloak) emitindo JWT com a claim `tenant_id`
3. **CRUD de conversas** no `Api` (hoje só existe o webhook de entrada — falta `GET /api/conversations`, `POST /api/conversations/{id}/messages` usados pelo frontend)
4. **Tela de conexão WhatsApp** no Angular (QR code + status da instância)
5. **Propostas**: endpoint que aciona `IAiAgentService.GenerateProposalDraftAsync` e tela de revisão/envio
6. **Agendamentos + lembretes**: criar `Appointment`, agendar `Reminder` como job Hangfire que dispara `IWhatsAppGateway.SendTextMessageAsync` na hora certa
7. **Billing**: integração com Stripe ou Mercado Pago, limites por `PlanTier`
8. **Multi-tenant onboarding**: fluxo de cadastro self-service para o cliente final (clínica, oficina etc.)

## Notas de arquitetura

- **Multi-tenancy**: hoje via `TenantId` em cada linha + *global query filter* no EF Core (`AppDbContext.OnModelCreating`). Simples de implementar e suficiente até uma escala considerável; migrar para schema-per-tenant só se necessário.
- **Trocar Evolution API pela Cloud API oficial**: implemente uma nova classe `WhatsAppCloudApiGateway : IWhatsAppGateway` e troque o registro no `Program.cs` — nada no domínio ou nos casos de uso muda.
- **Trocar Claude por outro LLM**: mesma lógica, nova classe implementando `IAiAgentService`.
