import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { registerLicense } from '@syncfusion/ej2-base';
registerLicense('Ngo9BigBOggjHTQxAR8/V1NNaF1cWWhPYVBpR2Nbek51flBFal9WVAciSV9jS3tTc0dkWXpbc3dSRGheV090Vg==');

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
