import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { DashboardService } from '../../../../services/dashboard.service';
import { UserService } from '../../../../services/user.service';
import { TeamService } from '../../../../services/team.service';
import { OKRSessionService } from '../../../../services/okr-session.service';
import { KeyResultTaskService } from '../../../../services/key-result-task.service';
import { KeyMetric, Activity, TeamPerformance } from '../../../../models/dashboard.interface';
import { User, UserDetailsWithRole, RoleType } from '../../../../models/user.interface';
import { Team } from '../../../../models/team.interface';
import { OKRSession } from '../../../../models/okr-session.interface';
import { KeyResultTask } from '../../../../models/key-result-task.interface';
import { Chart, ChartConfiguration } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ObjectiveService } from '../../../../services/objective.service';
import { KeyResultService } from '../../../../services/key-result.service';
import { Objective } from '../../../../models/objective.interface';
import { KeyResult } from '../../../../models/key-result.interface';
import { Status } from 'src/app/models/Status.enum';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { AuthStateService } from '../../../../services/auth-state.service';
import { Subject, of, concat, EMPTY, timer } from 'rxjs';
import { takeUntil, switchMap, catchError, tap, concatMap, toArray, take, finalize } from 'rxjs/operators';
import { 
  CollaboratorPerformanceRangeDto,
  CollaboratorTaskStatusStatsDto
} from '../../../../models/dashboard-stats.interface';
import { PdfExportService } from '../../../../services/pdf-export.service';

// Interface for the Top Performer data
interface TopPerformer {
  collaborator: UserDetailsWithRole;
  performanceScore: number;
  tasksCompleted: number;
  tasksInProgress: number;
  onTimePercentage: number;
  performanceTrend: number;
}

