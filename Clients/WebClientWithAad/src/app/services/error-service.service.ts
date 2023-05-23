import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { throwError } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ErrorsService {
    constructor() { }

    handleErrorWithNotification(error: HttpErrorResponse, customMessage: string | null = null) {
        let isBackendCustomError = false;
        if (error.error instanceof ErrorEvent) {
            // A client-side or network error occurred. Handle it accordingly.
            console.error('Error', error.error.message);
        } else {
            console.log(error);
            if (typeof error.error === "string") {
                isBackendCustomError = error.error.startsWith(environment.customErrorPrefix);
                const msg = this.getErrorString(error, isBackendCustomError);
                console.error(msg);
            }
        }
        if (error.status === 500) {
            if (isBackendCustomError) {
                this.showBackendNotification(error);
            } else {
                this.showFrontendNotification(customMessage, error);
            }
        } else if (error.status === 404) {
            this.showFrontendNotification('Not Found', error);
        } else if (error.status === 400 && customMessage !== null) {
            this.showFrontendNotification(customMessage, error);
        } else if (error.status !== 403) {
            this.showDefaultMessage(error);
        }

        // return an observable with a user-facing error message
        return throwError(() => new Error('Something bad happened'));
    }

    private showDefaultMessage(error: HttpErrorResponse) {
        alert(this.getMethodNameWithStatusFromHttpResponse(error));
    }

    private getErrorString(error: HttpErrorResponse, isBackendCustomError: boolean) {
        let msg = `response ${error.status}, `
            + `body: `;
        let stackTrace = '';

        let body = isBackendCustomError
            ? error.error.substr(environment.customErrorPrefix.length)
            : error.error;

        if (!environment.production) {
            const stackTracePosition = body.indexOf(environment.stackTraceSuffix);
            if (stackTracePosition > -1) {
                stackTrace = body.substr(stackTracePosition + environment.stackTraceSuffix.length);
                body = body.substr(0, stackTracePosition);
            }
        }
        msg += `${body}`;
        if (stackTrace) {
            msg += `Stack: ${stackTrace}`;
        }
        return msg;
    }

    showBackendNotification(error: HttpErrorResponse) {
        let msg = error.error.substr(environment.customErrorPrefix.length);
        if (!environment.production) {
            const stackTracePosition = msg.indexOf(environment.stackTraceSuffix);
            if (stackTracePosition > -1) {
                msg = msg.substr(0, stackTracePosition);
            }
        }
        alert(msg);
    }

    showFrontendNotification(customMessage: string | null, error: HttpErrorResponse) {
        if (customMessage) {
            alert(customMessage);
        } else {
            this.showDefaultMessage(error);
        }
    }

    private getMethodNameWithStatusFromHttpResponse(error: HttpErrorResponse) {
        if (!error || !error.url) {
            return 'Unknown';
        }
        const startSymb = error.url.lastIndexOf('/') + 1;
        const lastSymb = error.url.indexOf('?');
        const method = lastSymb === -1
            ? error.url.substring(startSymb)
            : error.url.substring(startSymb, lastSymb);
        return method + " " + error.status;
    }
}

