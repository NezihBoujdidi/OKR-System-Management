import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { AuthStateService } from '../../../../services/auth-state.service';
import { UserDetailsWithRole } from '../../../../models/user.interface';
import { KeyResultTaskDto, CollaboratorTaskDetailsDto, CollaboratorPerformanceRangeDto } from '../../../../models/dashboard-stats.interface';

@Component({
  selector: 'app-home-collaborator',
  templateUrl: './home-collaborator.component.html'
})
export class HomeCollaboratorComponent implements OnInit {
  showLoadingOverlay = false;
  currentUser: UserDetailsWithRole | null = null;
  
  // Task data
  recentTask: KeyResultTaskDto | null = null;
  taskCompletion: number = 0;
  isTaskInProgress: boolean = false;
  daysText: string = '';
  
  // Performance data
  performanceScore: number = 0;
  performanceChange: number = 0;

  constructor(
    private router: Router,
    private dashboardStatsService: DashboardStatsService,
    private authStateService: AuthStateService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authStateService.getCurrentUser();
    
    if (this.currentUser?.id && this.currentUser?.organizationId) {
      this.loadTaskData();
      this.loadPerformanceData();
    }
  }

  private loadTaskData(): void {
    if (!this.currentUser?.id) return;

    this.dashboardStatsService.getCollaboratorTaskDetails(this.currentUser.id)
      .subscribe({
        next: (details: CollaboratorTaskDetailsDto) => {
          // First try to get an in-progress task with the soonest due date
          if (details.inProgressTasks && details.inProgressTasks.length > 0) {
            // Sort by end date (ascending)
            const sortedTasks = [...details.inProgressTasks].sort((a, b) => 
              new Date(a.endDate).getTime() - new Date(b.endDate).getTime()
            );
            this.recentTask = sortedTasks[0];
            this.isTaskInProgress = true;
            this.taskCompletion = this.recentTask.progress || 0;
            
            // Calculate days until due
            const daysUntilDue = this.calculateDaysDifference(new Date(), new Date(this.recentTask.endDate));
            this.daysText = daysUntilDue === 0 ? 'Due today' : 
                           daysUntilDue === 1 ? 'Due tomorrow' : 
                           daysUntilDue > 0 ? `Due in ${daysUntilDue} days` : 
                           `Overdue by ${Math.abs(daysUntilDue)} days`;
          } 
          // If no in-progress tasks, try to get the most recently completed task
          else if (details.recentCompletedTasks && details.recentCompletedTasks.length > 0) {
            // Sort by end date (descending to get most recent)
            const sortedTasks = [...details.recentCompletedTasks].sort((a, b) => 
              new Date(b.endDate).getTime() - new Date(a.endDate).getTime()
            );
            this.recentTask = sortedTasks[0];
            this.isTaskInProgress = false;
            this.taskCompletion = 100;
            
            // Calculate days since completion
            const daysSinceCompletion = this.calculateDaysDifference(new Date(this.recentTask.endDate), new Date());
            this.daysText = daysSinceCompletion === 0 ? 'Completed today' : 
                           daysSinceCompletion === 1 ? 'Completed yesterday' : 
                           `Completed ${daysSinceCompletion} days ago`;
          }
        },
        error: (error) => {
          console.error('Error loading collaborator task details:', error);
        }
      });
  }

  private loadPerformanceData(): void {
    if (!this.currentUser?.id || !this.currentUser?.organizationId) return;

    this.dashboardStatsService.getCollaboratorPerformance(this.currentUser.organizationId)
      .subscribe({
        next: (data: CollaboratorPerformanceRangeDto[]) => {
          const collaboratorData = data.find(d => d.collaboratorId === this.currentUser?.id);
          if (collaboratorData) {
            this.performanceScore = collaboratorData.performanceLast30Days;
            this.performanceChange = collaboratorData.performanceLast30Days - collaboratorData.performanceLast3Months;
          }
        },
        error: (error) => {
          console.error('Error loading performance data:', error);
        }
      });
  }

  private calculateDaysDifference(date1: Date, date2: Date): number {
    // Reset time part to compare only dates
    const d1 = new Date(date1);
    d1.setHours(0, 0, 0, 0);
    const d2 = new Date(date2);
    d2.setHours(0, 0, 0, 0);
    
    // Calculate the difference in days
    const diffTime = d2.getTime() - d1.getTime();
    return Math.round(diffTime / (1000 * 60 * 60 * 24));
  }

  async navigateToTeams() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/teams/collaborator']);
  }

  async navigateToTasks() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/okrs']);
  }

  async navigateToAnalytics() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/dashboard']);
  }

  navigateToBestPractices(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.router.navigate(['/okr-best-practices']);
  }
} 