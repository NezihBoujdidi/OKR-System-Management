import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, throwError } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { CreateOkrCommand } from '../models/okr-session.interface';
import { Team } from '../models/team.interface';
import { AuthStateService } from './auth-state.service';

export interface OkrSuggestion {
  title: string;
  description: string;
  suggestedTeams?: string[];
  suggestedStartDate?: Date;
  suggestedEndDate?: Date;
  industryInsights?: string[];
  alignmentTips?: string[];
  keyFocusAreas?: string[];
  potentialKeyResults?: string[];
}

export interface IndustryInsight {
  industry: string;
  trends: string[];
  bestPractices: string[];
  challenges: string[];
}

export interface OkrTemplate {
  name: string;
  description: string;
  category: string;
  sampleObjectives: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AiAssistantService {
  private apiUrl = `${environment.apiUrl}/api/ai/okr-suggestions`;

  constructor(private http: HttpClient,
              private authStateService: AuthStateService, // Assuming you have an AuthStateService to get user context
  ) {}

  /**
   * Fetch dynamic OKR session suggestions from the backend AI service
   * @param prompt User prompt for OKR generation
   * @param teams Available teams to reference in suggestions
   * @returns Observable with OKR suggestions
   */
  getOkrSessionSuggestions(prompt: string, teams: Team[]): Observable<OkrSuggestion> {
    const requestData = {
      prompt,
      availableTeams: teams.map(team => ({ id: team.id, name: team.name, description: team.description })),
      context: 'okr_session_creation'
    };
    return this.http.post<OkrSuggestion>(this.apiUrl, requestData).pipe(
      catchError(error => {
        console.error('Error fetching OKR session suggestions:', error);
        return throwError(() => new Error('Failed to fetch OKR session suggestions. Please try again.'));
      })
    );
  }

  /**
   * Get suggested quarterly timeframe based on current date
   * @returns Object with suggested start and end dates for a quarterly OKR session
   */
  getSuggestedTimeframe(): { startDate: Date, endDate: Date } {
    const now = new Date();
    const currentMonth = now.getMonth();
    
    // Calculate start of current or next quarter
    let startMonth: number;
    if (currentMonth < 3) startMonth = 0; // Q1: Jan-Mar
    else if (currentMonth < 6) startMonth = 3; // Q2: Apr-Jun
    else if (currentMonth < 9) startMonth = 6; // Q3: Jul-Sep
    else startMonth = 9; // Q4: Oct-Dec
    
    // If we're in the latter half of the quarter, suggest the next quarter
    const dayOfMonth = now.getDate();
    const isLatterHalfOfQuarter = dayOfMonth > 15;
    
    if (isLatterHalfOfQuarter) {
      startMonth = (startMonth + 3) % 12;
    }
    
    // Create start date (1st day of the quarter)
    const startYear = startMonth === 0 && currentMonth >= 9 ? now.getFullYear() + 1 : now.getFullYear();
    const startDate = new Date(startYear, startMonth, 1);
    
    // Create end date (last day of the quarter)
    const endMonth = (startMonth + 2) % 12;
    const endYear = startMonth <= 9 && endMonth >= 0 && endMonth < 3 ? startYear + 1 : startYear;
    const endDate = new Date(endYear, endMonth + 1, 0); // Last day of end month
    
    return { startDate, endDate };
  }

  /**
   * Generate improvement suggestions for an existing OKR session
   * @param sessionTitle The title of the OKR session
   * @param sessionDescription The description of the session
   * @returns Observable with improvement suggestions
   */
  getOkrImprovementSuggestions(sessionTitle: string, sessionDescription: string): Observable<string[]> {
    // In a real implementation, this would call the backend AI service
    // For now, returning mock suggestions
    
    // Sample improvement suggestions based on common OKR best practices
    const suggestions = [
      'Consider using more specific, measurable targets in your session description',
      'Try to focus on 3-5 key objectives for better team focus and clarity',
      'Add timebound elements to make progress tracking clearer',
      'Consider adding objectives related to customer satisfaction and retention',
      'Balance your OKRs between growth initiatives and operational excellence',
      'Include innovation-focused objectives to drive future growth',
      'Ensure alignment between team and organizational objectives',
      'Add metrics for measuring the success of each objective',
      'Consider cross-functional objectives that span multiple teams',
      'Include employee development and culture-related objectives'
    ];
    
    // Return 2-3 contextually relevant suggestions based on session title/description
    const sessionText = (sessionTitle + ' ' + sessionDescription).toLowerCase();
    const relevantSuggestions = suggestions
      .filter(suggestion => {
        const words = suggestion.toLowerCase().split(' ');
        // Check if any important words from the suggestion appear in the session text
        return words.some(word => word.length > 5 && sessionText.includes(word));
      })
      .slice(0, 3);
    
    // If no relevant suggestions found, return random ones
    const selectedSuggestions = relevantSuggestions.length > 0
      ? relevantSuggestions
      : suggestions
          .sort(() => 0.5 - Math.random())
          .slice(0, 3);
    
    // Return as observable with delay to simulate API call
    return new Observable(observer => {
      setTimeout(() => {
        observer.next(selectedSuggestions);
        observer.complete();
      }, 800);
    });
  }

  /**
   * Get AI-generated insights for a session based on user instructions
   */
  getSessionInsights(sessionId: string, instructions: string): Observable<string[]> {
    const url = `${environment.apiUrl}/api/ai/session-insights`;
    const currentUser = this.authStateService.getCurrentUser();
    const userContext = {
      userId: currentUser?.id || '',
      userName: currentUser ? `${currentUser.firstName} ${currentUser.lastName}` : '',
      email: currentUser?.email || '',
      organizationId: currentUser?.organizationId || '',
      role: currentUser?.role || '',
      selectedLLMProvider: "azureopenai"
    };
    const body = {
      sessionId,
      instructions,
      userContext
    };
    return this.http.post<{ insights: string[] }>(url, body).pipe(
      map((res: any) => res.insights),
      catchError(error => {
        console.error('Error fetching AI session insights:', error);
        return throwError(() => new Error('Failed to generate AI report. Please try again.'));
      })
    );
  }
}