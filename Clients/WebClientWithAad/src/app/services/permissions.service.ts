import { Injectable } from '@angular/core';
import { combineLatest, Observable, map } from 'rxjs';
import { MsalAuthService } from './msal-auth.service';
import { UserPermissionsSectionsNames } from '../constants/userPermissionsSectionsNames';
import { UserPermissionsNames } from '../constants/userPermissionsNames';

@Injectable({
  providedIn: 'root'
})
export class PermissionsService {

  constructor(private authService: MsalAuthService) { }

  hasAnyRoleAsync(): Observable<boolean> {
    return combineLatest(
      [
        this.authService.isInRoleAsync(UserPermissionsSectionsNames.Web, UserPermissionsNames.Admin),
        this.authService.isInRoleAsync(UserPermissionsSectionsNames.Web, UserPermissionsNames.User)
      ]
    ).pipe(
      map(data => {
        return data.some(x => x === true);
      })
    );
  }

  hasAdminPermAsync() {
    return this.authService.isInRoleAsync(UserPermissionsSectionsNames.Web, UserPermissionsNames.Admin);
  }

  hasSystemPerm() {
    return this.authService.isInRole(UserPermissionsSectionsNames.Web, UserPermissionsNames.Admin);
  }

  hasUserPerm() {
    return this.hasSystemPerm() || this.authService.isInRole(UserPermissionsSectionsNames.Web, UserPermissionsNames.User);
  }

  hasUserPermAsync() {
    return combineLatest(
      [
        this.hasAdminPermAsync(),
        this.authService.isInRoleAsync(UserPermissionsSectionsNames.Web, UserPermissionsNames.User)
      ]
    ).pipe(
      map(data => {
        return data.some(x => x === true);
      })
    );
  }
}
