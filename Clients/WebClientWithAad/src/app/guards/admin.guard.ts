import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanLoad, Route, RouterStateSnapshot, UrlSegment } from '@angular/router';
import { take } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { PermissionsService } from '../services/permissions.service';
import { UserService } from '../services/user.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
  constructor(private permissions: PermissionsService, private userService: UserService) { }
  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {
    this.userService.getCurrentUser().subscribe();
    return this.permissions.hasAdminPermAsync().pipe(take(1));
  }

}
