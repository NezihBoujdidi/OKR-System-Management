import { Injectable } from '@angular/core';
import { Objective } from '../models/objective.interface';
import { KeyResult } from '../models/key-result.interface';
import { KeyResultTask } from '../models/key-result-task.interface';
import { OKRSession } from '../models/okr-session.interface';
import { User } from '../models/user.interface';
import { Status } from '../models/Status.enum';
import { BillingHistoryItem } from './subscription.service';

@Injectable({
  providedIn: 'root'
})
export class PdfExportService {
  private readonly PAGE_HEIGHT = 297;
  private readonly MARGIN_BOTTOM = 20;
  private readonly CONTENT_HEIGHT = this.PAGE_HEIGHT - this.MARGIN_BOTTOM;

  async exportOKRSessionToPdf(session: OKRSession, objectives: Objective[], owners: Map<string, any>, keyResults: Map<string, KeyResult[]>, tasks: Map<string, KeyResultTask[]>, insights?: string[]): Promise<void> {
    const { jsPDF } = await import('jspdf');
    const { default: autoTable } = await import('jspdf-autotable');
    
    const doc = new jsPDF();
    
    // Cover Page
    await this.createCoverPage(doc, session, objectives, insights);
    doc.addPage();

    // Content Pages
    objectives.forEach((obj, index) => {
      if (index > 0) {
        doc.addPage();
      }

      // Objective Header
      doc.setFontSize(24);
      doc.setFont('Helvetica', 'bold');
      doc.setTextColor(255, 215, 0);
      doc.text(`Objective ${index + 1}`, 14, 20);
      doc.setFontSize(12);
      doc.setFont('Helvetica', 'normal');
      doc.setTextColor(0, 0, 0);
      doc.text(`Date: ${new Date().toLocaleDateString()}`, 14, 30);
      doc.setLineWidth(0.5);
      doc.setDrawColor(0, 0, 0);
      doc.line(14, 35, 196, 35);

      // Objective Table
      const objectiveData = [{
        title: obj.title,
        description: obj.description,
        owner: owners.get(obj.id)?.firstName || 'N/A',
        status: obj.status,
        progress: obj.progress || 0
      }];

      autoTable(doc, {
        head: [['Objective Title', 'Description', 'Owner', 'Status', 'Progress']],
        body: objectiveData.map(item => [
          item.title,
          item.description,
          item.owner,
          item.status,
          `${item.progress}%`
        ]),
        startY: 40,
        theme: 'grid',
        styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
        headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
        alternateRowStyles: { fillColor: [240, 240, 240] },
        didDrawCell: (data: any) => {
          doc.setDrawColor(0, 0, 0);
          doc.line(data.cell.x, data.cell.y + data.cell.height, data.cell.x + data.cell.width, data.cell.y + data.cell.height); // Bottom border
        }
      });

      let currentY = (doc as any).lastAutoTable.finalY + 20;

      // Key Results Section
      const keyResultsForObjective = keyResults.get(obj.id) || [];
      if (keyResultsForObjective.length > 0) {
        if (currentY > this.CONTENT_HEIGHT - 60) {
          doc.addPage();
          currentY = 20;
        }

        doc.setFontSize(20);
        doc.setFont('Helvetica', 'bold');
        doc.setTextColor(255, 215, 0);
        doc.text('Key Results', 14, currentY);
        currentY += 10;

        const keyResultData = keyResultsForObjective.map(kr => ({
          title: kr.title,
          description: kr.description || 'N/A',
          progress: kr.progress,
          status: kr.status || 'N/A'
        }));

        autoTable(doc, {
          head: [['Key Result Name', 'Description', 'Progress', 'Status']],
          body: keyResultData.map(item => [
            item.title,
            item.description,
            `${item.progress}%`,
            item.status
          ]),
          startY: currentY + 5,
          theme: 'grid',
          styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
          headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
          alternateRowStyles: { fillColor: [240, 240, 240] },
          didDrawCell: (data: any) => {
            doc.setDrawColor(0, 0, 0);
            doc.line(data.cell.x, data.cell.y + data.cell.height, data.cell.x + data.cell.width, data.cell.y + data.cell.height); // Bottom border
          }
        });

        currentY = (doc as any).lastAutoTable.finalY + 20;

        keyResultsForObjective.forEach(kr => {
          const tasksForKeyResult = tasks.get(kr.id) || [];
          if (tasksForKeyResult.length > 0) {
            if (currentY > this.CONTENT_HEIGHT - 60) {
              doc.addPage();
              currentY = 20;
            }

            doc.setFontSize(16);
            doc.setFont('Helvetica', 'bold');
            doc.setTextColor(255, 215, 0);
            doc.text(`Tasks for: ${kr.title}`, 14, currentY);
            currentY += 10;

            const taskData = tasksForKeyResult.map(task => ({
              name: task.title,
              dueDate: task.endDate ? new Date(task.endDate).toLocaleDateString() : 'N/A',
              status: task.status,
              progress: task.progress
            }));

            autoTable(doc, {
              head: [['Task Name', 'Due Date', 'Status', 'Progress']],
              body: taskData.map(item => [
                item.name,
                item.dueDate,
                item.status,
                `${item.progress}%`
              ]),
              startY: currentY + 5,
              theme: 'grid',
              styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
              headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
              alternateRowStyles: { fillColor: [240, 240, 240] },
              didDrawCell: (data: any) => {
                doc.setDrawColor(0, 0, 0);
                doc.line(data.cell.x, data.cell.y + data.cell.height, data.cell.x + data.cell.width, data.cell.y + data.cell.height); // Bottom border
              }
            });
            currentY = (doc as any).lastAutoTable.finalY + 20;
          }
        });
      }
    });
    doc.save(`OKR_Session_${session.title}.pdf`);
  }

