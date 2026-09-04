import { inject, Injectable } from '@angular/core';
import { PaymentOrderStatus } from '../payment-orders/payment-order.model';
import { AppLocale, PreferencesService } from './preferences.service';

const ptBr = {
  'brand.subtitle': 'Ordens de pagamento',
  'nav.aria': 'Navegação principal',
  'nav.home': 'Início',
  'nav.newOrder': 'Nova ordem',
  'footer.tagline': 'Fluxo de pagamentos rastreável',
  'language.aria': 'Selecionar idioma',
  'language.portuguese': 'Português',
  'language.english': 'English',
  'theme.toDark': 'Ativar modo escuro',
  'theme.toLight': 'Ativar modo claro',
  'theme.darkShort': 'Escuro',
  'theme.lightShort': 'Claro',
  'route.home': 'TreasuryFlow',
  'route.newOrder': 'Nova ordem | TreasuryFlow',
  'route.editOrder': 'Editar ordem | TreasuryFlow',
  'route.orderDetails': 'Ordem de pagamento | TreasuryFlow',
  'home.eyebrow': 'Operações financeiras com rastreabilidade',
  'home.title': 'Acompanhe uma ordem do rascunho ao processamento.',
  'home.description':
    'Crie, revise e envie ordens de pagamento. O processamento acontece em segundo plano, preservando o histórico de cada decisão.',
  'home.create': 'Criar nova ordem',
  'home.lookupTitle': 'Localizar uma ordem',
  'home.lookupHelp': 'Informe o identificador recebido na criação para consultar o estado atual.',
  'home.orderId': 'ID da ordem',
  'home.search': 'Consultar',
  'home.idRequired': 'Informe o ID da ordem.',
  'home.idInvalid': 'Use um identificador UUID válido.',
  'home.flowAria': 'Fluxo de uma ordem de pagamento',
  'status.Draft': 'Rascunho',
  'status.Pending': 'Pendente',
  'status.Processing': 'Em processamento',
  'status.Completed': 'Concluída',
  'status.Failed': 'Falhou',
  'status.Cancelled': 'Cancelada',
  'statusDescription.Draft': 'Os dados ainda podem ser revisados antes do envio.',
  'statusDescription.Pending': 'A ordem aguarda o Worker iniciar o processamento.',
  'statusDescription.Processing': 'O processamento financeiro está acontecendo em segundo plano.',
  'statusDescription.Completed': 'A ordem foi processada com sucesso.',
  'statusDescription.Failed': 'O processamento terminou com falha.',
  'statusDescription.Cancelled': 'A ordem foi preservada, mas não seguirá para processamento.',
  'details.back': 'Voltar ao início',
  'details.loading': 'Carregando ordem...',
  'details.eyebrow': 'Ordem de pagamento',
  'details.dataAria': 'Dados da ordem',
  'details.amount': 'Valor da ordem',
  'details.beneficiary': 'Beneficiário',
  'details.createdAt': 'Criada em',
  'details.processedAt': 'Processada em',
  'details.notProcessed': 'Ainda não processada',
  'details.tracking': 'Esta página está acompanhando automaticamente o processamento.',
  'details.actionsAria': 'Ações disponíveis',
  'details.deleteDraft': 'Excluir rascunho',
  'details.edit': 'Editar',
  'details.submitting': 'Enviando...',
  'details.submit': 'Enviar para processamento',
  'details.cancelPending': 'Cancelar ordem',
  'details.notDisplayed': 'Não foi possível exibir a ordem.',
  'details.verifyId': 'Verifique o identificador informado.',
  'details.backHome': 'Voltar ao início',
  'details.progressAria': 'Progresso da ordem de pagamento',
  'details.currentStep': 'Etapa atual',
  'modal.deleteTitle': 'Excluir este rascunho?',
  'modal.deleteMessage':
    'A ordem não será apagada. O registro será preservado como cancelado para manter a rastreabilidade financeira.',
  'modal.cancelTitle': 'Cancelar esta ordem?',
  'modal.cancelMessage':
    'O cancelamento será concluído somente se o Worker ainda não tiver iniciado o processamento.',
  'modal.keepOrder': 'Manter ordem',
  'modal.confirmDelete': 'Excluir rascunho',
  'modal.confirmCancel': 'Cancelar ordem',
  'error.submit': 'Não foi possível enviar a ordem para processamento.',
  'error.deleteDraft': 'Não foi possível excluir o rascunho.',
  'error.cancel': 'Não foi possível cancelar a ordem.',
  'error.load': 'Não foi possível carregar a ordem de pagamento.',
  'form.back': 'Voltar',
  'form.reviewEyebrow': 'Revisar rascunho',
  'error.draftChanged':
    'O rascunho mudou de estado antes da exclusão. A ordem foi atualizada para mostrar o estado atual.',
  'error.cancelRace':
    'O processamento começou antes da confirmação do cancelamento. A ordem foi atualizada para mostrar o estado atual.',
  'form.newEyebrow': 'Nova operação',
  'form.editTitle': 'Editar ordem de pagamento',
  'form.createTitle': 'Criar ordem de pagamento',
  'form.editDescription': 'Enquanto estiver em Draft, todos os dados podem ser corrigidos.',
  'form.createDescription': 'A ordem será criada como Draft para permitir revisão antes do envio.',
  'form.loading': 'Carregando ordem...',
  'form.description': 'Descrição',
  'form.descriptionRequired': 'Informe uma descrição.',
  'form.descriptionTooLong': 'A descrição deve ter no máximo 200 caracteres.',
  'form.amount': 'Valor',
  'form.amountInvalid': 'Informe um valor maior que zero, com até duas casas decimais.',
  'form.currency': 'Moeda',
  'form.beneficiary': 'Beneficiário',
  'form.beneficiaryRequired': 'Informe o beneficiário.',
  'form.beneficiaryTooLong': 'O beneficiário deve ter no máximo 150 caracteres.',
  'form.draftWhy': 'Por que começamos em Draft?',
  'form.draftExplanation':
    'O rascunho permite revisar os dados antes de iniciar um processo financeiro assíncrono.',
  'form.cancel': 'Cancelar',
  'form.saving': 'Salvando...',
  'form.saveChanges': 'Salvar alterações',
  'form.createDraft': 'Criar rascunho',
  'error.save': 'Não foi possível salvar a ordem de pagamento.',
  'error.onlyDraftEditable': 'Somente ordens em rascunho podem ser editadas.',
} as const;

