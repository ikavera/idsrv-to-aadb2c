import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { of } from 'rxjs/internal/observable/of';
import { environment } from 'src/environments/environment';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthorizeInterceptor implements HttpInterceptor {

  private isAllowed = true;
  private isTosAccepted = true;

  constructor(private authorize: AuthService) {
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if ((this.isAllowed && this.isTosAccepted) || this.needtoLoadAnyway(req) || this.isAssetRequest(req)) {
      return this.processRequestWithToken(this.authorize.accessToken, req, next);
    }
    return of(new HttpResponse({ status: 200, body: ((null) as any) }));
  }

  private isAssetRequest(req: HttpRequest<any>): boolean {
    return req.url.indexOf('assets/') > -1;
  }

  private needtoLoadAnyway(req: HttpRequest<any>): boolean {
    const item = req.headers.get('needLoad');
    return !!item;
  }

  handleExpiredAccess(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return of(new HttpResponse({ status: 200, body: ((null) as any) }));
  }

  // Checks if there is an access_token available in the authorize service
  // and adds it to the request in case it's targeted at the same origin as the
  // single page application.
  private processRequestWithToken(token: string | null, req: HttpRequest<any>, next: HttpHandler) {
    if (!!token && (this.isSameOriginUrl(req) || this.isApiRequest(req))) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }
    return next.handle(req)
      .pipe(tap({
        error: (err: any) => {
          if (err instanceof HttpErrorResponse) {
            if (err.status !== 401) {
              if (err.status === 403) {
                if (err.error && (err.error as string).length > 0) {
                  alert(err.error);
                } else {
                  alert('Not Authorized');
                }
              }
              return;
            }
          }
        }
      }));
  }

  private isSameOriginUrl(req: any) {
    // It's an absolute url with the same origin.
    if (req.url.startsWith(`${window.location.origin}/`)) {
      return true;
    }

    // It's a protocol relative url with the same origin.
    // For example: //www.example.com/api/Products
    if (req.url.startsWith(`//${window.location.host}/`)) {
      return true;
    }

    // It's a relative url like /api/Products
    if (/^\/[^\/].*/.test(req.url)) {
      return true;
    }

    // It's an absolute or protocol relative url that
    // doesn't have the same origin.
    return false;
  }

  private isApiRequest(req: any) {
    if (req.url.startsWith(environment.apiUrl)) {
      return true;
    }

    return false;
  }
}