  async exportDashboardToPdf(
    dashboardData: {
      metrics?: { title: string; value: number | string; change?: number; icon?: string; }[];
      growthData?: { yearly?: { year: number; count: number }[]; monthly?: { year: number; month: number; count: number }[]; label: string; };
      performanceData?: { label: string; data: any[]; };
      topPerformers?: { data: { collaboratorName: string; performance30Days: number; performance90Days: number; tasksCompleted: number; onTimeRate: string; }[]; };
      sessions?: any[];
      teams?: any[];
      activities?: any[];
      additionalSections?: { title: string; data: any[]; columns: { header: string; property: string }[]; }[];
    },
    title: string = 'Dashboard Report',
    organizationName?: string
  ): Promise<void> {
    const { jsPDF } = await import('jspdf');
    const { default: autoTable } = await import('jspdf-autotable');
    const doc = new jsPDF();
    await this.createDashboardCoverPage(doc, title, organizationName);
    doc.addPage();
    let currentY = 20;
    
    if (dashboardData.metrics && dashboardData.metrics.length > 0) {
      doc.setFontSize(18);
      doc.setFont('Helvetica', 'bold');
      doc.setTextColor(0, 0, 0);
      doc.text('Key Performance Indicators', 14, currentY);
      currentY += 10;
      const metricsData = dashboardData.metrics.map(m => ({
        metric: m.title,
        value: m.value,
        change: m.change !== undefined ? `${m.change > 0 ? '+' : ''}${m.change}%` : 'N/A'
      }));
      autoTable(doc, {
        head: [['Metric', 'Value', 'Change']],
        body: metricsData.map(item => [item.metric, item.value, item.change]),
        startY: currentY,
        theme: 'grid',
        styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
        headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 },
        alternateRowStyles: { fillColor: [240, 240, 240] }
      });
      currentY = (doc as any).lastAutoTable.finalY + 15;
    }
    
