import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { AuthStateService } from '../../../../services/auth-state.service';
import { UserDetailsWithRole } from '../../../../models/user.interface';
import { TeamPerformanceBarDto } from '../../../../models/dashboard-stats.interface';
import { Objective } from '../../../../models/objective.interface';
import { Status } from '../../../../models/Status.enum';

@Component({
  selector: 'app-home-team-manager',
  templateUrl: './home-team-manager.component.html'
})
export class HomeTeamManagerComponent implements OnInit {
  showLoadingOverlay = false;
  currentUser: UserDetailsWithRole | null = null;
  
  // Team performance data
  topPerformingTeam: TeamPerformanceBarDto | null = null;
  
  // Recent objective data
  recentObjective: Objective | null = null;
  objectiveDueText: string = '';
  isObjectiveInProgress: boolean = false;

  constructor(
    private router: Router,
    private dashboardStatsService: DashboardStatsService,
    private authStateService: AuthStateService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authStateService.getCurrentUser();
    
    if (this.currentUser?.id && this.currentUser?.organizationId) {
      this.loadTopPerformingTeam();
      this.loadRecentObjective();
    }
  }

  private loadTopPerformingTeam(): void {
    if (!this.currentUser?.organizationId) return;

    this.dashboardStatsService.getTeamPerformanceBarChart(this.currentUser.organizationId)
      .subscribe({
        next: (teams) => {
          if (teams && teams.length > 0) {
            // Sort teams by 30-day performance (descending)
            const sortedTeams = [...teams].sort((a, b) => 
              b.performanceLast30Days - a.performanceLast30Days
            );
            this.topPerformingTeam = sortedTeams[0];
          }
        },
        error: (error) => {
          console.error('Error loading team performance data:', error);
        }
      });
  }

  private loadRecentObjective(): void {
    if (!this.currentUser?.id) return;

    this.dashboardStatsService.getObjectivesByManagerId(this.currentUser.id)
      .subscribe({
        next: (objectives) => {
          if (objectives && objectives.length > 0) {
            // First try to find in-progress objectives
            const inProgressObjectives = objectives.filter(obj => obj.status === Status.InProgress);
            
            if (inProgressObjectives.length > 0) {
              // Sort by end date (ascending)
              const sortedObjectives = [...inProgressObjectives].sort((a, b) => 
                new Date(a.endDate).getTime() - new Date(b.endDate).getTime()
              );
              this.recentObjective = sortedObjectives[0];
              this.isObjectiveInProgress = true;
              this.objectiveDueText = this.calculateDueText(new Date(this.recentObjective.endDate));
            } else {
              // If no in-progress objectives, find not-started ones
              const notStartedObjectives = objectives.filter(obj => obj.status === Status.NotStarted);
              
              if (notStartedObjectives.length > 0) {
                // Sort by start date (ascending)
                const sortedObjectives = [...notStartedObjectives].sort((a, b) => 
                  new Date(a.startedDate).getTime() - new Date(b.startedDate).getTime()
                );
                this.recentObjective = sortedObjectives[0];
                this.isObjectiveInProgress = false;
                this.objectiveDueText = this.calculateStartsText(new Date(this.recentObjective.startedDate));
              }
            }
          }
        },
        error: (error) => {
          console.error('Error loading manager objectives:', error);
        }
      });
  }

  private calculateDueText(dueDate: Date): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const due = new Date(dueDate);
    due.setHours(0, 0, 0, 0);
    
    const diffTime = due.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Due today';
    if (diffDays === 1) return 'Due tomorrow';
    if (diffDays > 0) return `Due in ${diffDays} days`;
    return `Overdue by ${Math.abs(diffDays)} days`;
  }

  private calculateStartsText(startDate: Date): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const start = new Date(startDate);
    start.setHours(0, 0, 0, 0);
    
    const diffTime = start.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Starts today';
    if (diffDays === 1) return 'Starts tomorrow';
    if (diffDays > 0) return `Starts in ${diffDays} days`;
    return `Started ${Math.abs(diffDays)} days ago`;
  }

  async navigateToTeams() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/teams/teamManager']);
  }

  async navigateToOKRs() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/okrs']);
  }

  async navigateToAnalytics() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/dashboard']);
  }
  
  async viewOKRs() {
    this.showLoadingOverlay = true;
    // Add a small delay for the animation
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/okrs']);
  }
  
  navigateToBestPractices(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.router.navigate(['/okr-best-practices'], {
      state: { from: 'teamManager' }
    });
  }
} 