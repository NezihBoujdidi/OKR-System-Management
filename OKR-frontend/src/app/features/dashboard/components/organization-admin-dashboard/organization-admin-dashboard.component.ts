import { Component, OnInit, OnDestroy, SecurityContext, ViewChild } from '@angular/core';
import { DashboardService } from '../../../../services/dashboard.service';
import { KeyMetric, Activity, ChartData, TeamPerformance } from '../../../../models/dashboard.interface';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { AuthStateService } from '../../../../services/auth-state.service';
import { UserService } from '../../../../services/user.service';
import { TeamService } from '../../../../services/team.service';
import { OKRSessionService } from '../../../../services/okr-session.service';
import { forkJoin, Subject, of, timer } from 'rxjs';
import { takeUntil, switchMap, tap, map, catchError, take, finalize } from 'rxjs/operators';
import { UserDetailsWithRole } from '../../../../models/user.interface';
import { Team } from '../../../../models/team.interface';
import { OKRSession } from '../../../../models/okr-session.interface';
import { ObjectiveService } from '../../../../services/objective.service';
import { KeyResultService } from '../../../../services/key-result.service';
import { KeyResultTaskService } from '../../../../services/key-result-task.service';
import { Objective } from '../../../../models/objective.interface';
import { KeyResult } from '../../../../models/key-result.interface';
import { KeyResultTask } from '../../../../models/key-result-task.interface';
import { Status } from '../../../../models/Status.enum';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { MonthlyGrowthDto, YearlyGrowthDto } from '../../../../models/dashboard-stats.interface';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ChatService, MessageEvent } from '../../../../services/chat.service';
import { ChatMessage } from '../../../../models/chat.models.interface';
import { PdfExportService } from '../../../../services/pdf-export.service';
import { BillingHistoryComponent } from '@features/subscription/components/billing-history.component';

@Component({
  selector: 'app-organization-admin-dashboard',
  templateUrl: './organization-admin-dashboard.component.html'
})
export class OrganizationAdminDashboardComponent implements OnInit, OnDestroy {
  @ViewChild('billingHistory') billingHistoryComponent!: BillingHistoryComponent;
  keyMetrics: KeyMetric[] = [];
  recentActivities: Activity[] = [];
  teamPerformance: TeamPerformance[] = [];
  
  // Dashboard metrics
  totalEmployees: number = 0;
  activeTeams: number = 0;
  activeOkrSessions: number = 0;
  averagePerformancePercentage: number = 0;
  
  // Trend metrics
  employeeGrowthPercentage: number = 0;
  activeTeamsChange: number = 0;
  activeOkrSessionsChange: number = 0;
  performanceChange: number = 0;
  
  // Chart view states
  employeeGrowthView: 'monthly' | 'yearly' = 'monthly';
  teamPerformanceView: '30days' | '90days' = '30days';
  
  // Data storage
  employeeGrowthMonthly: MonthlyGrowthDto[] = [];
  employeeGrowthYearly: YearlyGrowthDto[] = [];
  
  // Current user
  currentUser: UserDetailsWithRole | null = null;
  
  // Destroy subject for subscription management
  private destroy$ = new Subject<void>();

  // Employee Growth Chart
  employeeGrowthData: ChartConfiguration['data'] = {
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
    datasets: [
      {
        label: 'Employee Growth',
        data: [65, 80, 95, 110, 130, 150],
        borderColor: '#FFD700',
        backgroundColor: 'rgba(255, 215, 0, 0.1)',
        tension: 0.4,
        fill: false,
        pointBackgroundColor: '#FFFFFF',
        pointBorderColor: [], // Will be set dynamically
        pointBorderWidth: 2,
        pointRadius: 5,
        pointHoverRadius: 7
      }
    ]
  };

  employeeGrowthOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    },
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        padding: 10,
        displayColors: false,
        callbacks: {
          label: (context) => {
            return `Employees: ${context.parsed.y}`;
          }
        }
      }
    }
  };

  // Team Performance Chart
  teamPerformanceData: ChartConfiguration['data'] = {
    labels: ['Team A', 'Team B', 'Team C', 'Team D', 'Team E'],
    datasets: [
      {
        label: 'Performance Score',
        data: [85, 92, 78, 95, 88],
        backgroundColor: '#FFD700',
        borderRadius: 5
      }
    ]
  };

  teamPerformanceOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: false
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    },
    plugins: {
      legend: {
        display: false
      }
    }
  };

  // Array to store the fetched data
  sessions: OKRSession[] = [];
  objectives: Objective[] = [];
  keyResults: KeyResult[] = [];
  tasks: KeyResultTask[] = [];

  // Properties for OKR Risk Analysis
  riskAnalysisLoading: boolean = false;
  pdfPreviewUrl: SafeResourceUrl | null = null;
  private riskAnalysisChatSessionId: string | null = null;
  private riskAnalysisSubscription: any; // To store the subscription to message events

  // Report generation state
  isGeneratingReport: boolean = false;

  // Property to store the original team performance data
  private originalTeamPerformanceData: any[] = [];

  constructor(
    private dashboardService: DashboardService,
    private authState: AuthStateService,
    private userService: UserService,
    private teamService: TeamService,
    private okrSessionService: OKRSessionService,
    private objectiveService: ObjectiveService,
    private keyResultService: KeyResultService,
    private keyResultTaskService: KeyResultTaskService,
    private dashboardStatsService: DashboardStatsService,
    private chatService: ChatService,
    private sanitizer: DomSanitizer,
    private pdfExportService: PdfExportService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authState.getCurrentUser();
    if (this.currentUser?.organizationId) {
      this.loadDashboardData(this.currentUser.organizationId);
    }
    // Ensure a chat session is ready for risk analysis, but don't set it as current globally
    // The chat service should handle its own current session for the main chat popup
    this.chatService.getSessions().pipe(takeUntil(this.destroy$)).subscribe(sessions => {
      if (sessions.length > 0) {
        // Attempt to find a session titled for risk analysis or use the most recent one
        // This logic can be refined if dedicated session management for this feature is needed
        const riskSession = sessions.find(s => s.title.includes('Risk Analysis'));
        if (riskSession!= null)
          this.riskAnalysisChatSessionId = riskSession.id;
      } else {
        this.chatService.createSession().pipe(takeUntil(this.destroy$)).subscribe(newSession => {
          this.riskAnalysisChatSessionId = newSession.id;
          // Optionally update the title for clarity
          // newSession.title = 'OKR Risk Analysis Session'; 
        });
      }
    });
  }
  
  ngOnDestroy(): void {
    console.log("destroying org admin dash")

    if (this.riskAnalysisChatSessionId) {
      this.chatService.deleteConversation(this.riskAnalysisChatSessionId)
        .pipe(
          // finalize will always run when the source completes, errors, or is unsubscribed.
          // takeUntil(this.destroy$) will ensure that if this.destroy$ emits before
          // deleteConversation completes, the finalize block still runs due to unsubscription.
          takeUntil(this.destroy$), 
          finalize(() => {
            console.log('Finalize block for deleteConversation triggered. Completing destroy$.');
            // It's crucial that destroy$ is completed here to clean up other subscriptions
            // that might be using takeUntil(this.destroy$).
            if (!this.destroy$.isStopped) {
              this.destroy$.next();
              this.destroy$.complete();
            }

            if (this.riskAnalysisSubscription && !this.riskAnalysisSubscription.closed) {
              this.riskAnalysisSubscription.unsubscribe();
            }
          })
        )
        .subscribe({
          next: () => console.log('Risk analysis conversation deleted on component destroy (HTTP success).'),
          error: (err) => console.error('Error deleting risk analysis conversation (HTTP error):', err)
        });
    } else {
      // If there was no chat session ID, we can directly complete destroy$.
      console.log('No riskAnalysisChatSessionId, completing destroy$ directly.');
      if (!this.destroy$.isStopped) {
        this.destroy$.next();
        this.destroy$.complete();
      }
      if (this.riskAnalysisSubscription && !this.riskAnalysisSubscription.closed) {
        this.riskAnalysisSubscription.unsubscribe();
      }
    }
  }
  
  /**
   * Loads all dashboard data using the DashboardStatsService
   */
  private loadDashboardData(organizationId: string): void {
    console.log('Loading dashboard data for organization:', organizationId);
    
    // Load Active OKR Sessions
    this.dashboardStatsService.getActiveOKRs(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Active OKRs data loaded:', data);
          this.activeOkrSessions = data.activeOKRSessionCount;
          this.activeOkrSessionsChange = data.activeOKRSessionCount - data.activeOKRSessionCountLastMonth;
        },
        error: (error) => {
          console.error('Error loading active OKRs:', error);
        }
      });
    
    // Load Active Teams
    this.dashboardStatsService.getActiveTeams(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Active Teams data loaded:', data);
          this.activeTeams = data.activeTeamsCount;
          this.activeTeamsChange = data.activeTeamsCount - data.activeTeamsCountLastMonth;
        },
        error: (error) => {
          console.error('Error loading active teams:', error);
        }
      });
    
    // Load Employee Growth Stats
    this.dashboardStatsService.getEmployeeGrowthStats(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Employee Growth data loaded:', data);
          
          if (data) {
            this.employeeGrowthMonthly = data.monthly || [];
            this.employeeGrowthYearly = data.yearly || [];
            
            // Update the chart based on current view
            this.updateEmployeeGrowthChart();
            
            // Get the current employee count and calculate growth percentage
            const monthly = this.employeeGrowthMonthly;
            if (monthly && monthly.length > 0) {
              // Get current month's count
              const currentMonth = monthly[monthly.length - 1];
              this.totalEmployees = currentMonth.count;
              
              // Get 6 months ago count if available
              if (monthly.length > 6) {
                const sixMonthsAgo = monthly[monthly.length - 7];
                if (sixMonthsAgo.count > 0) {
                  this.employeeGrowthPercentage = Math.round(((this.totalEmployees - sixMonthsAgo.count) / sixMonthsAgo.count) * 100);
                }
              }
            }
          }
        },
        error: (error) => {
          console.error('Error loading employee growth stats:', error);
        }
      });
    
    // Load Collaborator Performance
    this.dashboardStatsService.getCollaboratorPerformance(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Collaborator Performance data loaded:', data);
          
          if (data && data.length > 0) {
            // Calculate current average performance
            const totalPerformance = data.reduce((sum, item) => sum + item.performanceAllTime, 0);
            this.averagePerformancePercentage = Math.round(totalPerformance / data.length);
            
            // Calculate last month's average performance for trend
            const totalPerformanceLast30Days = data.reduce((sum, item) => sum + item.performanceLast30Days, 0);
            const lastMonthAvgPerformance = Math.round(totalPerformanceLast30Days / data.length);
            
            // Calculate change in percentage points
            this.performanceChange = this.averagePerformancePercentage - lastMonthAvgPerformance;
          }
        },
        error: (error) => {
          console.error('Error loading collaborator performance:', error);
        }
      });
    
    // Load Team Performance Chart
    this.dashboardStatsService.getTeamPerformanceBarChart(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Team Performance chart data loaded:', data);
          
          if (data && data.length > 0) {
            this.updateTeamPerformanceChart(data);
          }
        },
        error: (error) => {
          console.error('Error loading team performance chart:', error);
        }
      });
  }
  
  /**
   * Updates the employee growth chart with data from the API
   */
  private updateEmployeeGrowthChart(): void {
    if (this.employeeGrowthView === 'monthly') {
      this.updateMonthlyEmployeeGrowthChart();
    } else {
      this.updateYearlyEmployeeGrowthChart();
    }
  }
  
  /**
   * Updates the chart with monthly data
   */
  private updateMonthlyEmployeeGrowthChart(): void {
    const monthlyData = this.employeeGrowthMonthly;
    if (!monthlyData || monthlyData.length === 0) return;
    
    const labels = monthlyData.map(item => {
      const date = new Date(item.year, item.month - 1);
      return date.toLocaleString('default', { month: 'short', year: '2-digit' });
    });
    
    const data = monthlyData.map(item => item.count);
    
    // Generate point border colors based on values
    const pointBorderColors = this.generatePointColors(data);
    
    this.employeeGrowthData = {
      labels: labels,
      datasets: [
        {
          label: 'Employee Growth',
          data: data,
          borderColor: '#FFD700',
          backgroundColor: 'rgba(255, 215, 0, 0.1)',
          tension: 0.4,
          fill: true,
          pointBackgroundColor: '#FFFFFF',
          pointBorderColor: pointBorderColors,
          pointBorderWidth: 2,
          pointRadius: 5,
          pointHoverRadius: 7
        }
      ]
    };
  }
  
  /**
   * Updates the chart with yearly data
   */
  private updateYearlyEmployeeGrowthChart(): void {
    const yearlyData = this.employeeGrowthYearly;
    if (!yearlyData || yearlyData.length === 0) return;
    
    const labels = yearlyData.map(item => item.year.toString());
    const data = yearlyData.map(item => item.count);
    
    // Generate point border colors based on values
    const pointBorderColors = this.generatePointColors(data);
    
    this.employeeGrowthData = {
      labels: labels,
      datasets: [
        {
          label: 'Employee Growth',
          data: data,
          borderColor: '#FFD700',
          backgroundColor: 'rgba(255, 215, 0, 0.1)',
          tension: 0.4,
          fill: true,
          pointBackgroundColor: '#FFFFFF',
          pointBorderColor: pointBorderColors,
          pointBorderWidth: 2,
          pointRadius: 5,
          pointHoverRadius: 7
        }
      ]
    };
  }
  
  /**
   * Generate border colors for chart points based on data values
   */
  private generatePointColors(data: number[]): string[] {
    if (!data || data.length === 0) return [];
    
    // Find min and max for scaling
    const min = Math.min(...data);
    const max = Math.max(...data);
    const range = max - min > 0 ? max - min : 1;
    
    return data.map(value => {
      // Calculate a normalized position between 0 and 1
      const normalizedValue = (value - min) / range;
      
      // Scale based on the value:
      if (normalizedValue > 0.8) {
        return '#10B981'; // Green for high values
      } else if (normalizedValue > 0.5) {
        return '#FFD700'; // Gold for medium-high values
      } else if (normalizedValue > 0.3) {
        return '#F59E0B'; // Amber for medium-low values
      } else {
        return '#EF4444'; // Red for low values
      }
    });
  }
  
  /**
   * Toggles between monthly and yearly employee growth views
   */
  toggleEmployeeGrowthView(view: 'monthly' | 'yearly'): void {
    if (this.employeeGrowthView !== view) {
      this.employeeGrowthView = view;
      this.updateEmployeeGrowthChart();
    }
  }
  
  /**
   * Updates the team performance chart with data from the API
   */
  private updateTeamPerformanceChart(teamPerformance: any[]): void {
    // Store the original data for reporting
    this.originalTeamPerformanceData = teamPerformance;
    
    const labels = teamPerformance.map(team => team.teamName);
    
    // Use data based on the current view
    const data = teamPerformance.map(team => 
      this.teamPerformanceView === '30days' 
        ? team.performanceLast30Days 
        : team.performanceLast3Months
    );
    
    this.teamPerformanceData = {
      labels: labels,
      datasets: [
        {
          label: 'Performance Score',
          data: data,
          backgroundColor: '#FFD700',
          borderRadius: 5
        }
      ]
    };
  }

  /**
   * Toggles between 30-day and 90-day team performance views
   */
  toggleTeamPerformanceView(view: '30days' | '90days'): void {
    if (this.teamPerformanceView !== view) {
      this.teamPerformanceView = view;
      
      // If we have data, update the chart with the new view
      if (this.dashboardStatsService) {
        const organizationId = this.currentUser?.organizationId;
        if (organizationId) {
          this.dashboardStatsService.getTeamPerformanceBarChart(organizationId)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (data) => {
                if (data && data.length > 0) {
                  this.updateTeamPerformanceChart(data);
                }
              },
              error: (error) => {
                console.error('Error loading team performance chart:', error);
              }
            });
        }
      }
    }
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'on-track': return 'text-green-600';
      case 'at-risk': return 'text-yellow-600';
      case 'behind': return 'text-red-600';
      default: return 'text-gray-600';
    }
  }

  getTrendIcon(trend: number): string {
    return trend > 0 ? '↑' : trend < 0 ? '↓' : '→';
  }

  getTrendClass(trend: number): string {
    return trend > 0 ? 'text-green-600' : trend < 0 ? 'text-red-600' : 'text-gray-600';
  }

  triggerOkrRiskAnalysis(): void {
    if (!this.riskAnalysisChatSessionId) {
      console.error('No chat session available for risk analysis.');
      // Optionally, try to create/get a session again here
      this.chatService.createSession().pipe(takeUntil(this.destroy$)).subscribe(newSession => {
        this.riskAnalysisChatSessionId = newSession.id;
        this.proceedWithRiskAnalysis();
      });
      return;
    }
    this.proceedWithRiskAnalysis();
  }

  private proceedWithRiskAnalysis(): void {
    this.riskAnalysisLoading = true;
    this.pdfPreviewUrl = null;

    // First, ensure we're using the correct session
    if (this.riskAnalysisChatSessionId) {
      // Get all sessions
      this.chatService.getSessions().pipe(
        take(1), // Only take the first emission
        switchMap(sessions => {
          // Find our target session
          const targetSession = sessions.find(s => s.id === this.riskAnalysisChatSessionId);
          if (targetSession) {
            // Set it as the current session
            this.chatService.setCurrentSession(targetSession);
            console.log('Set current session to:', targetSession.id);
            return of(true);
          } else {
            console.error('Could not find session with ID:', this.riskAnalysisChatSessionId);
            return of(false);
          }
        })
      ).subscribe(sessionFound => {
        if (sessionFound) {
          // Continue with sending message now that session is set
          this.chatService.sendMessage('analyze okrs risks according to the structure').subscribe({
            next: () => console.log('Risk analysis message sent successfully'),
            error: (err) => console.error('Error sending risk analysis message:', err)
          });
          this.setupMessageEventSubscription();
        } else {
          // Create a new session if not found
          this.chatService.createSession().pipe(take(1)).subscribe(newSession => {
            this.riskAnalysisChatSessionId = newSession.id;
            this.chatService.setCurrentSession(newSession);
            console.log('Created and set new session:', newSession.id);
            this.chatService.sendMessage('analyze okrs risks according to the structure').subscribe({
              next: () => console.log('Risk analysis message sent successfully from new session'),
              error: (err) => console.error('Error sending risk analysis message from new session:', err)
            });
            this.setupMessageEventSubscription();
          });
        }
      });
    } else {
      // No session ID, create one
      this.chatService.createSession().pipe(take(1)).subscribe(newSession => {
        this.riskAnalysisChatSessionId = newSession.id;
        this.chatService.setCurrentSession(newSession);
        console.log('Created and set new session (no prior ID):', newSession.id);
        this.chatService.sendMessage('analyze okrs risks according to the structure').subscribe({
          next: () => console.log('Risk analysis message sent successfully from newly created session'),
          error: (err) => console.error('Error sending risk analysis message from newly created session:', err)
        });
        this.setupMessageEventSubscription();
      });
    }
  }

  // Extract message event subscription logic to a separate method
  private setupMessageEventSubscription(): void {
    if (this.riskAnalysisSubscription) {
      this.riskAnalysisSubscription.unsubscribe();
    }

    this.riskAnalysisSubscription = this.chatService.getMessageEvents().pipe(takeUntil(this.destroy$)).subscribe((event: MessageEvent) => {
      if (event.type === 'bot-message-received') {
        // Check if still loading (i.e., not cancelled)
        if (!this.riskAnalysisLoading) return;

        this.chatService.getCurrentSession().pipe(takeUntil(this.destroy$)).subscribe(currentSession => {
          if (currentSession && currentSession.id === this.riskAnalysisChatSessionId) {
            const botMessage = currentSession.messages[currentSession.messages.length - 1];
            if (botMessage && botMessage.sender === 'bot') {
              // Store the PDF data but keep loading state active for extended period
              let tempPdfDataUrl: SafeResourceUrl | null = null;
              const rawPdfData = botMessage.pdfData;

              if (rawPdfData) {
                try {
                  const pdfDataUrl = `data:application/pdf;base64,${rawPdfData}`;
                  tempPdfDataUrl = this.sanitizer.bypassSecurityTrustResourceUrl(pdfDataUrl);
                  console.log('Received new PDF data, keeping shimmer visible for a moment...');
                } catch (e) {
                  console.error('Error processing PDF data:', e);
                  tempPdfDataUrl = null;
                }
              } else {
                console.log('No PDF data in bot message');
              }
              
              // Keep the shimmer animation for a minimum duration to ensure smooth transition
              timer(10000).pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                  // Finally update the UI
                  this.riskAnalysisLoading = false;
                  this.pdfPreviewUrl = tempPdfDataUrl;
                  
                  // Unsubscribe after processing this response
                  if (this.riskAnalysisSubscription) {
                    this.riskAnalysisSubscription.unsubscribe();
                    this.riskAnalysisSubscription = null; // Clear the reference
                  }
                })
              ).subscribe();
            }
          }
        });
      }
    });
  }

  sendFollowUpMessage(message: string): void {
    if (!message.trim() || !this.riskAnalysisChatSessionId) {
      return;
    }
    this.riskAnalysisLoading = true;
    this.pdfPreviewUrl = null; // Clear previous preview
    const followUpMessage = 'Adjustment of analyze okrs risks : ' + message; 
    // First, ensure we're using the correct session
    this.chatService.getSessions().pipe(
      take(1), // Only take the first emission
      switchMap(sessions => {
        // Find our target session
        const targetSession = sessions.find(s => s.id === this.riskAnalysisChatSessionId);
        if (targetSession) {
          // Set it as the current session
          this.chatService.setCurrentSession(targetSession);
          console.log('Set current session for follow-up message:', targetSession.id);
          return of(true);
        } else {
          console.error('Could not find session with ID for follow-up:', this.riskAnalysisChatSessionId);
          return of(false);
        }
      })
    ).subscribe(sessionFound => {
      if (sessionFound) {
        // Continue with sending message now that session is set
        this.chatService.sendMessage(followUpMessage).subscribe({
          next: () => console.log('Follow-up message sent successfully'),
          error: (err) => console.error('Error sending follow-up message:', err)
        });
        this.setupFollowUpMessageSubscription();
      } else {
        // Handle session not found case - this shouldn't happen for follow-up messages
        console.error('Session not found for follow-up message');
        this.riskAnalysisLoading = false;
      }
    });
  }

  // Extract follow-up message event subscription logic to a separate method
  private setupFollowUpMessageSubscription(): void {
    if (this.riskAnalysisSubscription) {
      this.riskAnalysisSubscription.unsubscribe();
    }
    
    this.riskAnalysisSubscription = this.chatService.getMessageEvents().pipe(takeUntil(this.destroy$)).subscribe((event: MessageEvent) => {
      if (event.type === 'bot-message-received') {
         // Check if still loading (i.e., not cancelled)
         if (!this.riskAnalysisLoading) return;
         
         this.chatService.getCurrentSession().pipe(takeUntil(this.destroy$)).subscribe(currentSession => {
          if (currentSession && currentSession.id === this.riskAnalysisChatSessionId) {
            const botMessage = currentSession.messages[currentSession.messages.length - 1];
            if (botMessage && botMessage.sender === 'bot') {
              // Store the PDF data but keep loading state active for extended period
              let tempPdfDataUrl: SafeResourceUrl | null = null;
              const rawPdfData = botMessage.pdfData;
              
              if (rawPdfData) {
                try {
                  const pdfDataUrl = `data:application/pdf;base64,${rawPdfData}`;
                  tempPdfDataUrl = this.sanitizer.bypassSecurityTrustResourceUrl(pdfDataUrl);
                  console.log('Received new PDF data, keeping shimmer visible for a moment...');
                } catch (e) {
                  console.error('Error processing PDF data:', e);
                  tempPdfDataUrl = null;
                }
              } else {
                console.log('No PDF data in follow-up bot message');
              }
              
              // Keep the shimmer animation for a minimum duration to ensure smooth transition
              timer(10000).pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                  // Finally update the UI
                  this.riskAnalysisLoading = false;
                  this.pdfPreviewUrl = tempPdfDataUrl;
                  
                  // Unsubscribe after processing this response
                  if (this.riskAnalysisSubscription) {
                    this.riskAnalysisSubscription.unsubscribe();
                    this.riskAnalysisSubscription = null; // Clear the reference
                  }
                })
              ).subscribe();
            }
          }
        });
      }
    });
  }

  cancelOkrRiskAnalysis(): void {
    console.log('Cancelling OKR Risk Analysis');
    this.riskAnalysisLoading = false;
    this.pdfPreviewUrl = null;
    if (this.riskAnalysisSubscription) {
      this.riskAnalysisSubscription.unsubscribe();
      this.riskAnalysisSubscription = null; // Clear the reference
    }
    // Optionally, notify the backend or ChatService if true cancellation is needed/possible
  }

  // Method to adjust the height of the textarea dynamically based on content
  adjustTextareaHeight(event: any): void {
    const textarea = event.target;
    
    // Reset height to auto to get the correct scrollHeight
    textarea.style.height = 'auto';
    
    // Check if content has multiple lines
    const hasMultipleLines = textarea.value.includes('\n');
    const contentHeight = textarea.scrollHeight;
    
    // Set new height based on content
    if (hasMultipleLines) {
      // For multiline content, allow scrolling up to max height
      const newHeight = Math.max(60, Math.min(contentHeight, 120));
      textarea.style.height = newHeight + 'px';
    } else {
      // For single line, just fit the content
      textarea.style.height = Math.max(60, contentHeight) + 'px';
    }
  }

  // Handle keydown events in the follow-up message textarea
  handleFollowUpKeydown(event: KeyboardEvent, inputElement: HTMLTextAreaElement): void {
    // If Enter is pressed without Shift key, send the message
    if (event.key === 'Enter' && !event.shiftKey) {
      // Prevent the default behavior (new line)
      event.preventDefault();
      
      const message = inputElement.value.trim();
      if (message && !this.riskAnalysisLoading) {
        this.sendFollowUpMessage(message);
        inputElement.value = '';
      }
    }
  }

  // Generate dashboard report as PDF
  async generateDashboardReport(): Promise<void> {
    // Set loading state
    this.isGeneratingReport = true;

    // Add a 3-second delay before starting the report generation
    await timer(3000).pipe(take(1)).toPromise();
    
    try {
      // Retrieve any data that might not be loaded yet
      if (this.currentUser?.organizationId) {
        // If teams aren't loaded, load them
        let teams: any[] = [];
        const teamsPromise = new Promise<void>((resolve) => {
          this.teamService.getTeamsByOrganizationId(this.currentUser!.organizationId!)
            .subscribe({
              next: (teamsData) => {
                teams = teamsData;
                resolve();
              },
              error: () => resolve() // Resolve even on error to continue report generation
            });
        });
        
        await teamsPromise;

        // If team performance data isn't loaded, load it
        if (!this.teamPerformance || this.teamPerformance.length === 0) {
          await new Promise<void>((resolve) => {
            this.dashboardStatsService.getTeamPerformanceBarChart(this.currentUser!.organizationId!)
              .subscribe({
                next: (data) => {
                  if (data && data.length > 0) {
                    this.updateTeamPerformanceChart(data);
                  }
                  resolve();
                },
                error: () => resolve() // Resolve even on error
              });
          });
        }
      
        // Prepare metrics data
        const metrics = [
          { 
            title: 'Total Employees', 
            value: this.totalEmployees, 
            change: this.employeeGrowthPercentage 
          },
          { 
            title: 'Active Teams', 
            value: this.activeTeams, 
            change: this.activeTeamsChange 
          },
          { 
            title: 'Active OKR Sessions', 
            value: this.activeOkrSessions, 
            change: this.activeOkrSessionsChange 
          },
          { 
            title: 'Average Performance', 
            value: `${this.averagePerformancePercentage}%`, 
            change: this.performanceChange 
          }
        ];
        
        // Prepare growth data
        const growthData = {
          yearly: this.employeeGrowthYearly,
          monthly: this.employeeGrowthMonthly,
          label: 'Employee'
        };
        
        // Format team performance data for the report 
        // Use originalTeamPerformanceData to include both 30-day and 90-day metrics
        const performanceData = {
          label: 'Team',
          data: this.originalTeamPerformanceData.map(team => ({
            name: team.teamName,
            performance30Days: team.performanceLast30Days,
            performance90Days: team.performanceLast3Months
          }))
        };
        
        // Generate organization name for the report
        let organizationName = 'Your Organization';
        if (this.currentUser?.organizationId) {
          // You could fetch the organization name here if not already available
        }
        
        // Prepare dashboard data object
        const dashboardData = {
          metrics,
          growthData,
          performanceData,
          sessions: this.sessions,
          teams,
          activities: this.recentActivities
        };
        
        // Generate the PDF report
        await this.pdfExportService.exportDashboardToPdf(
          dashboardData, 
          'Organization Admin Dashboard', 
          organizationName
        );
      }
    } catch (error) {
      console.error('Error generating dashboard report:', error);
      // Handle error - show a notification to the user
    } finally {
      // Reset loading state immediately
      this.isGeneratingReport = false;
    }
  }
} 