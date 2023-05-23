import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-auth-callback',
  template: ''
})
export class AuthCallbackComponent implements OnInit {

  constructor(private authService: AuthService) { }

  async ngOnInit() {
    await this.authService.completeAuthentication();
  }

}
