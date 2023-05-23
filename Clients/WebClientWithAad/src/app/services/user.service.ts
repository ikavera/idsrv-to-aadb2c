import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, of, shareReplay, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { User } from '../models/user';
import { ImpersonateModel } from '../models/impersonateModel';
import { MsalAuthService } from './msal-auth.service';
import { ErrorsService } from './error-service.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private apiUrl = environment.apiUrl + 'api/Restricted/';

  private currentUser: User | null = null;
  private userDetailsInProgress = false;
  private userDetailsRequest: Observable<User> = of(new User());

  constructor(private http: HttpClient,
    private authService: MsalAuthService,
    private errorService: ErrorsService) { }

  getProfile(): Observable<User> {
    return this.http.get<User>(this.apiUrl + 'GetUserProfile');
  }

  getUsersList(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl + 'GetUsersList');
  }

  getUser(userId: number): Observable<User> {
    return this.http.get<User>(this.apiUrl + 'GetUser', { params: { userId } });
  }

  impersonateUser(userId: number, clientId: string) {
    return this.http.get<ImpersonateModel>(this.apiUrl + 'ImpersonateUser', {
      params: {
        userId,
        clientId
      },
    });
  }

  getCurrentUser() {
    if (!this.currentUser) {
      if (this.userDetailsInProgress) {
        return this.userDetailsRequest;
      }
      const userData = this.authService.getUser();
      if (!userData || !userData.account) {
        return of(new User());
      }
      this.userDetailsRequest = this.http.get<User>(this.apiUrl + 'GetUserProfile')
        .pipe(
          catchError(e => this.errorService.handleErrorWithNotification(e)),
          tap(x => { this.currentUser = x; this.userDetailsInProgress = false; }),
          shareReplay({ refCount: true }));
      this.userDetailsInProgress = true;
      return this.userDetailsRequest;
    }
    return of(this.currentUser);
  }
}
