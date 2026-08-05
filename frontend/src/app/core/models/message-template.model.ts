export type TemplateScope =
  | 'Cobranca'
  | 'Lembrete'
  | 'BoasVindas'
  | 'Orcamento'
  | 'Agendamento'
  | 'Outro';

export interface MessageTemplate {
  id: string;
  name: string;
  scope: TemplateScope;
  content: string;
  isActive: boolean;
}

export const SCOPE_LABELS: Record<TemplateScope, string> = {
  Cobranca: 'Cobrança',
  Lembrete: 'Lembrete',
  BoasVindas: 'Boas-vindas',
  Orcamento: 'Orçamento',
  Agendamento: 'Agendamento',
  Outro: 'Outro',
};
