import { Component, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { ContentSidebarComponent } from '../../shared/content-sidebar/components/content-sidebar.component';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-dashboard-layout',
  templateUrl: './dashboard-layout.component.html'
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  @ViewChild('contentSidebar') contentSidebar!: ContentSidebarComponent;
  activeSection: string = 'home';
  private destroy$ = new Subject<void>();

  constructor(private router: Router) {}

  ngOnInit() {
    // Initial route detection
    this.detectActiveSection(this.router.url);

    // Subscribe to router events
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntil(this.destroy$)
    ).subscribe((event: any) => {
      this.detectActiveSection(event.url);
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onNavigationButtonClicked(section: string) {
    this.activeSection = section;
    if (this.contentSidebar) {
      this.contentSidebar.updateContent(section);
    }
  }

  private detectActiveSection(url: string) {
    if (url.includes('/okrs') || url.includes('/organizations')) {
      this.activeSection = 'okrs';
    } else if (url.includes('/home') || url.includes('/dashboard')) {
      this.activeSection = 'home';
    } else if (url.includes('/employees') || url.includes('/teams')) {
      this.activeSection = 'employees';
    } else if (url.includes('/organization') || url.includes('/users') || url.includes('/subscription')) {
      this.activeSection = 'manage';
    } else if (url.includes('/profile')) {
      this.activeSection = 'profile';
    } else if (url.includes('/chat')) {
      this.activeSection = 'chat';
    } else {
      this.activeSection = 'home';
    }
  }
} 