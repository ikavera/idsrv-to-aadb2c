import { Injectable, Inject, OnDestroy } from '@angular/core';
import { UserManager, User, WebStorageStateStore } from 'oidc-client';
import { BehaviorSubject, Subscription, Observable, ReplaySubject, of } from 'rxjs';
import { SESSION_STORAGE, StorageService } from 'ngx-webstorage-service';
import { Router } from '@angular/router';
import { map, filter } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService implements OnDestroy {

  private userManager: UserManager;
  private user: User | null = null;

  private user$ = new ReplaySubject<User | null>(1);

  private authNavStatusSource = new BehaviorSubject<boolean>(false);
  private storageKey = 'routeData';
  private userStorageKey = '';

  loginStarted = false;
  isFromConstructor = true;
  subscriptions$: Subscription[] = [];
  isInited = false;
  private authCompleted = new BehaviorSubject<boolean>(false);
  private impersonationUpd$ = new BehaviorSubject<boolean>(false);
  private impersonationErrors$ = new BehaviorSubject<string[]>([]);

  constructor(@Inject(SESSION_STORAGE) private storage: StorageService, private router: Router) {
    const baseRedirectUrl = location.protocol + '//' + location.host;
    const settings = {
      authority: environment.authUrl,
      client_id: environment.clientId,
      redirect_uri: baseRedirectUrl + environment.redirectUrl,
      post_logout_redirect_uri: baseRedirectUrl + environment.postLogoutRedirectUrl,
      response_type: environment.responseType,
      response_mode: environment.responseMode,
      scope: environment.scope,
      silent_redirect_uri: baseRedirectUrl + environment.silentRedirectUrl,
      automaticSilentRenew: environment.automaticSilentRenew,
      checkSessionInterval: environment.checkSessionInterval,
      accessTokenExpiringNotificationTime: environment.accessTokenExpiringNotificationTime,
      userStore: new WebStorageStateStore({ prefix: 'ls.', store: window.localStorage })
    };
    console.log(settings);
    this.userManager = new UserManager(settings);
    this.userManager.getUser().then(user => {
      this.user = user;
      this.isInited = true;
      this.isFromConstructor = true;
      this.authNavStatusSource.next(this.isAuthenticated());
    });
    this.initSubscriptions();
  }

  ngOnDestroy(): void {
    this.subscriptions$.forEach(s => s.unsubscribe());
  }

  get accessToken(): string | null {
    if (this.isAuthenticated()) {
      return this.user?.access_token ?? null;
    }
    return null;
  }

  getUserStorageKey() {
    if (this.userStorageKey.length > 0) {
      return this.userStorageKey;
    }
    const entries = Object.entries(localStorage);
    if (entries.length > 0) {
      entries.forEach(element => {
        if (element.length > 0 && element[0].indexOf('ls.') === 0) {
          this.userStorageKey = element[0];
          return this.userStorageKey;
        }
        return '';
      });
      return '';
    }
    return '';
  }

  isAuthenticated(): boolean {
    if (this.user != null && this.user.expired) {
      const tmp = JSON.parse(window.localStorage.getItem(this.getUserStorageKey()!)!) as User;
      if (this.user != null && tmp != null) {
        this.user.expires_at = tmp.expires_at;
        this.user.access_token = tmp.access_token;
      }
    }
    return this.user != null && !this.user.expired;
  }

  isAuthenticatedAsync() {
    return this.authNavStatusSource.asObservable();
  }

  initSubscriptions() {
    const sub = this.authNavStatusSource.asObservable().subscribe(res => {
      if (res) {
        this.authCompleted.next(true);
        this.user$.next(this.user);
      }
    });
    this.subscriptions$.push(sub);
  }

  async completeAuthentication() {
    try {
      this.user = await this.userManager.signinRedirectCallback();
      this.isFromConstructor = false;
      this.authNavStatusSource.next(this.isAuthenticated());
      this.router.navigate([this.storage.get(this.storageKey)]);
    } catch (e) {
      console.log(e);
      this.router.navigate(['']);
    }
  }

  async completeRenew() {
    await this.userManager.signinSilentCallback();
  }

  async completeLogout() {
    await this.userManager.signoutCallback();
  }

  doLogin() {
    if (this.loginStarted) {
      return;
    }
    this.loginStarted = true;
    let path = window.location.pathname.replace(environment.additionalAppPath, '');
    // for correct redirection after setting password and other when link before login is with suffix /auth-callback
    if (path === '/auth-callback') {
      path = '/';
    }
    this.storage.set(this.storageKey, path);
    const someState = { message: 'some data' };
    this.userManager.signinRedirect({ state: someState, useReplaceToNavigate: true }).then(() => {
      console.log('signinRedirect done');
    }).catch((err) => {
      console.log(err);
    });
  }

  isInRole(section: string, permission: string): boolean {
    if (!this.isAuthenticated()) {
      if (this.loginStarted || this.subscriptions$.length > 0) {
        return false;
      }
      const sub = this.authNavStatusSource.asObservable().subscribe(res => {
        if (!res && !this.loginStarted && !this.isFromConstructor) {
          this.doLogin();
        }
      });
      this.subscriptions$.push(sub);
      return false;
    }
    return this.user?.profile['role'].indexOf(section + ':' + permission) > -1;
  }

  isInRoleAsync(section: string, permission: string): Observable<boolean> {
    if (!this.isAuthenticated()) {
      this.authNavStatusSource.asObservable().subscribe(res => {
        if (!res && !this.loginStarted && this.isInited) {
          this.doLogin();
        }
      });
    }
    return this.authCompleted.pipe(
      filter(x => x === true),
      map(res => {
        if (res && this.user) {
          //@ts-ignore
          return this.user!.roles.indexOf(section + ':' + permission) > -1;
        }
        return false;
      })
    );
    // return this.authCompleted.pipe(
    //   filter(x => x === true),
    //   map(res => {
    //     if (res) {
    //       return this.user.profile.role.indexOf(section + ':' + permission) > -1;
    //     }
    //   })
    // );
  }

  getPermissions(): string[] | null {
    if (!this.isAuthenticated()) {
      if (this.loginStarted || this.subscriptions$.length > 0) {
        return null;
      }
      const sub = this.authNavStatusSource.asObservable().subscribe(res => {
        if (!res && !this.loginStarted && !this.isFromConstructor) {
          this.doLogin();
        }
      });
      this.subscriptions$.push(sub);
      return null;
    }
    return this.user?.profile['role'];
  }

  getUser(): User | null {
    return this.user;
  }

  getUserAsync() {
    return this.user$.asObservable();
  }

  logout() {
    this.userManager.signoutRedirect();
  }

  getUrlWithAccessToken(url: string) {
    return url + (url.indexOf('?') === -1 ? '?' : '&') + 'access_token=' + this.accessToken;
  }

  updateUserName(firstName: string, secondName: string) {
    if (this.user) {
      this.user.profile.given_name = firstName;
      this.user.profile.family_name = secondName;
    }
    this.user$.next(this.user);
  }

  impersonate(userId: number) {
    this.userManager.signinSilent({ acr_values: 'impersonateId:' + userId }).then(newUser => {
      this.user = newUser;
      this.authCompleted.next(true);
      console.log(this.user);
      this.user$.next(this.user);
      this.impersonationUpd$.next(true);
    }).catch(e => {
      this.handleError(e);
    });
  }

  handleError(e: any) {
    const errors = [];
    if (e.message === 'invalid_grant') {
      errors.push('User is not active');
    }
    if (e.message === 'login_required') {
      errors.push('User is not active');
    }
    if (e.message === 'Frame window timed out') {
      errors.push(e.message);
    }
    this.impersonationErrors$.next(errors);
  }

  isImpersonatedAsync() {
    return this.impersonationUpd$.asObservable();
  }

  impersonateErrors() {
    return this.impersonationErrors$.asObservable();
  }

  cleanImpersonateErrors() {
    this.impersonationErrors$.next([]);
  }
}