@Component({
  selector: 'app-team-manager-dashboard',
  templateUrl: './team-manager-dashboard.component.html'
})
export class TeamManagerDashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild(BaseChartDirective) chart: BaseChartDirective | undefined;
  
  currentUser: UserDetailsWithRole | null = null;
  managedTeams: Team[] = [];
  managedCollaborators: UserDetailsWithRole[] = [];
  managedCollaboratorIds: string[] = [];

  // Dashboard metrics
  totalCollaborators: number = 0;
  totalOkrSessions: number = 0;
  averagePerformance: number = 0;
  delayedSessions: number = 0;
  
  // Team performance view state
  teamPerformanceView: '30days' | '90days' = '30days';
  
  // Top performers view state
  topPerformersView: '30days' | '90days' = '30days';
  topPerformers: TopPerformer[] = [];
  performersPage: number = 0;
  
  // Math reference for template use
  Math = Math;

  keyMetrics: KeyMetric[] = [];
  recentActivities: Activity[] = [];
  teamPerformance: TeamPerformance[] = [];
  
  // Data collections
  performanceData: CollaboratorPerformanceRangeDto[] = [];
  taskStatusData: CollaboratorTaskStatusStatsDto[] = [];

  // Destroy subject for managing subscriptions
  private destroy$ = new Subject<void>();

  // Tasks Status Distribution Chart
  taskStatusChartData: ChartConfiguration<'doughnut'>['data'] = {
    labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
    datasets: [{
      data: [0, 0, 0, 0], // Initialize with zeros
      backgroundColor: [
        '#10B981', // Green for Completed
        '#6366F1', // Purple for In Progress
        '#9CA3AF', // Gray for Not Started
        '#EF4444'  // Red for Overdue
      ],
      borderWidth: 0
    }]
  };

  // Task Status chart options
  taskStatusOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '70%',
    plugins: {
      legend: {
        position: 'right',
        labels: {
          padding: 20,
          usePointStyle: true,
          pointStyle: 'circle'
        }
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const index = context.dataIndex;
            const value = context.raw as number;
            
            // Calculate sum manually to avoid type issues
            let sum = 0;
            if (this.taskStatusChartData && this.taskStatusChartData.datasets && this.taskStatusChartData.datasets[0]) {
              this.taskStatusChartData.datasets[0].data.forEach(val => {
                sum += (val as number);
              });
            }
            
            const percentage = sum > 0 ? Math.round((value / sum) * 100) : 0;
            return `${context.label}: ${value} (${percentage}%)`;
          }
        }
      }
    }
  };

  // Team Performance Chart
  teamPerformanceData: ChartConfiguration['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Performance Score',
        data: [],
        backgroundColor: '#6366F1',
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

  objectives: Objective[] = [];
  keyResults: KeyResult[] = [];

  // Property to store the original team performance data
  private originalTeamPerformanceData: any[] = [];
  
  // Report generation state
  isGeneratingReport: boolean = false;

  constructor(
    private dashboardService: DashboardService,
    private userService: UserService,
    private teamService: TeamService,
    private okrSessionService: OKRSessionService,
    private keyResultTaskService: KeyResultTaskService,
    private objectiveService: ObjectiveService,
    private keyResultService: KeyResultService,
    private dashboardStatsService: DashboardStatsService,
    private authStateService: AuthStateService,
    private pdfExportService: PdfExportService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authStateService.getCurrentUser();
    
    if (this.currentUser?.id) {
      this.loadTeamsAndCollaborators(this.currentUser.id);
    }
    
    // Initialize empty chart data to ensure proper rendering
    if (!this.taskStatusChartData || !this.taskStatusChartData.datasets) {
      this.taskStatusChartData = {
        labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
        datasets: [{
          data: [0, 0, 0, 0],
          backgroundColor: [
            '#10B981', '#6366F1', '#9CA3AF', '#EF4444'
          ],
          borderWidth: 0
        }]
      };
    }
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngAfterViewInit(): void {
    // Chart reference will be available after view init
    // Wait a short time to ensure everything is ready
    setTimeout(() => {
      // If we already have task data, update the chart
      if (this.taskStatusData?.length > 0) {
        this.updateTaskStatusChart();
      }
      
      // Otherwise, ensure chart is initialized with zeros
      else if (this.chart && this.chart.chart) {
        console.log('Initializing empty chart');
        this.chart.chart.update();
      }
    }, 200);
  }

  private loadTeamsAndCollaborators(managerId: string): void {
    // Get teams managed by this manager
    this.teamService.getTeamsByManagerId(managerId)
      .pipe(
        takeUntil(this.destroy$),
        tap(teams => {
          this.managedTeams = teams;
          console.log('Managed teams loaded:', teams);
          
          // After teams are loaded, load all dashboard data
          if (teams.length > 0) {
            this.loadCollaboratorsForTeams(teams);
            this.loadDashboardMetrics(managerId);
          }
        }),
        catchError(error => {
          console.error('Error loading teams:', error);
          return of([]);
        })
      )
      .subscribe();
  }
  
  private loadCollaboratorsForTeams(teams: Team[]): void {
    // If there are no teams, skip this step
    if (teams.length === 0) {
      return;
    }
    
    // Create an array to hold all collaborators
    let allCollaborators: UserDetailsWithRole[] = [];
    
    // Start with the first team
    this.processTeamCollaborators(teams, 0, allCollaborators);
  }
  
  private processTeamCollaborators(teams: Team[], index: number, allCollaborators: UserDetailsWithRole[]): void {
    // If we've processed all teams, finish the operation
    if (index >= teams.length) {
      // Remove duplicates (if a collaborator is in multiple teams)
      this.managedCollaborators = Array.from(
        new Map(allCollaborators.map(c => [c.id, c])).values()
      );
      
      // Store collaborator IDs for filtering other data
      this.managedCollaboratorIds = this.managedCollaborators.map(c => c.id);
      
      // Update total collaborators count
      this.totalCollaborators = this.managedCollaborators.length;
      
      console.log('Managed collaborators loaded:', this.managedCollaborators);
      
      // After collaborators are loaded, load performance and task data
      this.loadCollaboratorPerformance();
      this.loadTaskStatusData();
      return;
    }
    
    // Get the current team
    const team = teams[index];
    
    // Load collaborators for this team
    this.teamService.getCollaboratorsForTeam(team.id)
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          console.error(`Error loading collaborators for team ${team.id}:`, error);
          return of([]);
        })
      )
      .subscribe({
        next: (collaborators) => {
          console.log(`Loaded ${collaborators.length} collaborators for team ${team.name}`);
          
          // Add these collaborators to our array
          allCollaborators = [...allCollaborators, ...collaborators];
          
          // Continue with the next team
          this.processTeamCollaborators(teams, index + 1, allCollaborators);
        },
        error: (error) => {
          console.error(`Error processing team ${team.id}:`, error);
          
          // Continue with the next team despite the error
          this.processTeamCollaborators(teams, index + 1, allCollaborators);
        }
      });
  }
  
  private loadDashboardMetrics(managerId: string): void {
    // Load session statistics
    this.dashboardStatsService.getManagerSessionStats(managerId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          console.log('Manager session stats loaded:', stats);
          this.totalOkrSessions = stats.activeSessions;
          this.delayedSessions = stats.delayedSessions;
        },
        error: (error) => {
          console.error('Error loading session stats:', error);
        }
      });
      
    // Load team performance data
    if (this.currentUser?.organizationId) {
      this.loadTeamPerformanceData(this.currentUser.organizationId);
    }
    
    // Load recent activities
    this.dashboardService.getRecentActivities()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (activities) => {
      this.recentActivities = activities;
        },
        error: (error) => {
          console.error('Error loading activities:', error);
        }
      });
  }
  
  private loadCollaboratorPerformance(): void {
    if (!this.currentUser?.organizationId || this.managedCollaboratorIds.length === 0) {
      return;
    }
    
    this.dashboardStatsService.getCollaboratorPerformance(this.currentUser.organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: CollaboratorPerformanceRangeDto[]) => {
          console.log('Collaborator performance data loaded:', data);
          
          // Filter to only include managed collaborators
          this.performanceData = data.filter((item: CollaboratorPerformanceRangeDto) => 
            this.managedCollaboratorIds.includes(item.collaboratorId)
          );
          
          // Calculate average performance
          if (this.performanceData.length > 0) {
            const totalPerformance = this.performanceData.reduce(
              (sum, item) => sum + item.performanceAllTime, 0
            );
            this.averagePerformance = Math.round(totalPerformance / this.performanceData.length);
          }
          
          // Update top performers
          this.updateTopPerformers();
        },
        error: (error) => {
          console.error('Error loading collaborator performance:', error);
        }
      });
  }
  
  private loadTaskStatusData(): void {
    if (!this.currentUser?.organizationId || this.managedCollaboratorIds.length === 0) {
      return;
    }

    this.dashboardStatsService.getCollaboratorTaskStatusStats(this.currentUser.organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Task status data loaded:', data);
          
          // Filter to only include managed collaborators
          this.taskStatusData = data.filter(item => 
            this.managedCollaboratorIds.includes(item.collaboratorId)
          );
          
          this.updateTaskStatusChart();
        },
        error: (error) => {
          console.error('Error loading task status data:', error);
        }
      });
  }
  
  private loadTeamPerformanceData(organizationId: string): void {
    this.dashboardStatsService.getTeamPerformanceBarChart(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          console.log('Team performance data loaded:', data);
          
          // Filter to only include managed teams
          const managedTeamIds = this.managedTeams.map(team => team.id);
          const filteredData = data.filter(item => 
            managedTeamIds.includes(item.teamId)
          );
          
          if (filteredData.length > 0) {
            this.updateTeamPerformanceChart(filteredData);
          }
        },
        error: (error) => {
          console.error('Error loading team performance data:', error);
        }
      });
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
          backgroundColor: '#6366F1',
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
      
      // If we have an organization ID, reload the data
      if (this.currentUser?.organizationId) {
        this.loadTeamPerformanceData(this.currentUser.organizationId);
      }
    }
  }
  
  /**
   * Toggles between 30-day and 90-day top performers views
   */
  toggleTopPerformersView(view: '30days' | '90days'): void {
    if (this.topPerformersView !== view) {
      this.topPerformersView = view;
      this.performersPage = 0; // Reset to first page
      this.updateTopPerformers();
    }
  }
  
  /**
   * Updates the top performers based on the current view (30 days or 90 days)
   */
  private updateTopPerformers(): void {
    if (!this.performanceData || this.performanceData.length === 0) {
      this.topPerformers = [];
      return;
    }

    // Filter to only include managed collaborators and exclude the team manager (current user)
    const relevantPerformanceData = this.performanceData.filter(
      p => this.managedCollaboratorIds.includes(p.collaboratorId) && p.collaboratorId !== this.currentUser?.id
    );

    // Sort and map to TopPerformer interface
    const mappedPerformers = relevantPerformanceData.map(perf => {
      const collaborator = this.managedCollaborators.find(c => c.id === perf.collaboratorId);
      
      // Skip if we couldn't find the collaborator or it's the current user
      if (!collaborator || collaborator.id === this.currentUser?.id) {
        return null;
      }
      
      // Find task data for this collaborator if available
      const taskData = this.taskStatusData.find(t => t.collaboratorId === perf.collaboratorId);
      
      // Calculate the score based on the current view
      const performanceScore = this.topPerformersView === '30days' 
        ? perf.performanceLast30Days 
        : perf.performanceLast3Months;
      
      // Calculate trend using real data comparison
      // For 30-day view: difference between 30-day and 90-day performance
      // For 90-day view: difference between 90-day and all-time performance
      const performanceTrend = this.topPerformersView === '30days'
        ? perf.performanceLast30Days - perf.performanceLast3Months
        : perf.performanceLast3Months - perf.performanceAllTime;
      
      // Calculate on-time percentage (example calculation, adjust as needed)
      const onTimePercentage = Math.round(
        ((taskData?.completed || 0) / 
        Math.max(1, (taskData?.completed || 0) + (taskData?.overdue || 0))) * 100
      );
      
      return {
        collaborator: collaborator,
        performanceScore,
        tasksCompleted: taskData?.completed || 0,
        tasksInProgress: taskData?.inProgress || 0,
        onTimePercentage,
        performanceTrend
      };
    })
    .filter(performer => performer !== null) as TopPerformer[]; // Filter out null entries
    
    // Sort by performanceScore in descending order
    const sortedPerformers = mappedPerformers.sort((a, b) => 
      b.performanceScore - a.performanceScore
    );
    
    this.topPerformers = sortedPerformers;
  }
  
  /**
   * Gets the visible subset of top performers based on the current page
   */
  getVisibleTopPerformers(): TopPerformer[] {
    const start = this.performersPage * 4;
    return this.topPerformers.slice(start, start + 4);
  }
  
  /**
   * Navigates through the pages of performers
   */
  showMorePerformers(offset: number): void {
    const newPage = this.performersPage + (offset / 4);
    if (newPage >= 0 && newPage * 4 < this.topPerformers.length) {
      this.performersPage = newPage;
    }
  }
  
  /**
   * Gets the score to display for a performer
   */
  getPerformerScore(performer: TopPerformer): number {
    return Math.round(performer.performanceScore);
  }
  
  /**
   * Gets a stroke-dasharray value for an SVG circle based on percentage
   * For a circle with radius 45, the circumference is 2πr = 2π*45 ≈ 282.74
   */
  getStrokeDashArray(percentage: number): string {
    // The circumference of a circle with radius 45
    const circumference = 2 * Math.PI * 45;
    
    // Calculate the length of the colored part of the stroke
    const dashLength = (percentage / 100) * circumference;
    
    // Return the dasharray - colored length followed by the remaining circumference
    return `${dashLength} ${circumference}`;
  }

  /**
   * Gets the appropriate color for the performance circle based on percentage
   */
  getPerformanceColor(percentage: number): string {
    if (percentage >= 60) return '#4285F4'; // Blue for high performance
    if (percentage > 0) return '#EA4335';  // Red for medium/low performance
    return '#D1D5DB'; // Gray for 0%
  }

  /**
   * Gets the appropriate SVG path for the performance arc
   * This is kept for backward compatibility
   */
  getPerformanceArc(percentage: number): string {
    // Calculate the arc path for the given percentage (0-100)
    const r = 15.9155; // radius from the original SVG viewBox
    
    // Convert percentage to a value between 0 and 1
    const value = percentage / 100;
    
    // Calculate the angle based on the percentage (0-100%)
    const angle = value * 360;
    
    // Convert angle to radians
    const rad = (angle - 90) * Math.PI / 180;
    
    // Calculate the end point of the arc
    const x = 18 + r * Math.cos(rad);
    const y = 18 + r * Math.sin(rad);
    
    // Determine if the arc should be drawn as a large arc
    const largeArc = angle > 180 ? 1 : 0;
    
    // Create the SVG path
    return `M18 2.0845 a ${r} ${r} 0 ${largeArc} 1 ${x - 18} ${y - 2.0845}`;
  }
  
  /**
   * Returns the appropriate color class based on performance
   */
  getPerformanceColorClass(percentage: number): string {
    if (percentage >= 80) return 'text-green-500';
    if (percentage >= 60) return 'text-blue-500';
    if (percentage >= 40) return 'text-amber-500';
    return 'text-red-500';
  }
  
  /**
   * Returns the appropriate text color class based on performance
   */
  getPerformanceTextClass(percentage: number): string {
    if (percentage >= 60) return 'text-blue-600';
    if (percentage > 0) return 'text-red-600';
    return 'text-gray-500';
  }
  
  /**
   * Gets the initials for a user
   */
  getInitials(user?: UserDetailsWithRole): string {
    if (!user) return '';
    return `${user.firstName?.charAt(0) || ''}${user.lastName?.charAt(0) || ''}`;
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

  /**
   * Updates the task status chart with aggregated data from all managed collaborators
   */
  private updateTaskStatusChart(): void {
    // Handle empty data case - maintain the existing structure but with zero values
    if (!this.taskStatusData || this.taskStatusData.length === 0) {
      console.log('No task data to display, using empty dataset');
      this.taskStatusChartData = {
        labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
        datasets: [{
          data: [0, 0, 0, 0],
          backgroundColor: [
            '#10B981', // Green for Completed
            '#6366F1', // Purple for In Progress
            '#9CA3AF', // Gray for Not Started
            '#EF4444'  // Red for Overdue
          ],
          borderWidth: 0
        }]
      };
      return;
    }

    console.log('Updating task status chart with data:', this.taskStatusData);
    
    // Sum up all task statuses across collaborators
    let totalCompleted = 0;
    let totalInProgress = 0;
    let totalNotStarted = 0;
    let totalOverdue = 0;
    
    this.taskStatusData.forEach(collaborator => {
      totalCompleted += collaborator.completed;
      totalInProgress += collaborator.inProgress;
      totalNotStarted += collaborator.notStarted;
      totalOverdue += collaborator.overdue;
    });
    
    const total = totalCompleted + totalInProgress + totalNotStarted + totalOverdue;
    
    if (total === 0) {
      console.log('No tasks found in the data, using empty dataset');
      this.taskStatusChartData = {
        labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
        datasets: [{
          data: [0, 0, 0, 0],
          backgroundColor: [
            '#10B981', '#6366F1', '#9CA3AF', '#EF4444'
          ],
          borderWidth: 0
        }]
      };
      return;
    }
    
    // Log the totals for debugging
    console.log('Task status totals:', {
      completed: totalCompleted,
      inProgress: totalInProgress,
      notStarted: totalNotStarted,
      overdue: totalOverdue,
      total: total
    });
    
    // Create a properly typed chartData object
    const chartData: ChartConfiguration<'doughnut'>['data'] = {
      labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
      datasets: [
        {
          data: [totalCompleted, totalInProgress, totalNotStarted, totalOverdue],
          backgroundColor: ['#10B981', '#6366F1', '#9CA3AF', '#EF4444'],
          borderWidth: 0
        }
      ]
    };
    
    // Assign to component property
    this.taskStatusChartData = chartData;
    
    console.log('Updated task status chart data:', this.taskStatusChartData);
    
    // Force chart update
    setTimeout(() => {
      if (this.chart && this.chart.chart) {
        this.chart.chart.update();
        console.log('Chart update triggered');
      } else {
        console.log('Chart reference is not available yet');
      }
    }, 100);
  }

  /**
   * Generate a comprehensive dashboard report as PDF
   */
  async generateDashboardReport(): Promise<void> {
    // Set loading state
    this.isGeneratingReport = true;

    // Add a 3-second delay before starting the report generation
    await timer(3000).pipe(take(1)).toPromise();
    
    try {
      // If current user ID is available
      if (this.currentUser?.id) {
        // If teams aren't loaded, load them
        if (this.managedTeams.length === 0) {
          await new Promise<void>((resolve) => {
            this.teamService.getTeamsByManagerId(this.currentUser!.id)
              .subscribe({
                next: (teamsData) => {
                  this.managedTeams = teamsData;
                  resolve();
                },
                error: () => resolve() // Resolve even on error to continue report generation
              });
          });
        }
        
        // If team performance data isn't loaded, load it
        if (this.originalTeamPerformanceData.length === 0 && this.currentUser?.organizationId) {
          await new Promise<void>((resolve) => {
            this.dashboardStatsService.getTeamPerformanceBarChart(this.currentUser!.organizationId!)
              .subscribe({
                next: (data) => {
                  // Filter to only include managed teams
                  const managedTeamIds = this.managedTeams.map(team => team.id);
                  const filteredData = data.filter(item => 
                    managedTeamIds.includes(item.teamId)
                  );
                  
                  if (filteredData.length > 0) {
                    this.updateTeamPerformanceChart(filteredData);
                  }
                  resolve();
                },
                error: () => resolve() // Resolve even on error
              });
          });
        }
        
        // If collaborator performance data isn't loaded, load it for top performers
        if (this.performanceData.length === 0 && this.managedCollaboratorIds.length > 0 && this.currentUser?.organizationId) {
          await new Promise<void>((resolve) => {
            this.dashboardStatsService.getCollaboratorPerformance(this.currentUser!.organizationId!)
              .subscribe({
                next: (data: CollaboratorPerformanceRangeDto[]) => {
                  this.performanceData = data.filter((item: CollaboratorPerformanceRangeDto) => 
                    this.managedCollaboratorIds.includes(item.collaboratorId)
                  );
                  this.updateTopPerformers();
                  resolve();
                },
                error: () => resolve()
              });
          });
        }
        
        // Prepare metrics data
        const metrics = [
          { 
            title: 'Team Members', 
            value: this.totalCollaborators,
            change: 0  // You could calculate change if you have historical data
          },
          { 
            title: 'Active OKR Sessions', 
            value: this.totalOkrSessions,
            change: 0
          },
          { 
            title: 'Average Performance', 
            value: `${this.averagePerformance}%`,
            change: 0
          },
          { 
            title: 'Delayed Sessions', 
            value: this.delayedSessions,
            change: 0 
          }
        ];
        
        // Format team performance data for the report
        const performanceData = {
          label: 'Team',
          data: this.originalTeamPerformanceData.map(team => ({
            name: team.teamName,
            performance30Days: team.performanceLast30Days,
            performance90Days: team.performanceLast3Months
          }))
        };
        
        // Format task status data
        const taskStatusSummary = this.aggregateTaskStatusData();
        
        // Format top performers data for ranking
        const topPerformersData = this.prepareTopPerformersForReport();
        
        // Add custom sections
        const additionalSections = [
          {
            title: 'Task Status Summary',
            data: [taskStatusSummary],
            columns: [
              { header: 'Completed', property: 'completed' },
              { header: 'In Progress', property: 'inProgress' },
              { header: 'Not Started', property: 'notStarted' },
              { header: 'Overdue', property: 'overdue' }
            ]
          }
        ];
        
        // Prepare dashboard data object
        const dashboardData = {
          metrics,
          performanceData,
          additionalSections,
          teams: this.managedTeams,
          topPerformers: {
            data: topPerformersData
          }
        };
        
        // Generate the PDF report
        await this.pdfExportService.exportDashboardToPdf(
          dashboardData, 
          'Team Manager Dashboard',
          'Your Organization' // You could replace this with the actual organization name if available
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
  
  /**
   * Aggregates task status data across all managed collaborators
   */
  private aggregateTaskStatusData(): any {
    // Sum up all task statuses across collaborators
    let totalCompleted = 0;
    let totalInProgress = 0;
    let totalNotStarted = 0;
    let totalOverdue = 0;
    
    this.taskStatusData.forEach(collaborator => {
      totalCompleted += collaborator.completed;
      totalInProgress += collaborator.inProgress;
      totalNotStarted += collaborator.notStarted;
      totalOverdue += collaborator.overdue;
    });
    
    return {
      completed: totalCompleted,
      inProgress: totalInProgress,
      notStarted: totalNotStarted,
      overdue: totalOverdue
    };
  }

  private prepareTopPerformersForReport(): any[] {
    // Get top performers for both time periods
    const performersData = this.topPerformers
      // Filter out the team manager (current user)
      .filter(performer => performer.collaborator.id !== this.currentUser?.id)
      .map(performer => {
        // Extract relevant data for report
        const collaborator = performer.collaborator;
        
        // Calculate performance scores for different time periods
        let performance30Days = 0;
        let performance90Days = 0;
        
        // Find matching performance data
        const perfData = this.performanceData.find(p => p.collaboratorId === collaborator.id);
        if (perfData) {
          performance30Days = perfData.performanceLast30Days;
          performance90Days = perfData.performanceLast3Months;
        }
        
        return {
          collaboratorId: collaborator.id,
          collaboratorName: `${collaborator.firstName} ${collaborator.lastName}`,
          position: collaborator.position,
          performance30Days: Math.round(performance30Days),
          performance90Days: Math.round(performance90Days),
          tasksCompleted: performer.tasksCompleted,
          onTimeRate: `${Math.round(performer.onTimePercentage)}%`
        };
      });
    
    // Sort by 30-day performance (or switch to 90-day based on current view)
    const sortField = this.topPerformersView === '30days' ? 'performance30Days' : 'performance90Days';
    
    return performersData.sort((a, b) => b[sortField] - a[sortField]).slice(0, 10); // Limit to top 10
  }
} 