export type TranslationKey = keyof typeof ptBr;

const enUs: Record<TranslationKey, string> = {
  'brand.subtitle': 'Payment orders',
  'nav.aria': 'Main navigation',
  'nav.home': 'Home',
  'nav.newOrder': 'New order',
  'footer.tagline': 'Traceable payment flow',
  'language.aria': 'Select language',
  'language.portuguese': 'Português',
  'language.english': 'English',
  'theme.toDark': 'Enable dark mode',
  'theme.toLight': 'Enable light mode',
  'theme.darkShort': 'Dark',
  'theme.lightShort': 'Light',
  'route.home': 'TreasuryFlow',
  'route.newOrder': 'New order | TreasuryFlow',
  'route.editOrder': 'Edit order | TreasuryFlow',
  'route.orderDetails': 'Payment order | TreasuryFlow',
  'home.eyebrow': 'Traceable financial operations',
  'home.title': 'Follow an order from draft to processing.',
  'home.description':
    'Create, review, and submit payment orders. Processing happens in the background while preserving the history of every decision.',
  'home.create': 'Create new order',
  'home.lookupTitle': 'Find an order',
  'home.lookupHelp': 'Enter the identifier received at creation to view its current status.',
  'home.orderId': 'Order ID',
  'home.search': 'Search',
  'home.idRequired': 'Enter the order ID.',
  'home.idInvalid': 'Use a valid UUID identifier.',
  'home.flowAria': 'Payment order flow',
  'status.Draft': 'Draft',
  'status.Pending': 'Pending',
  'status.Processing': 'Processing',
  'status.Completed': 'Completed',
  'status.Failed': 'Failed',
  'status.Cancelled': 'Cancelled',
  'statusDescription.Draft': 'The data can still be reviewed before submission.',
  'statusDescription.Pending': 'The order is waiting for the Worker to start processing.',
  'statusDescription.Processing': 'Financial processing is happening in the background.',
  'statusDescription.Completed': 'The order was processed successfully.',
  'statusDescription.Failed': 'Processing ended with a failure.',
  'statusDescription.Cancelled': 'The order was preserved but will not be processed.',
  'details.back': 'Back to home',
  'details.loading': 'Loading order...',
  'details.eyebrow': 'Payment order',
  'details.dataAria': 'Order details',
  'details.amount': 'Order amount',
  'details.beneficiary': 'Beneficiary',
  'details.createdAt': 'Created at',
  'details.processedAt': 'Processed at',
  'details.notProcessed': 'Not processed yet',
  'details.tracking': 'This page is automatically tracking processing.',
  'details.actionsAria': 'Available actions',
  'details.deleteDraft': 'Delete draft',
  'details.edit': 'Edit',
  'details.submitting': 'Submitting...',
  'details.submit': 'Submit for processing',
  'details.cancelPending': 'Cancel order',
  'details.notDisplayed': 'The order could not be displayed.',
  'details.verifyId': 'Check the identifier provided.',
  'details.backHome': 'Back to home',
  'details.progressAria': 'Payment order progress',
  'details.currentStep': 'Current step',
  'modal.deleteTitle': 'Delete this draft?',
  'modal.deleteMessage':
    'The order will not be erased. The record will be preserved as cancelled to maintain financial traceability.',
  'modal.cancelTitle': 'Cancel this order?',
  'modal.cancelMessage':
    'Cancellation will succeed only if the Worker has not started processing yet.',
  'modal.keepOrder': 'Keep order',
  'modal.confirmDelete': 'Delete draft',
  'modal.confirmCancel': 'Cancel order',
  'error.submit': 'The order could not be submitted for processing.',
  'error.deleteDraft': 'The draft could not be deleted.',
  'error.cancel': 'The order could not be cancelled.',
  'error.load': 'The payment order could not be loaded.',
  'form.back': 'Back',
  'form.reviewEyebrow': 'Review draft',
  'form.newEyebrow': 'New operation',
  'form.editTitle': 'Edit payment order',
  'form.createTitle': 'Create payment order',
  'form.editDescription': 'All data can be corrected while the order is in Draft.',
  'error.draftChanged':
    'The draft changed state before deletion. The order was refreshed to show its current state.',
  'error.cancelRace':
    'Processing started before cancellation was confirmed. The order was refreshed to show its current state.',
  'form.createDescription': 'The order will be created as Draft for review before submission.',
  'form.loading': 'Loading order...',
  'form.description': 'Description',
  'form.descriptionRequired': 'Enter a description.',
  'form.descriptionTooLong': 'The description must have at most 200 characters.',
  'form.amount': 'Amount',
  'form.amountInvalid': 'Enter an amount greater than zero with up to two decimal places.',
  'form.currency': 'Currency',
  'form.beneficiary': 'Beneficiary',
  'form.beneficiaryRequired': 'Enter the beneficiary.',
  'form.beneficiaryTooLong': 'The beneficiary must have at most 150 characters.',
  'form.draftWhy': 'Why do we start in Draft?',
  'form.draftExplanation':
    'A draft lets you review the data before starting an asynchronous financial process.',
  'form.cancel': 'Cancel',
  'form.saving': 'Saving...',
  'form.saveChanges': 'Save changes',
  'form.createDraft': 'Create draft',
  'error.save': 'The payment order could not be saved.',
  'error.onlyDraftEditable': 'Only draft orders can be edited.',
};

