import { Component } from '@angular/core';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'webClient';

  constructor(private authService: AuthService) {
    this.authService.isImpersonatedAsync()
      .subscribe(x => {
        if (x) {
          window.location.reload();
        }
      });
  }

  logout() {
    this.authService.logout();
  }
}
