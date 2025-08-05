import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../../../services/dashboard.service';
import { KeyResultTaskService } from '../../../../services/key-result-task.service';
import { UserService } from '../../../../services/user.service';
import { DashboardStatsService } from '../../../../services/dashboard-stats.service';
import { AuthStateService } from '../../../../services/auth-state.service';
import { KeyMetric, Activity } from '../../../../models/dashboard.interface';
import { KeyResultTask } from '../../../../models/key-result-task.interface';
import { UserDetailsWithRole } from '../../../../models/user.interface';
import { Chart, ChartConfiguration } from 'chart.js';
import { Status } from 'src/app/models/Status.enum';
import { Priority } from 'src/app/models/Priority.enum';
import { 
  CollaboratorPerformanceDto, 
  CollaboratorTaskStatusStatsDto, 
  CollaboratorMonthlyPerformanceDto,
  CollaboratorTaskDetailsDto,
  KeyResultTaskDto
} from '../../../../models/dashboard-stats.interface';
import { PdfExportService } from '../../../../services/pdf-export.service';
import { timer } from 'rxjs';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-collaborator-dashboard',
  templateUrl: './collaborator-dashboard.component.html'
})
export class CollaboratorDashboardComponent implements OnInit {
  keyMetrics: KeyMetric[] = [];
  tasks: KeyResultTask[] = [];
  currentUser: UserDetailsWithRole | null = null;

  completedTasks: number = 0;
  pendingTasks: number = 0;
  overdueTasksCount: number = 0;
  performanceScore: number = 0;
  performanceChange: number = 0;

  taskDetails: CollaboratorTaskDetailsDto | null = null;

  // Task Completion Rate Chart (Donut)
  taskCompletionData: ChartConfiguration<'doughnut'>['data'] = {
    labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
    datasets: [{
      data: [0, 0, 0, 0],
      backgroundColor: [
        '#34d399', // Vibrant green for Completed
        '#60a5fa', // Vibrant blue for In Progress
        '#d1d5db', // Light gray for Not Started
        '#f87171'  // Vibrant red for Overdue
      ],
      borderWidth: 0
    }]
  };

  taskCompletionOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '70%',
    plugins: {
      legend: {
        display: true,
        position: 'right',
        labels: {
          padding: 20,
          usePointStyle: true,
          pointStyle: 'circle'
        }
      },
      tooltip: {
        callbacks: {
          label: function(context) {
            return `${context.label}: ${context.raw}`;
          }
        }
      }
    }
  };

  // Performance Trend Chart (Line)
  performanceTrendData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Performance Score',
        data: [],
        borderColor: '#6366F1',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        tension: 0.4,
        fill: true,
        pointBackgroundColor: '#FFFFFF', // White fill
        pointBorderColor: [], // Will be set dynamically in updatePerformanceTrendChart
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8
      }
    ]
  };

  performanceTrendOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: false
        },
        ticks: {
          callback: function(value) {
            return value + '%';
          }
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
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 13
        },
        displayColors: false,
        callbacks: {
          title: function(tooltipItems) {
            return tooltipItems[0].label;
          },
          label: function(context) {
            return `Performance: ${context.raw}%`;
          }
        }
      }
    },
    elements: {
      line: {
        tension: 0.3
      }
    }
  };

  performanceView: '6months' | '12months' = '6months';

  // Make Math available to the template
  Math = Math;

  // Report generation state
  isGeneratingReport: boolean = false;

  constructor(
    private dashboardService: DashboardService,
    private keyResultTaskService: KeyResultTaskService,
    private userService: UserService,
    private dashboardStatsService: DashboardStatsService,
    private authStateService: AuthStateService,
    private pdfExportService: PdfExportService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authStateService.getCurrentUser();
    
    if (this.currentUser?.id && this.currentUser?.organizationId) {
      this.loadDashboardData();
    }
  }

  private loadDashboardData(): void {
    if (!this.currentUser?.id || !this.currentUser?.organizationId) return;

    // Load performance data
    this.dashboardStatsService.getCollaboratorPerformance(this.currentUser.organizationId)
      .subscribe({
        next: (data) => {
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

    // Load task status data for KPI cards and doughnut chart
    this.dashboardStatsService.getCollaboratorTaskStatusStats(this.currentUser.organizationId)
      .subscribe({
        next: (data) => {
          const collaboratorData = data.find(d => d.collaboratorId === this.currentUser?.id);
          if (collaboratorData) {
            this.completedTasks = collaboratorData.completed;
            this.pendingTasks = collaboratorData.inProgress;
            this.overdueTasksCount = collaboratorData.overdue;
            this.updateTaskCompletionChart(collaboratorData);
          }
        },
        error: (error) => {
          console.error('Error loading task status data:', error);
        }
      });

    // Load monthly performance data for trend chart
    this.loadMonthlyPerformanceData();

    // Load detailed task lists
    this.dashboardStatsService.getCollaboratorTaskDetails(this.currentUser.id)
      .subscribe({
        next: (details) => {
          this.taskDetails = details;
        },
        error: (error) => {
          console.error('Error loading collaborator task details:', error);
          this.taskDetails = {
            recentCompletedTasks: [],
            inProgressTasks: [],
            overdueTasks: []
          };
        }
      });
  }

  private loadMonthlyPerformanceData(): void {
    if (!this.currentUser?.id || !this.currentUser?.organizationId) return;

    this.dashboardStatsService.getCollaboratorMonthlyPerformance(
      this.currentUser.organizationId,
      this.currentUser.id
    ).subscribe({
      next: (data) => {
        this.updatePerformanceTrendChart(data);
      },
      error: (error) => {
        console.error('Error loading monthly performance data:', error);
      }
    });
  }

  private updateTaskCompletionChart(taskData: CollaboratorTaskStatusStatsDto): void {
    this.taskCompletionData = {
      labels: ['Completed', 'In Progress', 'Not Started', 'Overdue'],
      datasets: [{
        data: [
          taskData.completed,
          taskData.inProgress,
          taskData.notStarted,
          taskData.overdue
        ],
        backgroundColor: [
          '#34d399', // Vibrant green for Completed
          '#60a5fa', // Vibrant blue for In Progress
          '#d1d5db', // Light gray for Not Started
          '#f87171'  // Vibrant red for Overdue
        ],
        borderWidth: 0
      }]
    };
  }

  private updatePerformanceTrendChart(data: CollaboratorMonthlyPerformanceDto[]): void {
    // Sort data by year and month
    const sortedData = [...data].sort((a, b) => {
      if (a.year !== b.year) return a.year - b.year;
      return a.month - b.month;
    });

    // Get the last N months based on view
    const monthsToShow = this.performanceView === '6months' ? 6 : 12;
    const recentData = sortedData.slice(-monthsToShow);

    // Format labels and data
    const labels = recentData.map(item => {
      const date = new Date(item.year, item.month - 1);
      return date.toLocaleString('default', { month: 'short' });
    });

    const performanceData = recentData.map(item => item.performance);
    
    // Generate border colors based on performance values
    const borderColors = performanceData.map(value => {
      if (value >= 90) return '#10B981'; // Green for high performance
      if (value >= 75) return '#FFD700'; // Yellow/gold for medium performance
      return '#EF4444'; // Red for lower performance
    });

    this.performanceTrendData = {
      labels: labels,
      datasets: [
        {
          label: 'Performance Score',
          data: performanceData,
          borderColor: '#6366F1',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          tension: 0.4,
          fill: true,
          pointBackgroundColor: '#FFFFFF', // White fill
          pointBorderColor: borderColors, // Colored borders
          pointBorderWidth: 2,
          pointRadius: 6,
          pointHoverRadius: 8
        }
      ]
    };
  }

  togglePerformanceView(view: '6months' | '12months'): void {
    if (this.performanceView !== view) {
      this.performanceView = view;
      this.loadMonthlyPerformanceData();
    }
  }

  getPriorityClass(priority: string | number): { textClass: string, borderClass: string } {
    // Convert to number if it's a string and can be parsed as a number
    const priorityNum = typeof priority === 'string' ? parseInt(priority, 10) : priority;
    
    // First check if we got a valid number 
    if (!isNaN(priorityNum)) {
      switch (priorityNum) {
        case Priority.Low: // 1
          return { textClass: 'text-green-600', borderClass: 'border-l-green-200 bg-green-50' };
        case Priority.Medium: // 2
          return { textClass: 'text-amber-600', borderClass: 'border-l-amber-200 bg-amber-50' };
        case Priority.High: // 3
          return { textClass: 'text-red-600', borderClass: 'border-l-red-200 bg-red-50' };
        case Priority.Urgent: // 4
          return { textClass: 'text-purple-600', borderClass: 'border-l-purple-200 bg-purple-50' };
      }
    }
    
    // Fallback for string values
    const priorityString = String(priority).toLowerCase();
    switch (priorityString) {
      case 'low':
      case '1':
        return { textClass: 'text-green-600', borderClass: 'border-l-green-200 bg-green-50' };
      case 'medium':
      case '2':
        return { textClass: 'text-amber-600', borderClass: 'border-l-amber-200 bg-amber-50' };
      case 'high':
      case '3':
        return { textClass: 'text-red-600', borderClass: 'border-l-red-200 bg-red-50' };
      case 'urgent':
      case '4':
        return { textClass: 'text-purple-600', borderClass: 'border-l-purple-200 bg-purple-50' };
      default:
        return { textClass: 'text-gray-500', borderClass: 'border-l-gray-200 bg-gray-50' };
    }
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'text-white bg-emerald-500 px-2 py-0.5 rounded-full';
      case 'inprogress': return 'text-white bg-blue-500 px-2 py-0.5 rounded-full';
      case 'notstarted': return 'text-white bg-gray-500 px-2 py-0.5 rounded-full';
      case 'overdue': return 'text-white bg-rose-500 px-2 py-0.5 rounded-full';
      default: return 'text-white bg-gray-500 px-2 py-0.5 rounded-full';
    }
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return 'N/A';
    try {
      return new Date(dateString).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    } catch (e) {
      return dateString;
    }
  }

  /**
   * Safely converts a priority value to its text representation
   */
  getPriorityText(priority: string | number): string {
    // Convert to number if it's a string and can be parsed as a number
    const priorityNum = typeof priority === 'string' ? parseInt(priority, 10) : priority;
    
    // Check for valid number values
    if (!isNaN(priorityNum)) {
      switch (priorityNum) {
        case Priority.Low: return 'Low';
        case Priority.Medium: return 'Medium';
        case Priority.High: return 'High';
        case Priority.Urgent: return 'Urgent';
      }
    }
    
    // Fallback for string values
    const priorityString = String(priority).toLowerCase();
    switch (priorityString) {
      case 'low':
      case '1': return 'Low';
      case 'medium':
      case '2': return 'Medium';
      case 'high':
      case '3': return 'High';
      case 'urgent':
      case '4': return 'Urgent';
      default: return String(priority);
    }
  }

  private getTaskStatusText(statusValue: any): string {
    const s = String(statusValue).toLowerCase();
    // User-specified mapping (0-indexed)
    if (s === '1') return 'Not Started';
    if (s === '2') return 'In Progress';
    if (s === '3') return 'Completed';
    if (s === '4') return 'Overdue';
  
    // Fallback for direct string values or existing enum numbers (1-indexed)
    switch (s) {
      case 'notstarted': case 'not started': case Status.NotStarted.toString(): return 'Not Started';
      case 'inprogress': case 'in progress': case Status.InProgress.toString(): return 'In Progress';
      case 'completed': case Status.Completed.toString(): return 'Completed';
      case 'overdue': case Status.Overdue.toString(): return 'Overdue';
      default: return String(statusValue); // Return original if no mapping found
    }
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
      if (!this.currentUser) {
        console.error('Current user not available for report generation.');
        this.isGeneratingReport = false; // Reset flag on early exit
        return;
      }

      // Prepare metrics data
      const metrics = [
        { 
          title: 'Completed Tasks', 
          value: this.completedTasks,
        },
        { 
          title: 'Pending Tasks', 
          value: this.pendingTasks,
        },
        { 
          title: 'Overdue Tasks', 
          value: this.overdueTasksCount,
        },
        { 
          title: 'Performance Score', 
          value: `${this.performanceScore}%`,
          change: this.performanceChange 
        }
      ];
      
      const additionalSections = [];

      // Performance Trend Section
      if (this.performanceTrendData && this.performanceTrendData.labels && this.performanceTrendData.datasets[0].data) {
        const trendData = this.performanceTrendData.labels.map((label, index) => ({
          period: String(label),
          score: this.performanceTrendData.datasets[0].data[index]
        }));
        additionalSections.push({
          title: 'Performance Trend',
          data: trendData.length > 0 ? trendData : [{ message: 'No performance trend data available.'}],
          columns: trendData.length > 0 ? 
            [{ header: 'Period', property: 'period' }, { header: 'Score (%)', property: 'score' }] : 
            [{ header: 'Info', property: 'message' }]
        });
      }

      const taskColumns = [
        { header: 'Task Title', property: 'title' },
        { header: 'Status', property: 'status' },
        { header: 'Priority', property: 'priority' },
        { header: 'Due Date', property: 'dueDate' }
      ];
      const messageColumn = [{ header: 'Info', property: 'message' }];

      // In Progress Tasks Section
      if (this.taskDetails && this.taskDetails.inProgressTasks) {
        additionalSections.push({
          title: 'In Progress Tasks',
          data: this.taskDetails.inProgressTasks.length > 0 ? 
            this.taskDetails.inProgressTasks.map(task => ({
              title: task.title,
              status: this.getTaskStatusText(task.status),
              priority: this.getPriorityText(task.priority),
              dueDate: this.formatDate(task.endDate)
            })) : 
            [{ message: 'All caught up! No tasks in progress.'}],
          columns: this.taskDetails.inProgressTasks.length > 0 ? taskColumns : messageColumn
        });
      }

      // Recently Completed Tasks Section
      if (this.taskDetails && this.taskDetails.recentCompletedTasks) {
        additionalSections.push({
          title: 'Recently Completed Tasks',
          data: this.taskDetails.recentCompletedTasks.length > 0 ? 
            this.taskDetails.recentCompletedTasks.map(task => ({
              title: task.title,
              status: this.getTaskStatusText(task.status),
              priority: this.getPriorityText(task.priority),
              dueDate: this.formatDate(task.endDate)
            })) : 
            [{ message: 'No tasks recently completed.'}],
          columns: this.taskDetails.recentCompletedTasks.length > 0 ? taskColumns : messageColumn
        });
      }

      // Overdue Tasks Section
      if (this.taskDetails && this.taskDetails.overdueTasks) {
        additionalSections.push({
          title: 'Overdue Tasks',
          data: this.taskDetails.overdueTasks.length > 0 ? 
            this.taskDetails.overdueTasks.map(task => ({
              title: task.title,
              status: this.getTaskStatusText(task.status),
              priority: this.getPriorityText(task.priority),
              dueDate: this.formatDate(task.endDate)
            })) : 
            [{ message: 'No overdue tasks! Great job.'}],
          columns: this.taskDetails.overdueTasks.length > 0 ? taskColumns : messageColumn
        });
      }
      
      const dashboardData = {
        metrics,
        additionalSections
      };
      
      // Generate the PDF report
      await this.pdfExportService.exportDashboardToPdf(
        dashboardData, 
        'Collaborator Dashboard Report',
        this.currentUser.firstName + ' ' + this.currentUser.lastName
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