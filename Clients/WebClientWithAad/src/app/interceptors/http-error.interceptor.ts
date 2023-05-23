import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ErrorsService } from '../services/error-service.service';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(private readonly injector: Injector) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError(error => {
        if(error && error.status === 403){
          if (error.error && (error.error as string).length > 0) {
            alert(error.error);
          } else {
            alert('Not Authorized');
          }
        }
        const errorService = this.injector.get(ErrorsService)
        errorService.handleErrorWithNotification(error);
        return throwError(error);
      })
    );
  }
}
