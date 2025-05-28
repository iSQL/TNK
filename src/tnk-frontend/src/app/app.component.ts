import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { BottomNavComponent } from './shared/components/bottom-nav/bottom-nav.component'; // Import BottomNav
import { CommonModule } from '@angular/common'; // Import CommonModule for async pipe

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, 
    RouterOutlet,
    BottomNavComponent,
    TranslateModule  
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  title = 'tnk-frontend';
  private translate = inject(TranslateService);

  constructor() {
    this.translate.setDefaultLang('sr');
    this.translate.use('sr');
  }

  ngOnInit(): void {
    // Initialization logic
  }
}
