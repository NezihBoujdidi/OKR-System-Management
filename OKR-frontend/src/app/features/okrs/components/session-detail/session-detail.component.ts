import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { switchMap, takeUntil } from 'rxjs/operators';
import { OKRSession } from '../../../../models/okr-session.interface';
import { Status } from 'src/app/models/Status.enum';
import { OKRSessionService } from '../../../../services/okr-session.service';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-session-detail',
  templateUrl: './session-detail.component.html'
})
export class SessionDetailComponent implements OnInit, OnDestroy {
  okrSession!: OKRSession;
  activeTab: 'objectives' | 'alignment' | 'statistics' = 'objectives';
  okrSessionId: string = '';
  private destroy$ = new Subject<void>();
  Status = Status;
  sessionId: string | null = null;
  organizationId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private okrSessionService: OKRSessionService
  ) {}

  ngOnInit(): void {
    // Clear any existing highlight parameters on initialization
    localStorage.removeItem('highlightElementId');
    localStorage.removeItem('highlightElementType');
    localStorage.removeItem('parentElementId');
    localStorage.removeItem('fromAlignment');

    // First, check if there's an activeSessionTab in localStorage
    const storedActiveTab = localStorage.getItem('activeSessionTab');
    if (storedActiveTab === 'objectives' || storedActiveTab === 'alignment' || storedActiveTab === 'statistics') {
      this.activeTab = storedActiveTab;
      // Clear after use
      localStorage.removeItem('activeSessionTab');
    }

    this.route.paramMap.subscribe(params => {
      this.organizationId = params.get('orgId');
      this.sessionId = params.get('id');
  
      console.log(`Session Detail Loaded | OrgId: ${this.organizationId}, SessionId: ${this.sessionId}`);
  
      if (this.sessionId) {
        this.okrSessionService.setActiveSession(this.sessionId);
        this.okrSessionService.getOKRSessionById(this.sessionId).subscribe(okrSession => {
          if (okrSession) {
            this.okrSession = okrSession;
            
            // Clear highlight parameters from URL if they exist
            const currentParams = { ...this.route.snapshot.queryParams };
            if (currentParams['highlight'] || currentParams['from'] || currentParams['parent']) {
              delete currentParams['highlight'];
              delete currentParams['from'];
              delete currentParams['parent'];
              
              this.router.navigate([], {
                relativeTo: this.route,
                queryParams: currentParams,
                replaceUrl: true
              });
            }
          } else {
            this.okrSessionService.setActiveSession(null);
            this.router.navigate(['/okrs']);
          }
        });
      }
    });
    
    // Check for query parameters to switch tabs
    this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        const tabParam = params['tab'];
        if (tabParam === 'objectives' || tabParam === 'alignment' || tabParam === 'statistics') {
          console.log(`Switching to tab: ${tabParam} based on URL parameter`);
          this.activeTab = tabParam;
        }
      }
    });
  }
  

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    // Clear active session when leaving the detail view
    this.okrSessionService.setActiveSession(null);
  }

  setActiveTab(tab: 'objectives' | 'alignment' | 'statistics') {
    this.activeTab = tab;
    localStorage.setItem('activeSessionTab', tab);

    // Clear highlight parameters when switching tabs directly
    const currentParams = { ...this.route.snapshot.queryParams };
    delete currentParams['highlight'];
    delete currentParams['parent'];
    delete currentParams['from'];

    // Remove highlight-related items from localStorage
    localStorage.removeItem('highlightElementId');
    localStorage.removeItem('highlightElementType');
    localStorage.removeItem('parentElementId');
    localStorage.removeItem('fromAlignment');

    // Update URL without preserving highlight parameters
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: '', // Don't preserve any query params
      replaceUrl: true
    });
  }

  goBack() {
    if (this.organizationId) {
      this.router.navigate(['/organizations', this.organizationId]);
    } else {
      this.router.navigate(['/okrs']);
    }
  }
}