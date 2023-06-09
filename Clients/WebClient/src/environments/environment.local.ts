export const environment = {
    production: false,
    apiUrl: 'https://localhost:7021/',
    authUrl: 'https://localhost:44335/',
    clientId: 'angular_spa',
    redirectUrl: '/WebClient/auth-callback',
    silentRedirectUrl: '/WebClient/silent-refresh',
    responseType: 'code',
    scope: 'profile openid api1',
    postLogoutRedirectUrl: '/WebClient/signout-callback',
    responseMode: 'query',
    automaticSilentRenew: true,
    checkSessionInterval: 20000,
    accessTokenExpiringNotificationTime: 15,
    additionalAppPath: '/WebClient',
};
