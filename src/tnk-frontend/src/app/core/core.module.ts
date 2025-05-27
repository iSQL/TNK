import { NgModule, Optional, SkipSelf } from '@angular/core';
import { CommonModule } from '@angular/common';
// No need to import AuthService here if it's providedIn: 'root'

@NgModule({
  declarations: [],
  imports: [
    CommonModule
  ],
  providers: [
    // AuthTokenInterceptor would be provided in app.config.ts
    // AuthService is providedIn: 'root'
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parentModule?: CoreModule) {
    if (parentModule) {
      throw new Error('CoreModule has already been loaded. Import Core modules in the AppModule only.');
    }
  }
}