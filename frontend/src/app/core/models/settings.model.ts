export interface Me {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface TenantSettings {
  id: string;
  name: string;
  segment: string;
  plan: string;
}

export interface AiAgentConfig {
  agentName: string;
  systemPrompt: string;
  autoReplyEnabled: boolean;
  requireHumanApproval: boolean;
  businessHours: string;
  fallbackMessage?: string;
  hasAnthropicApiKey: boolean;
  anthropicApiKeyPreview?: string;
}

export interface AiUsageLog {
  createdAtUtc: string;
  inputTokens: number;
  outputTokens: number;
  costUsd: number;
}

export interface AiUsageSummary {
  balanceUsd: number;
  totalSpentUsd: number;
  recentUsage: AiUsageLog[];
}

export interface TeamMember {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
}
