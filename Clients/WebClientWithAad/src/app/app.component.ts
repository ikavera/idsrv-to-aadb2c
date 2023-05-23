import { Component } from '@angular/core';
import { MsalAuthService } from './services/msal-auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'WebClientWithAad';

  constructor(private authService: MsalAuthService) {
    this.authService.isImpersonatedAsync()
      .subscribe();
  }

  logout() {
    this.authService.logout();
  }
}