    if (dashboardData.growthData) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18);
      doc.setFont('Helvetica', 'bold');
      doc.text(`${dashboardData.growthData.label} Growth`, 14, currentY);
      currentY += 10;
      doc.setFontSize(10);
      doc.setFont('Helvetica', 'normal');
      doc.text('Growth metrics displayed as monthly and yearly counts.', 14, currentY);
      currentY += 10;
      if (dashboardData.growthData.monthly && dashboardData.growthData.monthly.length > 0) {
        const monthlyData = dashboardData.growthData.monthly;
        const tableData = monthlyData.map(m => ({ period: `${this.getMonthName(m.month)} ${m.year}`, count: m.count }));
        autoTable(doc, {
          head: [['Month', 'Count']],
          body: tableData.map(item => [item.period, item.count]),
          startY: currentY, theme: 'grid', styles: { cellPadding: 3, fontSize: 10 },
          headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
        });
        currentY = (doc as any).lastAutoTable.finalY + 15;
      }
      if (dashboardData.growthData.yearly && dashboardData.growthData.yearly.length > 0) {
        if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
        const yearlyData = dashboardData.growthData.yearly;
        const tableData = yearlyData.map(y => ({ year: y.year, count: y.count }));
        autoTable(doc, {
          head: [['Year', 'Count']],
          body: tableData.map(item => [item.year, item.count]),
          startY: currentY, theme: 'grid', styles: { cellPadding: 3, fontSize: 10 },
          headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
        });
        currentY = (doc as any).lastAutoTable.finalY + 15;
      }
    }
    
    if (dashboardData.performanceData) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18);
      doc.setFont('Helvetica', 'bold');
      doc.text(`${dashboardData.performanceData.label} Performance`, 14, currentY);
      currentY += 10;
      doc.setFontSize(10);
      doc.setFont('Helvetica', 'normal');
      doc.text('Performance metrics shown as percentage scores for both 30-day and 90-day periods.', 14, currentY);
      currentY += 10;
      if (dashboardData.performanceData.data && dashboardData.performanceData.data.length > 0) {
        const hasDetailedMetrics = dashboardData.performanceData.data[0].performance30Days !== undefined;
        let headers: string[] = []; 
        let tableData: (string | number)[][] = [];
        if (hasDetailedMetrics) {
          headers = ['Team Name', '30-Day Performance', '90-Day Performance'];
          tableData = dashboardData.performanceData.data.map(item => [item.name, `${item.performance30Days}%`, `${item.performance90Days}%`]);
        } else {
          headers = ['Name', 'Performance'];
          tableData = dashboardData.performanceData.data.map(item => [item.name, item.performance ? `${item.performance}%` : 'N/A']);
        }
        autoTable(doc, {
          head: [headers], body: tableData, startY: currentY, theme: 'grid',
          styles: { cellPadding: 3, fontSize: 10 },
          headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
        });
        currentY = (doc as any).lastAutoTable.finalY + 15;
      }
    }
    
    if (dashboardData.topPerformers && dashboardData.topPerformers.data && dashboardData.topPerformers.data.length > 0) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18); doc.setFont('Helvetica', 'bold'); doc.text('Top Performers Ranking', 14, currentY); currentY += 10;
      doc.setFontSize(10); doc.setFont('Helvetica', 'normal'); doc.text('Ranked collaborators based on performance metrics and task completion.', 14, currentY); currentY += 10;
      const headers = ['Collaborator', '30-Day Score', '90-Day Score', 'Tasks Completed', 'On-Time Rate'];
      const tableData = dashboardData.topPerformers.data.map(item => [item.collaboratorName, `${item.performance30Days}%`, `${item.performance90Days}%`, item.tasksCompleted, item.onTimeRate]);
      autoTable(doc, {
        head: [headers], body: tableData, startY: currentY, theme: 'grid',
        styles: { cellPadding: 3, fontSize: 10 },
        headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
      });
      currentY = (doc as any).lastAutoTable.finalY + 10;
      doc.setFontSize(9); doc.setFont('Helvetica', 'italic'); doc.setTextColor(100, 100, 100);
      doc.text('* On-Time Rate is calculated as the percentage of completed tasks that were finished before their due date:', 14, currentY); currentY += 6;
      doc.text('   (Completed Tasks ÷ (Completed Tasks + Overdue Tasks)) × 100%', 14, currentY);
      doc.setTextColor(0, 0, 0); currentY += 15;
    }
    
    if (dashboardData.teams && dashboardData.teams.length > 0) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18); doc.setFont('Helvetica', 'bold'); doc.text('Teams Overview', 14, currentY); currentY += 10;
      const teamsTableData = dashboardData.teams.map(team => ({ name: team.name, description: team.description || 'No description available' }));
      autoTable(doc, {
        head: [['Team Name', 'Description']], body: teamsTableData.map(item => [item.name, item.description]),
        startY: currentY, theme: 'grid', styles: { cellPadding: 3, fontSize: 10 },
        headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 },
        columnStyles: { 0: { cellWidth: 60 }, 1: { cellWidth: 'auto' } }
      });
      currentY = (doc as any).lastAutoTable.finalY + 15;
    }
    
    if (dashboardData.sessions && dashboardData.sessions.length > 0) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18); doc.setFont('Helvetica', 'bold'); doc.text('OKR Sessions Overview', 14, currentY); currentY += 10;
      const sessionsTableData = dashboardData.sessions.map(session => ({
        title: session.title, startDate: new Date(session.startedDate).toLocaleDateString(),
        endDate: new Date(session.endDate).toLocaleDateString(), progress: session.progress ? `${session.progress}%` : 'N/A', status: session.status
      }));
      autoTable(doc, {
        head: [['Session Title', 'Start Date', 'End Date', 'Progress', 'Status']],
        body: sessionsTableData.map(item => [item.title, item.startDate, item.endDate, item.progress, item.status]),
        startY: currentY, theme: 'grid', styles: { cellPadding: 3, fontSize: 10 },
        headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
      });
      currentY = (doc as any).lastAutoTable.finalY + 15;
    }
    
    if (dashboardData.activities && dashboardData.activities.length > 0) {
      if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
      doc.setFontSize(18); doc.setFont('Helvetica', 'bold'); doc.text('Recent Activities', 14, currentY); currentY += 10;
      const activitiesTableData = dashboardData.activities.map(activity => ({
        description: activity.description, user: activity.user,
        date: typeof activity.date === 'string' ? activity.date : new Date(activity.date).toLocaleString()
      }));
      autoTable(doc, {
        head: [['Description', 'User', 'Date']],
        body: activitiesTableData.map(item => [item.description, item.user, item.date]),
        startY: currentY, theme: 'grid', styles: { cellPadding: 3, fontSize: 10 },
        headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
      });
      currentY = (doc as any).lastAutoTable.finalY + 15;
    }
    
    if (dashboardData.additionalSections && dashboardData.additionalSections.length > 0) {
      for (const section of dashboardData.additionalSections) {
        if (currentY > this.CONTENT_HEIGHT - 60) { doc.addPage(); currentY = 20; }
        doc.setFontSize(18); doc.setFont('Helvetica', 'bold'); doc.text(section.title, 14, currentY); currentY += 10;
        if (section.data && section.data.length > 0) {
          const columnHeaders = section.columns.map(col => col.header);
          const tableData = section.data.map(item => section.columns.map(col => {
              const value = this.getNestedProperty(item, col.property);
              return value !== undefined ? value : 'N/A';
          }));
          autoTable(doc, {
            head: [columnHeaders], body: tableData, startY: currentY, theme: 'grid',
            styles: { cellPadding: 3, fontSize: 10 },
            headStyles: { fillColor: [26, 26, 26], textColor: [255, 215, 0], fontSize: 12 }
          });
          currentY = (doc as any).lastAutoTable.finalY + 15;
        }
      }
    }
    this.addFooter(doc);
    doc.save(`${title.replace(/\s+/g, '_')}_${new Date().toISOString().split('T')[0]}.pdf`);
  }

  async createCoverPage(doc: any, session: OKRSession, objectives: Objective[], insights?: string[]): Promise<void> {
    doc.setFillColor(26, 26, 26);
    doc.rect(0, 0, 220, 297, 'F');
    doc.setFillColor(255, 215, 0);
    doc.rect(0, 40, 220, 4, 'F');
    
    doc.setFontSize(36);
    doc.setFont('Helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text('OKR Report', 20, 30);
    
    doc.setFontSize(24);
    doc.setTextColor(255, 215, 0);
    doc.text(session.title, 20, 80);
    
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text('Session Period:', 20, 100);
    doc.setFont('Helvetica', 'normal');
    doc.text(`${new Date(session.startedDate).toLocaleDateString()} - ${new Date(session.endDate).toLocaleDateString()}`, 20, 110);
    
    let insightsEndY = 110;
    if (insights && insights.length > 0) {
      doc.setFontSize(16);
      doc.setTextColor(255, 215, 0);
      doc.text('AI-Powered Insights', 20, 130);
      doc.setFontSize(12);
      doc.setTextColor(255, 255, 255);
      let y = 140;
      insights.forEach((insight: string, idx: number) => {
        doc.text(`• ${insight}`, 24, y);
        y += 8;
        if (y > 270) { doc.addPage(); y = 30; } // Basic overflow handling
      });
      insightsEndY = y;
    }

    doc.setFont('Helvetica', 'bold');
    doc.setFontSize(12); // Font size for "Overview" title
    doc.setTextColor(255, 255, 255); // White color for "Overview"
    doc.text('Overview', 20, insightsEndY + 20);
    
    doc.setFont('Helvetica', 'normal');
    doc.setFontSize(12);
    const stats = [
      `Total Objectives: ${objectives.length}`,
      `Completed: ${objectives.filter(o => o.status === Status.Completed).length}`,
      `In Progress: ${objectives.filter(o => o.status === Status.InProgress).length}`,
      `Not Started: ${objectives.filter(o => o.status === Status.NotStarted).length}`
    ];
    
    let statsY = insightsEndY + 20 + 20; // Position stats after "Overview" title
    stats.forEach((stat, index) => {
      doc.text(stat, 20, statsY + (index * 10));
    });
    
    doc.setFontSize(10);
    doc.setTextColor(255, 215, 0);
    doc.text('Generated on ' + new Date().toLocaleDateString(), 20, 280);
  }

  private async createDashboardCoverPage(doc: any, title: string, organizationName?: string): Promise<void> {
    doc.setFillColor(26, 26, 26);
    doc.rect(0, 0, 220, 297, 'F');
    doc.setFillColor(255, 215, 0);
    doc.rect(0, 40, 220, 4, 'F');
    doc.setFontSize(36);
    doc.setFont('Helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text('Dashboard Report', 20, 30);
    doc.setFontSize(24);
    doc.setTextColor(255, 215, 0);
    doc.text(title, 20, 80);
    if (organizationName) {
      doc.setFontSize(18);
      doc.setTextColor(255, 255, 255);
      doc.text(`Organization: ${organizationName}`, 20, 100);
    }
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text('Report Period:', 20, 130);
    doc.setFont('Helvetica', 'normal');
    doc.text(`${firstDayOfMonth.toLocaleDateString()} - ${today.toLocaleString()}`, 20, 140);
    doc.setFontSize(10);
    doc.setTextColor(255, 215, 0);
    doc.text(`Generated on ${today.toLocaleString()}`, 20, 280);
  }
  
  private addFooter(doc: any): void {
    const pageCount = doc.internal.getNumberOfPages();
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i);
      if (i > 1) {
        doc.setFontSize(8);
        doc.setTextColor(150, 150, 150);
        doc.text(`Page ${i} of ${pageCount}`, 100, 290, { align: 'center' });
        doc.text(`Generated on ${new Date().toLocaleString()}`, 195, 290, { align: 'right' });
        doc.text('NXM Tensai OKR Management System', 14, 290);
      }
    }
  }

  private buildSessionSummary(objectives: Objective[]): string[] {
    return [
      `Completed: ${objectives.filter(o => o.status === Status.Completed).length}`,
      `In Progress: ${objectives.filter(o => o.status === Status.InProgress).length}`,
      `Not Started: ${objectives.filter(o => o.status === Status.NotStarted).length}`
    ];
  }
  
  private getMonthName(month: number): string {
    const months = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];
    return months[month - 1] || '';
  }
  
  private getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((prev, curr) => {
      return prev ? prev[curr] : null;
    }, obj);
  }

  async exportBillingHistoryToPdf(billingHistory: BillingHistoryItem[]): Promise<void> {
    const { jsPDF } = await import('jspdf');
    const { default: autoTable } = await import('jspdf-autotable');
    const doc = new jsPDF();
    const brandColor = [255, 215, 0]; 
    const darkColor = [50, 50, 50]; 

    doc.setFillColor(brandColor[0], brandColor[1], brandColor[2]);
    doc.rect(0, 0, 210, 50, 'F');
    doc.setFillColor(255, 255, 255);
    doc.setDrawColor(230, 190, 0);
    doc.setFillColor(230, 190, 0);
    doc.circle(35, 25, 16, 'F');
    doc.setFillColor(255, 255, 255);
    doc.circle(33, 23, 15, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(28);
    doc.setTextColor(255, 255, 255);
    doc.text('BILLING HISTORY', 120, 25, { align: 'center' });
    doc.setFontSize(14);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(255, 255, 255);
    doc.text('OKR Assistant', 120, 35, { align: 'center' });
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(darkColor[0], darkColor[1], darkColor[2]);
    const formattedDate = new Date().toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
    });
    doc.text(`Generated: ${formattedDate}`, 20, 65);
    doc.setDrawColor(brandColor[0], brandColor[1], brandColor[2]);
    doc.setLineWidth(0.7);
    doc.line(20, 70, 190, 70);
    const totalAmount = billingHistory.reduce((sum, item) => sum + (item.amount || 0), 0);
    const paidTransactions = billingHistory.filter(item => item.status.toLowerCase() === 'paid').length;
    doc.setFillColor(250, 250, 250);
    doc.setDrawColor(230, 230, 230);
    doc.setLineWidth(0.3);
    doc.roundedRect(20, 75, 170, 35, 3, 3, 'FD');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(darkColor[0], darkColor[1], darkColor[2]);
    doc.text('SUMMARY', 30, 85);
    doc.setDrawColor(230, 230, 230);
    doc.setLineWidth(0.3);
    doc.line(30, 88, 180, 88);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text('Total Transactions:', 30, 96);
    doc.setFont('helvetica', 'bold');
    doc.text(`${billingHistory.length}`, 75, 96);
    doc.setFont('helvetica', 'normal');
    doc.text('Paid Transactions:', 100, 96);
    doc.setFont('helvetica', 'bold');
    doc.text(`${paidTransactions}`, 145, 96);
    doc.setFont('helvetica', 'normal');
    doc.text('Total Amount:', 30, 105);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(Math.round(brandColor[0] / 255 * 80), Math.round(brandColor[1] / 255 * 80), 0);
    doc.text(`${this.formatCurrency(totalAmount, 'USD')}`, 75, 105);
    
    const tableData = billingHistory.map(item => [
      this.formatDate(item.paidAt),
      item.description || 'Subscription Payment',
      this.formatCurrency(item.amount, item.currency),
      item.status.toUpperCase(),
      item.invoiceId || '-'
    ]);
    autoTable(doc, {
      head: [['Date', 'Description', 'Amount', 'Status', 'Invoice ID']],
      body: tableData,
      startY: 120, theme: 'grid',
      styles: { cellPadding: 7, fontSize: 9.5, lineColor: [230, 230, 230], lineWidth: 0.2, font: 'helvetica', valign: 'middle' },
      headStyles: { fillColor: [brandColor[0], brandColor[1], brandColor[2]], textColor: [50, 50, 50], fontSize: 10.5, fontStyle: 'bold', halign: 'left', cellPadding: 8 },
      columnStyles: {
        0: { cellWidth: 45, halign: 'left' }, 1: { cellWidth: 'auto', halign: 'left' },
        2: { cellWidth: 35, halign: 'right', fontStyle: 'bold' }, 3: { cellWidth: 32, halign: 'center' },
        4: { cellWidth: 40, halign: 'center' }
      },
      alternateRowStyles: { fillColor: [252, 252, 252] },
      didDrawCell: (data: any) => { 
        if (data.column.index === 3 && data.section === 'body') {
          const status = data.cell.text[0].toString().toLowerCase();
          const originalFillColor = doc.getFillColor(); 
          const originalTextColor = doc.getTextColor(); 
          if (status === 'paid') { doc.setFillColor(220, 252, 231); doc.setTextColor(22, 163, 74); }
          else if (status === 'draft') { doc.setFillColor(226, 232, 240); doc.setTextColor(71, 85, 105); }
          else if (status === 'unpaid' || status === 'failed') { doc.setFillColor(254, 226, 226); doc.setTextColor(220, 38, 38); }
          else { doc.setFillColor(241, 245, 249); doc.setTextColor(100, 116, 139); }
          const padding = 1.8; const radius = 4;
          const x = data.cell.x + padding; const yPos = data.cell.y + padding + 1; // Renamed y to yPos
          const width = data.cell.width - (padding * 2); const height = data.cell.height - (padding * 2) - 2;
          doc.roundedRect(x, yPos, width, height, radius, radius, 'F');
          doc.setFillColor(originalFillColor); 
          doc.setTextColor(originalTextColor); 
        }
      }
    });
    const pageCountBilling = (doc as any).internal.getNumberOfPages(); 
    for (let i = 1; i <= pageCountBilling; i++) {
      doc.setPage(i);
      doc.setDrawColor(220, 220, 220); doc.setLineWidth(0.3); doc.line(20, 280, 190, 280);
      doc.setFontSize(8); doc.setTextColor(130, 130, 130);
      doc.text(`OKR Assistant | Billing History Report | Page ${i} of ${pageCountBilling}`, 105, 287, { align: 'center' });
      doc.setFillColor(brandColor[0], brandColor[1], brandColor[2]);
      doc.circle(20, 285, 2.5, 'F');
    }
    const dateStr = new Date().toISOString().split('T')[0];
    doc.save(`Billing_History_${dateStr}.pdf`);
  }
  
  private formatDate(date: Date | string | undefined): string { // Added undefined type
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
  }
  
  private formatCurrency(amount: number | undefined | null, currency: string | undefined): string { // Added undefined types
    if (amount === undefined || amount === null) {
      return '$0.00';
    }
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: (currency || 'USD').toUpperCase()
    }).format(amount);
  }
}