const translations: Record<AppLocale, Record<TranslationKey, string>> = {
  'pt-BR': ptBr,
  'en-US': enUs,
};

const statusKeys: Record<PaymentOrderStatus, TranslationKey> = {
  Draft: 'status.Draft',
  Pending: 'status.Pending',
  Processing: 'status.Processing',
  Completed: 'status.Completed',
  Failed: 'status.Failed',
  Cancelled: 'status.Cancelled',
};

const statusDescriptionKeys: Record<PaymentOrderStatus, TranslationKey> = {
  Draft: 'statusDescription.Draft',
  Pending: 'statusDescription.Pending',
  Processing: 'statusDescription.Processing',
  Completed: 'statusDescription.Completed',
  Failed: 'statusDescription.Failed',
  Cancelled: 'statusDescription.Cancelled',
};

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly preferences = inject(PreferencesService);

  readonly locale = this.preferences.locale;

  t(key: TranslationKey): string {
    return translations[this.locale()][key];
  }

  statusLabel(status: PaymentOrderStatus): string {
    return this.t(statusKeys[status]);
  }

  statusDescription(status: PaymentOrderStatus): string {
    return this.t(statusDescriptionKeys[status]);
  }

  formatCurrency(value: number, currency: string): string {
    return new Intl.NumberFormat(this.locale(), {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale(), {
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(new Date(value));
  }
}
