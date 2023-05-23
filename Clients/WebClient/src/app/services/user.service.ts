import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { User } from '../models/user';
import { ImpersonateModel } from '../models/impersonateModel';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private apiUrl = environment.apiUrl + 'api/Restricted/';

  constructor(private http: HttpClient) { }

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
}
