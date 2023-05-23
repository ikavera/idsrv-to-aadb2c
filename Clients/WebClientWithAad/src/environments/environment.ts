export const environment = {
    production: false,
    apiUrl: 'https://supercompany5.com/WebApiWithAad/',
    azureTenantId: 'https://scomp5.b2clogin.com/scomp5.onmicrosoft.com',
    azureClintId: 'd0be451f-0926-4540-8ecb-e7ac5a124710',
    azureRedirectUrl: 'https://supercompany5.com/WebClientWithAad/',
    azureScope: 'https://scomp5.onmicrosoft.com/01082ab9-d752-477f-84b4-aba3d0e6ee53/webapi',
    signInPolicy: 'B2C_1A_B2C_1_DEV_WEBAPI',
    knownAuthorities: ['scomp5.b2clogin.com'],
    impersonatePolicy: 'B2C_1A_B2C_1_DEV_WEBAPI_Impersonation_LocalDev',
    accessTokenExpiringNotificationTime: 15,
    additionalAppPath: '/WebApiWithAad',
    customErrorPrefix: 'CUSTOM_ERROR:',
    stackTraceSuffix: '$STACK_TRACE:$'
};
