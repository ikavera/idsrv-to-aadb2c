import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { AuthCallbackComponent } from './components/auth/auth-callback/auth-callback.component';
import { SilentRefreshComponent } from './components/auth/silent-refresh/silent-refresh.component';
import { SignoutCallbackComponent } from './components/auth/signout-callback/signout-callback.component';
import { ProtectedComponent } from './components/protected/protected.component';
import { ProfileComponent } from './components/profile/profile.component';
import { UsersListComponent } from './components/users-list/users-list.component';
import { UserComponent } from './components/user/user.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'signin-oidc', component: AuthCallbackComponent },
  { path: 'silent-refresh', component: SilentRefreshComponent },
  { path: 'signout-callback', component: SignoutCallbackComponent },
  {
    path: 'protected',
    component: ProtectedComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'user-list',
    component: UsersListComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'user/:id',
    component: UserComponent
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }


