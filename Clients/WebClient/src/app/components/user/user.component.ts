import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { take } from 'rxjs';
import { User } from 'src/app/models/user';
import { AuthService } from 'src/app/services/auth.service';
import { UserService } from 'src/app/services/user.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.scss']
})
export class UserComponent implements OnInit {
  constructor(private userService: UserService,
    private authService: AuthService,
    private route: ActivatedRoute) { }

  public userDetails: User | null = null;

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    this.userService.getUser(+userId!)
      .subscribe(x => {
        this.userDetails = x;
      });
  }

  impersonateUser() {
    if (this.userDetails === null) return;
    this.userService.impersonateUser(this.userDetails.id, environment.clientId).subscribe(res => {
      this.authService.impersonate(res.userId);
      this.authService.impersonateErrors().pipe(take(1)).subscribe(x => {
        x.forEach(z => {
          alert(z);
        })
      });
      this.authService.isImpersonatedAsync().subscribe(x => {
        if (x) {
          window.location.reload()
        }
      });
    });
  }
}
