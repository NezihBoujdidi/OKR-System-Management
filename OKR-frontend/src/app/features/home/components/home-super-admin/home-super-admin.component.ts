import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardStatsService } from 'src/app/services/dashboard-stats.service';

@Component({
  selector: 'app-home-super-admin',
  templateUrl: './home-super-admin.component.html'
})
export class HomeSuperAdminComponent implements OnInit {
  totalOrganizations = 0;
  orgGrowth = 0;
  totalUsers = 0;
  userGrowth = 0;

  constructor(
    private dashboardStatsService: DashboardStatsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.dashboardStatsService.getGlobalOrgOkrStats().subscribe(data => {
      this.totalOrganizations = data.organizationCount;
      // For demo: orgGrowth is not provided, so set to 0 or fetch if you have it
      this.orgGrowth = 0;
    });

    this.dashboardStatsService.getUserGrowthStats().subscribe(data => {
      if (data && data.monthly && data.monthly.length > 0) {
        this.totalUsers = data.monthly[data.monthly.length - 1].count;
        if (data.monthly.length > 12) {
          const lastYear = data.monthly[data.monthly.length - 13];
          if (lastYear.count > 0) {
            this.userGrowth = Math.round(((this.totalUsers - lastYear.count) / lastYear.count) * 100);
          }
        }
      }
    });
  }

  goToOrganizations() {
    this.router.navigate(['/organization']);
  }
  
  goToUsers() {
    this.router.navigate(['/users']);
  }
  
  goToAnalytics() {
    this.router.navigate(['/dashboard/superAdmin']);
  }
  
  navigateToBestPractices(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.router.navigate(['/okr-best-practices']);
  }
}
