import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardStatsService } from 'src/app/services/dashboard-stats.service';
import { OKRSessionService } from 'src/app/services/okr-session.service';
import { AuthStateService } from 'src/app/services/auth-state.service';
import { TeamPerformanceBarDto } from 'src/app/models/dashboard-stats.interface';
import { OKRSession } from 'src/app/models/okr-session.interface';
import { Status } from 'src/app/models/Status.enum';

@Component({
  selector: 'app-home-organization-admin',
  templateUrl: './home-organization-admin.component.html'
})
export class HomeOrganizationAdminComponent implements OnInit {
  showLoadingOverlay = false;

  topTeam: { name: string; performance: number } | null = null;
  upcomingMilestone: { title: string; daysLeft: number; status: string ; startDate: Date ; endDate: Date } | null = null;

  constructor(
    private router: Router,
    private dashboardStatsService: DashboardStatsService,
    private okrSessionService: OKRSessionService,
    private authState: AuthStateService
  ) {}

  ngOnInit(): void {
    const user = this.authState.getCurrentUser();
    const orgId = user?.organizationId;
    if (!orgId) return;

    // Top Performing Team
    this.dashboardStatsService.getTeamPerformanceBarChart(orgId).subscribe((teams: TeamPerformanceBarDto[]) => {
      if (!teams || teams.length === 0) {
        this.topTeam = null;
        return;
      }
      const maxPerf = Math.max(...teams.map(t => t.performanceLast30Days));
      const topTeams = teams.filter(t => t.performanceLast30Days === maxPerf);
      const chosen = topTeams[Math.floor(Math.random() * topTeams.length)];
      this.topTeam = {
        name: chosen.teamName,
        performance: chosen.performanceLast30Days
      };
    });

    // Upcoming Milestone
    this.okrSessionService.getOKRSessionsByOrganizationId(orgId).subscribe((sessions: OKRSession[]) => {
      const now = new Date();
      const filtered = sessions.filter(s =>
        (s.status === Status.InProgress || s.status === Status.NotStarted) &&
        s.endDate && new Date(s.endDate) > now
      );
      if (filtered.length === 0) {
        this.upcomingMilestone = null;
        return;
      }
      filtered.sort((a, b) => new Date(a.endDate as any).getTime() - new Date(b.endDate as any).getTime());
      const soonest = filtered[0];
      const daysLeft = Math.ceil((new Date(soonest.endDate as any).getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
      const status = soonest.status === Status.NotStarted ? 'Not Started' : 'In Progress';
      this.upcomingMilestone = {
        title: soonest.title,
        daysLeft,
        status: status,
        startDate: new Date(soonest.startedDate),
        endDate: new Date(soonest.endDate)
      };
    });
  }

  // Calculate progress bar width for milestone
  getProgressWidth(): string {
    if (!this.upcomingMilestone) return '0%';
    
    // Assuming a standard OKR period is 90 days
    const Period = Math.ceil((this.upcomingMilestone.endDate.getTime() - this.upcomingMilestone.startDate.getTime()) / (1000 * 60 * 60 * 24));
    const daysLeft = this.upcomingMilestone.daysLeft;
    console.log(Period, daysLeft);
    // Calculate percentage of time elapsed
    const progressPercentage = Math.max(0, Math.min(100, ((Period - daysLeft) / Period) * 100));
    
    return `${progressPercentage}%`;
  }

  async navigateToOKRs() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/okrs']);
  }

  async navigateToCreateTeam() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/teams/organizationAdmin']);
  }

  async navigateToAnalytics() {
    this.showLoadingOverlay = true;
    await new Promise(resolve => setTimeout(resolve, 800));
    this.router.navigate(['/dashboard']);
  }

  navigateToBestPractices(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.router.navigate(['/okr-best-practices'], {
      state: { from: 'organizationAdmin' }
    });
  }
}

