import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProtectedComponent } from './components/protected/protected.component';
import { ProfileComponent } from './components/profile/profile.component';
import { UsersListComponent } from './components/users-list/users-list.component';
import { UserComponent } from './components/user/user.component';
import { MsalGuard } from '@azure/msal-angular';
import { BaseAccessGuard } from './guards/base-access.guard';
import { AdminGuard } from './guards/admin.guard';

const routes: Routes = [
  {
    path: 'protected',
    component: ProtectedComponent,
    canActivate: [MsalGuard, BaseAccessGuard]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [MsalGuard, BaseAccessGuard]
  },
  {
    path: 'user-list',
    component: UsersListComponent,
    canActivate: [MsalGuard, AdminGuard],
  },
  {
    path: 'user/:id',
    component: UserComponent,
    canActivate: [MsalGuard, AdminGuard]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }


