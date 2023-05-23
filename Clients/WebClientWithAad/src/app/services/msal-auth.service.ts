import { HttpClient } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { EventMessage, EventType, SsoSilentRequest } from '@azure/msal-browser';
import { BehaviorSubject, catchError, filter, forkJoin, map, Observable, of, ReplaySubject, share, Subject, Subscription, take, takeUntil, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CurrentUser } from '../models/currentUser';
import { ErrorsService } from './error-service.service';

@Injectable({
    providedIn: 'root'
})
export class MsalAuthService implements OnDestroy {
    loginStarted = false;
    rolesStarted = false;
    loginFinished = false;
    private readonly _destroying$ = new Subject<void>();
    private readonly invalidGrantErrorCode = 'AADB2C90080';
    private readonly interactionRequiredErrorCode = 'AADB2C90077';
    private user: CurrentUser | null = null;
    private user$ = new ReplaySubject<CurrentUser | null>(1);
    private authNavStatusSource = new BehaviorSubject<boolean>(false);
    subscriptions$: Subscription[] = [];
    private apiUrl = environment.apiUrl + 'api/Restricted/';
    private authCompleted = new BehaviorSubject<boolean>(false);
    private userRoles: string[] = [];
    private userId = 0;
    impersonateFinished = false;
    private impersonationUpd$ = new BehaviorSubject<boolean>(false);
    private impersonationErrors$ = new BehaviorSubject<string[]>([]);

    constructor(private authService: MsalService,
        private broadcastService: MsalBroadcastService,
        private http: HttpClient,
        private errorService: ErrorsService) {
        this.initSubscriptions();
    }

    isAuthenticated() {
        return this.authService.instance.getActiveAccount() !== null;
    }

    isTokenExpired() {
        const account = this.authService.instance.getActiveAccount();
        if (account !== null && account.idTokenClaims) {
            const expirationDate = new Date(account.idTokenClaims!['exp']! * 1000);
            return expirationDate <= new Date();
        }
        return true;
    }

    needToSilentRefresh() {
        const account = this.authService.instance.getActiveAccount();
        if (account !== null && account.idTokenClaims) {
            const expirationDate = new Date((account.idTokenClaims!['exp']! + environment.accessTokenExpiringNotificationTime) * 1000);
            return expirationDate <= new Date();
        }
        return true;
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
        this.broadcastService.msalSubject$
            .pipe(
                filter((msg: EventMessage) => msg.eventType === EventType.ACQUIRE_TOKEN_FAILURE),
                takeUntil(this._destroying$)
            )
            .subscribe((result: EventMessage) => {
                const item = (result.error as unknown as { errorMessage: string });
                if (item.errorMessage.indexOf(this.interactionRequiredErrorCode) != -1) {
                    this.authService.instance.setActiveAccount(null);
                    this.logout();
                } else if (item.errorMessage.indexOf(this.invalidGrantErrorCode) == -1) {
                    this.authService.loginRedirect();
                } else {
                    this.logout();
                }
            });
        this.broadcastService.msalSubject$
            .pipe(
                filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS || msg.eventType === EventType.ACQUIRE_TOKEN_SUCCESS),
                takeUntil(this._destroying$)
            )
            .subscribe((result: EventMessage) => {
                const acc = this.authService.instance.getActiveAccount();
                const impersonation = localStorage.getItem('impersonationActive');
                //@ts-ignore
                if (!impersonation && result.payload['authority'] &&
                    //@ts-ignore
                    result.payload['authority'].toLowerCase().indexOf(environment.impersonatePolicy.toLowerCase()) > -1 &&
                    //@ts-ignore
                    acc?.idTokenClaims && acc!.idTokenClaims['idp'][0] !== 'impersonation') {
                    this.authService.instance.setActiveAccount(null);
                    const allAccs = this.authService.instance.getAllAccounts();
                    allAccs[1].tenantId = allAccs[0].tenantId;
                    //@ts-ignore
                    this.authService.instance.setActiveAccount(result.payload['account']);
                    this.rolesStarted = false;
                    this.resetUserObject();
                    localStorage.setItem('impersonationActive', 'true');
                    this.impersonateFinished = true;
                }
                if (acc === null) {
                    const allAccs = this.authService.instance.getAllAccounts();
                    if (allAccs && allAccs.length > 0) {
                        this.authService.instance.setActiveAccount(allAccs[0]);
                        this.resetUserObject();
                    }
                } else if (!this.user) {
                    this.resetUserObject();
                }
                if (this.user) {
                    const item = (result.payload as unknown as { accessToken: string });
                    this.user.accessToken = item.accessToken;
                }
            });
        this.broadcastService.msalSubject$
            .pipe(
                filter((msg: EventMessage) => msg.eventType === EventType.SSO_SILENT_FAILURE || msg.eventType === EventType.LOGIN_FAILURE),
                takeUntil(this._destroying$)
            )
            .subscribe((result: EventMessage) => {
                const item = (result.error as unknown as { errorMessage: string });
                if (item.errorMessage?.indexOf('AADB2C90118') == -1) {
                    this.authService.instance.ssoSilent(this.getSilentRequest(false)).then(x => console.log(x));
                }
            });
    }

    getSilentRequest(skipHint: boolean) {
        let silentRequest: SsoSilentRequest = {
            scopes: [environment.azureScope],
            loginHint: this.getUser()?.account?.email
        };
        if (skipHint) {
            silentRequest = {
                scopes: [environment.azureScope],
            };
        }
        return silentRequest;
    }

    logout() {
        localStorage.removeItem('impersonationActive');
        this.authService.logoutRedirect();
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
        this.authService.loginRedirect();
    }

    resetUserObject() {
        if (this.rolesStarted) return;
        this.rolesStarted = true;
        this.user = {
            account: this.authService.instance.getActiveAccount(),
            roles: [],
            id: 0,
            tosAccepted: false,
            accessToken: ''
        };
        this.authService.handleRedirectObservable().subscribe({
            next: () => {
                forkJoin([this.loadUserRoles(), this.loadUserId()])
                    .pipe(takeUntil(this._destroying$))
                    .subscribe(([rolesResponse, userIdResponse]) => {
                        this.user = {
                            account: this.authService.instance.getActiveAccount(),
                            roles: rolesResponse,
                            id: userIdResponse,
                            tosAccepted: true,
                            accessToken: ''
                        };
                        this.authNavStatusSource.next(this.isAuthenticated());

                        this.impersonationUpd$.pipe(take(1)).subscribe(x => {
                            if (this.impersonateFinished === true && !x) {
                                this.impersonationUpd$.next(true);
                            }
                        })
                    });
            },
            error: err => {
                console.log(err);
            }
        });
    }

    isInRole(section: string, permission: string): boolean {
        if (!this.isAuthenticated()) {
            if (this.loginStarted || this.subscriptions$.length > 0) {
                return false;
            }
            const sub = this.authNavStatusSource.asObservable().subscribe(res => {
                if (!res && !this.loginStarted) {
                    this.doLogin();
                }
            });
            this.subscriptions$.push(sub);
            return false;
        }
        if (!this.user) return false;
        return this.user.roles.indexOf(section + ':' + permission) > -1;
    }

    isInRoleAsync(section: string, permission: string): Observable<boolean> {
        return this.authCompleted.pipe(
            filter(x => x === true),
            map(res => {
                if (res) {
                    return this.user!.roles.indexOf(section + ':' + permission) > -1;
                }
                return false;
            })
        );
    }

    getPermissions(): string[] | null {
        if (!this.isAuthenticated()) {
            if (this.loginStarted || this.subscriptions$.length > 0) {
                return null;
            }
            const sub = this.authNavStatusSource.asObservable().subscribe(res => {
                if (!res && !this.loginStarted) {
                    this.doLogin();
                }
            });
            this.subscriptions$.push(sub);
            return null;
        }
        return this.user!.roles;
    }

    loadUserRoles() {
        if (this.userRoles && this.userRoles.length > 0) {
            return of(this.userRoles);
        }
        return this.http.get<string[]>(this.apiUrl + 'GetCurrentUserRoles')
            .pipe(
                catchError(e => this.errorService.handleErrorWithNotification(e)),
                tap(x => this.userRoles = x),
                takeUntil(this._destroying$),
                share());
    }

    loadUserId() {
        if (this.userId) {
            return of(this.userId);
        }
        return this.http.get<number>(this.apiUrl + 'GetCurrentUserId')
            .pipe(
                catchError(e => this.errorService.handleErrorWithNotification(e)),
                tap(x => this.userId = x),
                takeUntil(this._destroying$),
                share());
    }

    getUser(): CurrentUser | null {
        if (!this.user || !this.user.account) {
            this.resetUserObject();
        }
        return this.user;
    }

    getUserAsync() {
        return this.user$.asObservable();
    }

    async completeAuthentication() {
        try {
            this.authNavStatusSource.next(this.isAuthenticated());
        } catch (e) {
            console.log(e);
        }
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

    impersonate(userName: string) {
        //@ts-ignore
        this.authService.instance['config'].auth.authority = environment.azureTenantId + '/' + environment.impersonatePolicy;
        const impersonateRequest = {
            scopes: [environment.azureScope],
            authority: environment.azureTenantId + '/' + environment.impersonatePolicy,
            extraQueryParameters: { ['targetEmail']: userName }
        };
        this.authService.loginRedirect(impersonateRequest);
    }

    updateUserName(firstName: string, secondName: string) {
        this.user!.account.given_name = firstName;
        this.user!.account.family_name = secondName;
        this.user$.next(this.user);
    }

    getUrlWithAccessToken(url: string) {
        const tokenName = 'access_token=';
        if (url.length === 0 || url.indexOf(tokenName) > -1) return url;
        return url + (url.indexOf('?') === -1 ? '?' : '&') + tokenName + this.user?.accessToken;
    }

    handleError(e: any) {
        console.log(e);
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

    ngOnDestroy(): void {
        this.subscriptions$.forEach(s => s.unsubscribe());
        this._destroying$.next();
        this._destroying$.complete();
    }
}

