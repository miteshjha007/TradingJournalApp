import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './services/theme.service';
import { ToastComponent } from './components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  template: `
    <router-outlet />
    <app-toast></app-toast>
  `
})
export class AppComponent implements OnInit {
  constructor(private themeService: ThemeService) {}
  ngOnInit(): void { /* ThemeService auto-initializes in constructor */ }
}
