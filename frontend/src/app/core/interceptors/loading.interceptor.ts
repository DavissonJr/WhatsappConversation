import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);

  // Só bloqueia a tela em ações que o usuário disparou de propósito (criar,
  // salvar, enviar, deletar...). GETs em segundo plano (polling do Inbox,
  // recarregar listas) não devem travar a interface.
  const isMutation = req.method !== 'GET';
  if (!isMutation) return next(req);

  loading.start();
  return next(req).pipe(finalize(() => loading.stop()));
};
