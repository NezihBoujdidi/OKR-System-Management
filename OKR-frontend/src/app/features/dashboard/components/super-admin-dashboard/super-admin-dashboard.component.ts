import { Component, OnInit } from '@angular/core';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { AuthStateService } from '../../../../services/auth-state.service';
import { UserDetailsWithRole } from '../../../../models/user.interface';
import { 
  GlobalOrgOkrStatsDto,
  UserGrowthStatsDto,
  UserRolesCountDto,
  YearlyGrowthDto,
  MonthlyGrowthDto
} from '../../../../models/dashboard-stats.interface';
import { PdfExportService } from '../../../../services/pdf-export.service';
import { timer } from 'rxjs';
import { take } from 'rxjs/operators';
import { SubscriptionService, SuperAdminDashboard, PlanDistributionItem } from '../../../../services/subscription.service';

@Component({
  selector: 'app-super-admin-dashboard',
  templateUrl: './super-admin-dashboard.component.html'
})
export class SuperAdminDashboardComponent implements OnInit {
  // Stats
  totalUsers: number = 0;
  totalOrganizations: number = 0;
  totalSubscribedOrgs: number = 0;
  totalOkrSessions: number = 0;
  
  // Growth metrics
  userGrowthPercentage: number = 0;
  organizationGrowthPercentage: number = 0;
  subscriptionGrowthPercentage: number = 0;
  
  // Chart view states
  userGrowthView: 'monthly' | 'yearly' = 'monthly';
  
  // Data storage
  userGrowthMonthly: MonthlyGrowthDto[] = [];
  userGrowthYearly: YearlyGrowthDto[] = [];
  
  // Current user
  currentUser: UserDetailsWithRole | null = null;

  // Math reference for template use
  Math = Math;
  
  // Report generation state
  isGeneratingReport: boolean = false;

  // Subscription metrics
  subscriptionStats: SuperAdminDashboard | null = null;
  mrr: number = 0;
  arr: number = 0;
  arpu: number = 0;
  churnRate: number = 0;
  planDistribution: PlanDistributionItem[] = [];

  // User Growth Chart
  userGrowthData: ChartConfiguration['data'] = {
    labels: [],
    datasets: [
      {
        label: 'User Growth',
        data: [],
        borderColor: '#6366F1',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        tension: 0.4,
        borderWidth: 2,
        fill: true,
        pointBackgroundColor: '#FFFFFF',
        pointBorderColor: [], // Will be set dynamically
        pointBorderWidth: 2,
        pointRadius: 5,
        pointHoverRadius: 7
      }
    ]
  };

  userGrowthOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          font: {
            size: 11
          }
        }
      },
      x: {
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 11
          }
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
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 13
        },
        displayColors: false,
        callbacks: {
          label: (context) => {
            return `Users: ${context.parsed.y}`;
          }
        }
      }
    },
    elements: {
      line: {
        tension: 0.4
      }
    }
  };

  // User Roles Distribution Chart
  userRolesData: ChartConfiguration<'doughnut'>['data'] = {
    labels: ['Organization Admins', 'Team Managers', 'Collaborators'],
    datasets: [{
      data: [0, 0, 0],
      backgroundColor: [
        '#6366F1', // Indigo for Org Admins
        '#10B981', // Green for Team Managers 
        '#F59E0B'  // Amber for Collaborators
      ],
      borderWidth: 0,
      hoverOffset: 4
    }]
  };

  userRolesOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '70%',
    plugins: {
      legend: {
        position: 'right',
        labels: {
          padding: 20,
          usePointStyle: true,
          pointStyle: 'circle',
          font: {
            size: 12
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        padding: 10,
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 13
        },
        callbacks: {
          label: (context) => {
            const value = context.raw as number;
            const total = (context.dataset.data as number[]).reduce((a, b) => a + b, 0);
            const percentage = total > 0 ? Math.round((value / total) * 100) : 0;
            return `${context.label}: ${value} (${percentage}%)`;
          }
        }
      }
    }
  };

  // Plan Distribution Chart
  planDistributionData: ChartConfiguration<'pie'>['data'] = {
    labels: [],
    datasets: [{
      data: [],
      backgroundColor: [
        '#6366F1', // Indigo
        '#10B981', // Green
        '#F59E0B', // Amber
        '#EF4444'  // Red
      ],
      borderWidth: 0,
      hoverOffset: 4
    }]
  };

  planDistributionOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'right',
        labels: {
          padding: 20,
          usePointStyle: true,
          pointStyle: 'circle',
          font: {
            size: 12
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        padding: 10,
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 13
        },
        callbacks: {
          label: (context) => {
            const value = context.raw as number;
            const total = (context.dataset.data as number[]).reduce((a, b) => a + b, 0);
            const percentage = total > 0 ? Math.round((value / total) * 100) : 0;
            return `${context.label}: ${value} (${percentage}%)`;
          }
        }
      }
    }
  };

  constructor(
    private dashboardStatsService: DashboardStatsService,
    private authState: AuthStateService,
    private pdfExportService: PdfExportService,
    private subscriptionService: SubscriptionService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authState.getCurrentUser();
    this.loadDashboardData();
    this.loadSubscriptionData();
  }

  private loadDashboardData(): void {
    // Load User Growth Stats
    this.dashboardStatsService.getUserGrowthStats()
      .subscribe({
        next: (data) => {
          console.log('User Growth data loaded:', data);
          
          if (data) {
            this.userGrowthMonthly = data.monthly || [];
            this.userGrowthYearly = data.yearly || [];
            
            // Update the chart based on current view
            this.updateUserGrowthChart();
            
            // Get the current user count and calculate growth percentage
            const monthly = this.userGrowthMonthly;
            if (monthly && monthly.length > 0) {
              // Get current month's count
              const currentMonth = monthly[monthly.length - 1];
              this.totalUsers = currentMonth.count;
              
              // Get last year's count if available
              if (monthly.length > 12) {
                const lastYear = monthly[monthly.length - 13];
                if (lastYear.count > 0) {
                  this.userGrowthPercentage = Math.round(((this.totalUsers - lastYear.count) / lastYear.count) * 100);
                }
              }
            }
          }
        },
        error: (error) => {
          console.error('Error loading user growth stats:', error);
        }
      });

    // Load Global Organization OKR Stats
    this.dashboardStatsService.getGlobalOrgOkrStats()
      .subscribe({
        next: (data) => {
          console.log('Global Organization OKR stats loaded:', data);
          this.totalOrganizations = data.organizationCount;
          this.totalOkrSessions = data.okrSessionCount;
          
          // Calculate derived metrics
          this.calculateDerivedMetrics();
        },
        error: (error) => {
          console.error('Error loading global organization OKR stats:', error);
        }
      });

    // Load Paid Organization Count
    this.dashboardStatsService.getPaidOrgCount()
      .subscribe({
        next: (data) => {
          console.log('Paid organization count loaded:', data);
          this.totalSubscribedOrgs = data.paidOrganizations;
          
          // Calculate subscription percentage if organizations data is available
          if (this.totalOrganizations > 0) {
            this.subscriptionGrowthPercentage = Math.round((this.totalSubscribedOrgs / this.totalOrganizations) * 100);
          }
          
          // Recalculate metrics in case this loads after the org stats
          this.calculateDerivedMetrics();
        },
        error: (error) => {
          console.error('Error loading paid organization count:', error);
        }
      });

    // Load User Roles Count
    this.dashboardStatsService.getUserRolesCount()
      .subscribe({
        next: (data) => {
          console.log('User roles count loaded:', data);
          this.updateUserRolesChart(data);
        },
        error: (error) => {
          console.error('Error loading user roles count:', error);
        }
      });
  }

  private loadSubscriptionData(): void {
    this.subscriptionService.getSuperAdminDashboard()
      .subscribe({
        next: (data) => {
          console.log('Subscription dashboard data loaded:', data);
          this.subscriptionStats = data;
          this.mrr = data.mrr;
          this.arr = data.arr;
          this.arpu = data.arpu;
          this.churnRate = data.churnRate;
          this.planDistribution = data.planDistribution;
          
          // Update the plan distribution chart
          this.updatePlanDistributionChart();
        },
        error: (error) => {
          console.error('Error loading subscription dashboard data:', error);
        }
      });
  }

  private updatePlanDistributionChart(): void {
    if (!this.planDistribution || this.planDistribution.length === 0) return;
    
    // Extract labels and data
    const labels = this.planDistribution.map(item => item.plan);
    const data = this.planDistribution.map(item => item.count);
    
    // Update chart data
    this.planDistributionData.labels = labels;
    this.planDistributionData.datasets[0].data = data;
    
    // Create or update chart
    const chartElement = document.getElementById('planDistributionChart') as HTMLCanvasElement;
    if (chartElement) {
      new Chart(chartElement, {
        type: 'pie',
        data: this.planDistributionData,
        options: this.planDistributionOptions
      });
    }
  }

  /**
   * Calculate derived metrics from the loaded data
   */
  private calculateDerivedMetrics(): void {
    // Calculate subscription percentage
    if (this.totalOrganizations > 0) {
      this.subscriptionGrowthPercentage = Math.round((this.totalSubscribedOrgs / this.totalOrganizations) * 100);
    }
    
    // Add more derived calculations as needed
  }

  private updateUserGrowthChart(): void {
    if (this.userGrowthView === 'monthly') {
      this.updateMonthlyUserGrowthChart();
    } else {
      this.updateYearlyUserGrowthChart();
    }
  }

  private updateMonthlyUserGrowthChart(): void {
    const monthlyData = this.userGrowthMonthly;
    if (!monthlyData || monthlyData.length === 0) return;
    
    // Limit to the last 12 months for better visualization
    const recentMonths = monthlyData.slice(-12);
    
    const labels = recentMonths.map(item => {
      const date = new Date(item.year, item.month - 1);
      return date.toLocaleString('default', { month: 'short', year: '2-digit' });
    });
    
    const data = recentMonths.map(item => item.count);
    
    // Generate point border colors based on values
    const pointBorderColors = this.generatePointColors(data);
    
    this.userGrowthData = {
      labels: labels,
      datasets: [
        {
          label: 'User Growth',
          data: data,
          borderColor: '#6366F1',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          tension: 0.4,
          borderWidth: 2,
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

  private updateYearlyUserGrowthChart(): void {
    const yearlyData = this.userGrowthYearly;
    if (!yearlyData || yearlyData.length === 0) return;
    
    const labels = yearlyData.map(item => item.year.toString());
    const data = yearlyData.map(item => item.count);
    
    // Generate point border colors based on values
    const pointBorderColors = this.generatePointColors(data);
    
    this.userGrowthData = {
      labels: labels,
      datasets: [
        {
          label: 'User Growth',
          data: data,
          borderColor: '#6366F1',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          tension: 0.4,
          borderWidth: 2,
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
        return '#6366F1'; // Indigo for medium-high values
      } else if (normalizedValue > 0.3) {
        return '#F59E0B'; // Amber for medium-low values
      } else {
        return '#EF4444'; // Red for low values
      }
    });
  }

  private updateUserRolesChart(data: UserRolesCountDto): void {
    // Calculate total for percentage calculation
    const total = data.organizationAdmins + data.teamManagers + data.collaborators;
    
    this.userRolesData = {
      labels: ['Organization Admins', 'Team Managers', 'Collaborators'],
      datasets: [{
        data: [data.organizationAdmins, data.teamManagers, data.collaborators],
        backgroundColor: [
          '#6366F1', // Indigo for Org Admins
          '#10B981', // Green for Team Managers 
          '#F59E0B'  // Amber for Collaborators
        ],
        borderWidth: 0,
        hoverOffset: 4
      }]
    };
  }

  toggleUserGrowthView(view: 'monthly' | 'yearly'): void {
    if (this.userGrowthView !== view) {
      this.userGrowthView = view;
      this.updateUserGrowthChart();
    }
  }

  getTrendIcon(trend: number): string {
    return trend > 0 ? '↑' : trend < 0 ? '↓' : '→';
  }

  getTrendClass(trend: number): string {
    return trend > 0 ? 'text-green-600' : trend < 0 ? 'text-red-600' : 'text-gray-600';
  }
  
  /**
   * Calculate subscription growth percentage
   */
  getSubscriptionPercentage(): number {
    if (!this.totalOrganizations) return 0;
    return Math.round((this.totalSubscribedOrgs / this.totalOrganizations) * 100);
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
      // Prepare metrics data
      const metrics = [
        { 
          title: 'Total Users', 
          value: this.totalUsers,
          change: this.userGrowthPercentage
        },
        { 
          title: 'Organizations', 
          value: this.totalOrganizations,
          change: this.organizationGrowthPercentage
        },
        { 
          title: 'Subscribed Organizations', 
          value: this.totalSubscribedOrgs,
          change: this.subscriptionGrowthPercentage
        },
        { 
          title: 'Active OKR Sessions', 
          value: this.totalOkrSessions,
          change: 0 // We don't have this data yet
        }
      ];
      
      // Format user growth data for the report
      const growthData = {
        label: 'User',
        monthly: this.userGrowthMonthly,
        yearly: this.userGrowthYearly
      };
      
      // Format user roles data
      const rolesData = {
        title: 'User Role Distribution',
        data: [{
          name: 'Organization Admins',
          value: this.userRolesData.datasets[0].data[0]
        }, {
          name: 'Team Managers',
          value: this.userRolesData.datasets[0].data[1]
        }, {
          name: 'Collaborators',
          value: this.userRolesData.datasets[0].data[2]
        }]
      };
      
      // Add custom sections
      const additionalSections = [
        {
          title: 'User Role Distribution',
          data: rolesData.data,
          columns: [
            { header: 'Role', property: 'name' },
            { header: 'Count', property: 'value' }
          ]
        },
        {
          title: 'Platform Metrics',
          data: [{
            totalOrgs: this.totalOrganizations,
            subscribedOrgs: this.totalSubscribedOrgs,
            subscriptionRate: `${this.getSubscriptionPercentage()}%`,
            totalSessions: this.totalOkrSessions
          }],
          columns: [
            { header: 'Total Organizations', property: 'totalOrgs' },
            { header: 'Subscribed Organizations', property: 'subscribedOrgs' },
            { header: 'Subscription Rate', property: 'subscriptionRate' },
            { header: 'Total OKR Sessions', property: 'totalSessions' }
          ]
        }
      ];
      
      // Prepare dashboard data object
      const dashboardData = {
        metrics,
        growthData,
        additionalSections
      };
      
      // Generate the PDF report
      await this.pdfExportService.exportDashboardToPdf(
        dashboardData, 
        'Super Admin Dashboard',
        'Platform Overview'
      );
    } catch (error) {
      console.error('Error generating dashboard report:', error);
      // Handle error - show a notification to the user
    } finally {
      // Reset loading state immediately
      this.isGeneratingReport = false;
    }
  }
}

