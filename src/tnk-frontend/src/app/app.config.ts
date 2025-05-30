// src/app/app.config.ts
import { ApplicationConfig, importProvidersFrom, isDevMode } from '@angular/core';
import { PreloadAllModules, provideRouter, withComponentInputBinding, withPreloading, withDebugTracing } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { HttpClient } from '@angular/common/http';

// Import the new functional interceptor+
import { authTokenFnInterceptor } from '@core/interceptors/auth-token-fn.interceptor'; // Adjust path as needed

// Required for i18n
export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(
      routes,
      withComponentInputBinding(),
      withPreloading(PreloadAllModules)
      // Comment out or remove withDebugTracing() for production if it was added for debugging
      // withDebugTracing() 
    ),
    provideAnimationsAsync(), // For Angular Material/CDK animations if used elsewhere, or general animations
    
    // Configure HttpClient with the functional interceptor
    provideHttpClient(
      withInterceptors([
        authTokenFnInterceptor // Use the functional interceptor here
      ])
    ),
    
    // ngx-translate configuration
    importProvidersFrom(TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      },
      defaultLanguage: 'sr'
    })),
    // Service Worker (if you plan to use it)
    // provideServiceWorker('ngsw-worker.js', {
    //   enabled: !isDevMode(),
    //   registrationStrategy: 'registerWhenStable:30000'
    // })
  ],
};
