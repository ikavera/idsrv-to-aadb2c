import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, take } from 'rxjs';
import { User } from 'src/app/models/user';
import { MsalAuthService } from 'src/app/services/msal-auth.service';
import { PermissionsService } from 'src/app/services/permissions.service';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.scss']
})
export class UserComponent implements OnInit {
  constructor(private userService: UserService,
    private authService: MsalAuthService,
    private route: ActivatedRoute,
    private permissionService: PermissionsService) { }

  public userDetails: User | null = null;
  isAdmin = false;
  private impersonateErrors$: Subscription | null = null;

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    this.userService.getUser(+userId!)
      .subscribe(x => {
        this.userDetails = x;
      });
    this.permissionService.hasAdminPermAsync()
      .subscribe(x => {
        this.isAdmin = x;
      });
  }

  impersonateUser() {
    if (this.userDetails === null) return;
    if (this.isAdmin) {
      if (this.impersonateErrors$) {
        this.impersonateErrors$.unsubscribe();
      }
      this.userService.impersonateUser(this.userDetails!.id, '').subscribe(res => {
        if (!res.isEnabled) {
          alert('user is not enabled');
          return;
        }
        this.authService.impersonate(this.userDetails!.email);
        this.impersonateErrors$ = this.authService.impersonateErrors().subscribe(x => {
          x.forEach(z => {
            alert(z);
          });
          if (x.length > 0) {
            this.authService.cleanImpersonateErrors();
          }
        });
      });
    }
  }
}
