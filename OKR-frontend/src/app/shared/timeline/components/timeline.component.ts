import { Component, Input, OnInit, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { TimelineConfig, TimelineSession } from '../../../models/timeline.interface';

@Component({
  selector: 'app-timeline',
  templateUrl: './timeline.component.html',
  styles: [`
    :host {
      display: block;
      width: 100%;
      position: relative;
      z-index: 1;
    }
    
    /* Ensure the current day bubble is visible */
    .current-day-bubble {
      position: fixed;
      z-index: 9999;
    }
  `]
})
export class TimelineComponent implements OnInit, OnChanges {
  @Input() config!: TimelineConfig;
  @Output() yearChange = new EventEmitter<number>();
  @Output() viewAll = new EventEmitter<void>();

  months = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
  currentYear: number = new Date().getFullYear();
  visibleSessions: TimelineSession[] = [];
  showAllSessions = false;

  ngOnInit() {
    if (this.config?.currentYear) {
      this.currentYear = this.config.currentYear;
    }
    this.updateVisibleSessions();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['config']) {
      if (this.config?.currentYear) {
        this.currentYear = this.config.currentYear;
      }
      this.updateVisibleSessions();
    }
  }

  changeYear(delta: number) {
    this.currentYear += delta;
    this.yearChange.emit(this.currentYear);
  }

  viewAllSessions() {
    this.showAllSessions = !this.showAllSessions;
    this.viewAll.emit();
  }

  getDisplayedSessions(): TimelineSession[] {
    return this.showAllSessions ? this.visibleSessions : this.visibleSessions.slice(0, 3);
  }

  updateVisibleSessions() {
    if (!this.config?.sessions) {
      this.visibleSessions = [];
      return;
    }

    const yearStart = new Date(this.currentYear, 0, 1);
    const yearEnd = new Date(this.currentYear, 11, 31, 23, 59, 59);

    this.visibleSessions = this.config.sessions.filter(session => {
      try {
        const start = new Date(session.startDate);
        const end = new Date(session.endDate);
        return (start <= yearEnd && end >= yearStart);
      } catch (e) {
        console.error('Invalid date in session:', session);
        return false;
      }
    });
  }

  getSessionLeftPosition(session: TimelineSession): string {
    try {
      const sessionStart = new Date(session.startDate);
      const yearStart = new Date(this.currentYear, 0, 1);
      const yearEnd = new Date(this.currentYear, 11, 31, 23, 59, 59);
      
      // If session starts before this year, align to beginning of year
      const effectiveStart = sessionStart < yearStart ? yearStart : sessionStart;
      
      const totalDays = this.getDaysInYear(this.currentYear);
      const daysFromYearStart = this.getDaysDifference(yearStart, effectiveStart);
      
      return `${(daysFromYearStart / totalDays) * 100}%`;
    } catch (e) {
      console.error('Error calculating left position:', e);
      return '0%';
    }
  }

  getSessionWidth(session: TimelineSession): string {
    try {
      const sessionStart = new Date(session.startDate);
      const sessionEnd = new Date(session.endDate);
      const yearStart = new Date(this.currentYear, 0, 1);
      const yearEnd = new Date(this.currentYear, 11, 31, 23, 59, 59);
      
      // Adjust start and end dates if they fall outside current year
      const effectiveStart = sessionStart < yearStart ? yearStart : sessionStart;
      const effectiveEnd = sessionEnd > yearEnd ? yearEnd : sessionEnd;
      
      const totalDays = this.getDaysInYear(this.currentYear);
      const sessionDays = Math.max(1, this.getDaysDifference(effectiveStart, effectiveEnd));
      
      return `${(sessionDays / totalDays) * 100}%`;
    } catch (e) {
      console.error('Error calculating width:', e);
      return '10%'; // Minimum width for visibility
    }
  }

  getCurrentDatePosition(): string {
    const now = new Date();
    const yearStart = new Date(this.currentYear, 0, 1);
    const totalDays = this.getDaysInYear(this.currentYear);
    
    // If we're viewing a different year, position the indicator based on today's month/day in the viewed year
    const targetDate = new Date(this.currentYear, now.getMonth(), now.getDate());
    const daysPassed = this.getDaysDifference(yearStart, targetDate);
    
    return `${(daysPassed / totalDays) * 100}%`;
  }

  getCurrentDayOfTheMonth(): string {
    const now = new Date();
    const currentMonth = this.months[now.getMonth()];
    const currentDay = now.getDate();
    return `${currentDay} ${currentMonth}`;
  }

  isCurrentYear(): boolean {
    return true; // Always return true to show the indicator
  }

  formatDuration(session: TimelineSession): string {
    try {
      const start = new Date(session.startDate);
      const end = new Date(session.endDate);
      
      const months = (end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth());
      
      if (months === 0) {
        const days = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
        return `${days} day${days !== 1 ? 's' : ''}`;
      } else if (months < 12) {
        return `${months} month${months !== 1 ? 's' : ''}`;
      } else {
        const years = Math.floor(months / 12);
        const remainingMonths = months % 12;
        if (remainingMonths === 0) {
          return `${years} year${years !== 1 ? 's' : ''}`;
        } else {
          return `${years}y ${remainingMonths}m`;
        }
      }
    } catch (e) {
      return 'Invalid dates';
    }
  }

  getDaysInMonth(month: number): number {
    return new Date(this.currentYear, month, 0).getDate();
  }

  getDaysInYear(year: number): number {
    return this.isLeapYear(year) ? 366 : 365;
  }

  isLeapYear(year: number): boolean {
    return (year % 4 === 0 && year % 100 !== 0) || (year % 400 === 0);
  }

  getDaysDifference(start: Date, end: Date): number {
    // Create new Date objects to avoid modifying the originals
    const startDate = new Date(start);
    const endDate = new Date(end);
    
    // Set to beginning/end of day to ensure full days are counted
    startDate.setHours(0, 0, 0, 0);
    endDate.setHours(23, 59, 59, 999);
    
    // Calculate difference in milliseconds and convert to days
    const diffTime = endDate.getTime() - startDate.getTime();
    return Math.max(1, Math.ceil(diffTime / (1000 * 60 * 60 * 24)));
  }

  getTimelineHeight(): string {
    const sessionCount = this.visibleSessions.length;
    if (sessionCount === 0) {
      return '100px'; // Minimum height when no sessions
    }
    return `${Math.max(100, sessionCount * 56)}px`;
  }

  getFullMonthName(monthIndex: number): string {
    const monthNames = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];
    return monthNames[monthIndex];
  }

  getMonthDays(monthIndex: number): number[] {
    const firstDay = new Date(this.currentYear, monthIndex, 1);
    const daysInMonth = this.getDaysInMonth(monthIndex + 1);
    const dayOfWeek = firstDay.getDay(); // 0 = Sunday, 1 = Monday, etc.
    
    // Create array with empty slots for days before the 1st of the month
    const days: number[] = Array(dayOfWeek).fill(0);
    
    // Add the actual days of the month
    for (let i = 1; i <= daysInMonth; i++) {
      days.push(i);
    }
    
    return days;
  }

  hasSessionOnDay(monthIndex: number, day: number): boolean {
    if (!this.config?.sessions) return false;
    
    const date = new Date(this.currentYear, monthIndex, day);
    
    return this.visibleSessions.some(session => {
      try {
        const start = new Date(session.startDate);
        const end = new Date(session.endDate);
        return date >= start && date <= end;
      } catch (e) {
        return false;
      }
    });
  }

  getSessionClassForDay(monthIndex: number, day: number): string {
    const sessionInfo = this.getSessionInfoForDay(monthIndex, day);
    
    if (!sessionInfo.hasSession) {
      return 'h-5 w-5 rounded-full text-[10px] flex items-center justify-center text-gray-600';
    }
    
    // If there are multiple sessions on this day
    if (sessionInfo.sessionCount > 1) {
      return 'h-5 w-5 rounded-full text-[10px] flex items-center justify-center text-white font-medium border-2 border-white shadow-sm';
    }
    
    // Single session
    return 'h-5 w-5 rounded-full text-[10px] flex items-center justify-center text-white font-medium';
  }

  getSessionColorForDay(monthIndex: number, day: number): string {
    const sessionInfo = this.getSessionInfoForDay(monthIndex, day);
    
    if (!sessionInfo.hasSession) {
      return 'transparent';
    }
    
    // If there are multiple sessions, use a gradient
    if (sessionInfo.sessionCount > 1) {
      // Return the first session's color as fallback
      return sessionInfo.sessionColors[0] || 'transparent';
    }
    
    // Single session
    return sessionInfo.sessionColors[0] || 'transparent';
  }

  getSessionTitleForDay(monthIndex: number, day: number): string {
    const sessionInfo = this.getSessionInfoForDay(monthIndex, day);
    
    if (!sessionInfo.hasSession) {
      return '';
    }
    
    if (sessionInfo.sessionCount > 1) {
      return `${sessionInfo.sessionCount} sessions on this day: ${sessionInfo.sessionTitles.join(', ')}`;
    }
    
    return sessionInfo.sessionTitles[0] || '';
  }

  getSessionInfoForDay(monthIndex: number, day: number): { 
    hasSession: boolean; 
    sessionCount: number; 
    sessionColors: string[];
    sessionTitles: string[];
  } {
    if (!this.config?.sessions) {
      return { hasSession: false, sessionCount: 0, sessionColors: [], sessionTitles: [] };
    }
    
    const date = new Date(this.currentYear, monthIndex, day);
    const sessionsOnDay = this.visibleSessions.filter(session => {
      try {
        const start = new Date(session.startDate);
        const end = new Date(session.endDate);
        return date >= start && date <= end;
      } catch (e) {
        return false;
      }
    });
    
    if (sessionsOnDay.length === 0) {
      return { hasSession: false, sessionCount: 0, sessionColors: [], sessionTitles: [] };
    }
    
    return {
      hasSession: true,
      sessionCount: sessionsOnDay.length,
      sessionColors: sessionsOnDay.map(s => s.color || '#4299E1'),
      sessionTitles: sessionsOnDay.map(s => s.title.split('(')[0])
    };
  }

  /**
   * Checks if the current displayed year is the actual current year
   */
  isActualCurrentYear(): boolean {
    return this.currentYear === new Date().getFullYear();
  }
}