import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

const TOKEN_KEY = 'marketcore_token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });

  return next(authReq);
};
