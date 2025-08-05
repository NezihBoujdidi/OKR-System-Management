import { Injectable } from '@angular/core';
import { TableConfig, OkrSession, Objective, KeyResult, KeyResultTask, Team } from './table.models';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TableDataService {
  private apiUrl = 'http://localhost:5001/api/ai'; // Adjust as needed
  
  constructor(private http: HttpClient) { }

  /**
   * Format date string to readable format
   */
  private formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  /**
   * Format status with proper styling (will be used for CSS classes)
   */
  private formatStatus(status: string): string {
    return status;
  }

  /**
   * Format progress as percentage
   */
  private formatProgress(progress: number): string {
    return `${progress}%`;
  }

  /**
   * Get table configuration for OKR sessions
   */
  getOkrSessionTableConfig(): TableConfig {
    return {
      columns: [
        { 
          key: 'title', 
          header: 'Session Name', 
          type: 'text', 
          sortable: true,
          width: '25%'
        },
        { 
          key: 'description', 
          header: 'Description', 
          type: 'text', 
          sortable: true,
          width: '25%'
        },
        { 
          key: 'startDate', 
          header: 'Start Date', 
          type: 'date', 
          sortable: true,
          width: '12%',
          format: (value) => this.formatDate(value)
        },
        { 
          key: 'endDate', 
          header: 'End Date', 
          type: 'date', 
          sortable: true,
          width: '12%',
          format: (value) => this.formatDate(value)
        },
        { 
          key: 'status', 
          header: 'Status', 
          type: 'text', 
          sortable: true,
          width: '13%',
          format: (value) => this.formatStatus(value)
        },
        { 
          key: 'progress', 
          header: 'Progress', 
          type: 'number', 
          sortable: true,
          width: '13%',
          format: (value) => value !== undefined && value !== null ? this.formatProgress(value) : 'N/A'
        }
      ],
      defaultSort: { column: 'startDate', direction: 'desc' },
      searchFields: ['title', 'description', 'status']
    };
  }

  /**
   * Get table configuration for objectives
   */
  getObjectivesTableConfig(): TableConfig {
    return {
      columns: [
        { 
          key: 'title', 
          header: 'Objective', 
          type: 'text', 
          sortable: true,
          width: '20%'
        },
        { 
          key: 'description', 
          header: 'Description', 
          type: 'text', 
          sortable: true,
          width: '20%'
        },
        { 
          key: 'okrSessionTitle', 
          header: 'OKR Session', 
          type: 'text', 
          sortable: true,
          width: '15%'
        },
        { 
          key: 'responsibleTeamName', 
          header: 'Team', 
          type: 'text', 
          sortable: true,
          width: '10%'
        },
        { 
          key: 'status', 
          header: 'Status', 
          type: 'text', 
          sortable: true,
          width: '10%',
          format: (value) => this.formatStatus(value)
        },
        { 
          key: 'priority', 
          header: 'Priority', 
          type: 'text', 
          sortable: true,
          width: '10%'
        },
        { 
          key: 'progress', 
          header: 'Progress', 
          type: 'number', 
          sortable: true,
          width: '15%',
          format: (value) => this.formatProgress(value)
        }
      ],
      defaultSort: { column: 'title', direction: 'asc' },
      searchFields: ['title', 'description', 'okrSessionTitle', 'responsibleTeamName']
    };
  }

  /**
   * Get table configuration for key results
   */
  getKeyResultsTableConfig(): TableConfig {
    return {
      columns: [
        { 
          key: 'title', 
          header: 'Key Result', 
          type: 'text', 
          sortable: true,
          width: '25%'
        },
        { 
          key: 'objectiveTitle', 
          header: 'Objective', 
          type: 'text', 
          sortable: true,
          width: '20%'
        },
        { 
          key: 'startedDate', 
          header: 'Start Date', 
          type: 'date', 
          sortable: true,
          width: '12%',
          format: (value) => this.formatDate(value)
        },
        { 
          key: 'endDate', 
          header: 'End Date', 
          type: 'date', 
          sortable: true,
          width: '12%',
          format: (value) => this.formatDate(value)
        },
        { 
          key: 'userName', 
          header: 'Owner', 
          type: 'text', 
          sortable: true,
          width: '13%'
        },
        { 
          key: 'status', 
          header: 'Status', 
          type: 'text', 
          sortable: true,
          width: '8%',
          format: (value) => this.formatStatus(value)
        },
        { 
          key: 'progress', 
          header: 'Progress', 
          type: 'number', 
          sortable: true,
          width: '10%',
          format: (value) => this.formatProgress(value)
        }
      ],
      defaultSort: { column: 'title', direction: 'asc' },
      searchFields: ['title', 'description', 'objectiveTitle', 'userName']
    };
  }

  /**
   * Get table configuration for key result tasks
   */
  getKeyResultTasksTableConfig(): TableConfig {
    return {
      columns: [
        { 
          key: 'title', 
          header: 'Task', 
          type: 'text', 
          sortable: true,
          width: '30%'
        },
        { 
          key: 'keyResultTitle', 
          header: 'Key Result', 
          type: 'text', 
          sortable: true,
          width: '25%'
        },
        { 
          key: 'collaboratorName', 
          header: 'Assignee', 
          type: 'text', 
          sortable: true,
          width: '15%'
        },
        { 
          key: 'endDate', 
          header: 'Due Date', 
          type: 'date', 
          sortable: true,
          width: '12%',
          format: (value) => this.formatDate(value)
        },
        { 
          key: 'priority', 
          header: 'Priority', 
          type: 'text', 
          sortable: true,
          width: '8%'
        },
        { 
          key: 'progress', 
          header: 'Progress', 
          type: 'number', 
          sortable: true,
          width: '10%',
          format: (value) => this.formatProgress(value)
        }
      ],
      defaultSort: { column: 'endDate', direction: 'asc' },
      searchFields: ['title', 'description', 'keyResultTitle', 'collaboratorName']
    };
  }

  /**
   * Table configuration for teams
   */
  getTeamsTableConfig(): TableConfig {
    return {
      columns: [
        {
          key: 'name',
          header: 'Team Name',
          type: 'text',
          sortable: true,
          width: '30%'
        },
        {
          key: 'description',
          header: 'Description',
          type: 'text',
          sortable: true,
          width: '40%'
        },
        {
          key: 'createdAt',
          header: 'Created',
          type: 'date',
          sortable: true,
          width: '15%',
          format: (value) => this.formatDate(value)
        },
        {
          key: 'members.length',
          header: 'Members',
          type: 'number',
          sortable: true,
          width: '15%'
        }
      ],
      defaultSort: { column: 'name', direction: 'asc' },
      searchFields: ['name', 'description']
    };
  }

  /**
   * Extract OKR sessions from the API response
   */
  extractOkrSessions(response: any): OkrSession[] {
    console.log('Extracting OKR sessions from response:', response);
    
    // Check for the new format where sessions are in functionResults.Sessions
    if (response?.functionResults?.Sessions && Array.isArray(response.functionResults.Sessions)) {
      console.log('Found sessions in functionResults.Sessions:', response.functionResults.Sessions);
      return response.functionResults.Sessions;
    }
    
    // Direct access (for history data)
    if (response?.Sessions && Array.isArray(response.Sessions)) {
      return response.Sessions;
    }
    
    if (response?.sessions && Array.isArray(response.sessions)) {
      return response.sessions;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const sessionsData = response.functionResults.find((result: any) => result.sessions);
      if (sessionsData && Array.isArray(sessionsData.sessions)) {
        return sessionsData.sessions;
      }
    }
    
    return [];
  }

  /**
   * Extract objectives from the API response
   */
  extractObjectives(response: any): Objective[] {
    console.log('Extracting objectives from response:', response);
    
    // Check for the new format where objectives are in functionResults.Objectives
    if (response?.functionResults?.Objectives && Array.isArray(response.functionResults.Objectives)) {
      console.log('Found objectives in functionResults.Objectives:', response.functionResults.Objectives);
      return response.functionResults.Objectives;
    }
    
    // Direct access (for history data)
    if (response?.Objectives && Array.isArray(response.Objectives)) {
      return response.Objectives;
    }
    
    if (response?.objectives && Array.isArray(response.objectives)) {
      return response.objectives;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const objectivesData = response.functionResults.find((result: any) => result.objectives);
      if (objectivesData && Array.isArray(objectivesData.objectives)) {
        return objectivesData.objectives;
      }
    }
    
    return [];
  }

  /**
   * Extract key results from the API response
   */
  extractKeyResults(response: any): KeyResult[] {
    console.log('Extracting key results from response:', response);
    
    // Check for the new format where key results are in functionResults.KeyResults
    if (response?.functionResults?.KeyResults && Array.isArray(response.functionResults.KeyResults)) {
      console.log('Found key results in functionResults.KeyResults:', response.functionResults.KeyResults);
      return response.functionResults.KeyResults;
    }
    
    // Direct access (for history data)
    if (response?.KeyResults && Array.isArray(response.KeyResults)) {
      return response.KeyResults;
    }
    
    if (response?.keyResults && Array.isArray(response.keyResults)) {
      return response.keyResults;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const keyResultsData = response.functionResults.find((result: any) => result.keyResults);
      if (keyResultsData && Array.isArray(keyResultsData.keyResults)) {
        return keyResultsData.keyResults;
      }
    }
    
    return [];
  }

  /**
   * Extract key result tasks from the API response
   */
  extractKeyResultTasks(response: any): KeyResultTask[] {
    console.log('Extracting key result tasks from response:', response);
    
    // Check for the new format where tasks are in functionResults.Tasks
    if (response?.functionResults?.Tasks && Array.isArray(response.functionResults.Tasks)) {
      console.log('Found tasks in functionResults.Tasks:', response.functionResults.Tasks);
      return response.functionResults.Tasks;
    }
    
    // Direct access (for history data)
    if (response?.Tasks && Array.isArray(response.Tasks)) {
      return response.Tasks;
    }
    
    if (response?.keyResultTasks && Array.isArray(response.keyResultTasks)) {
      return response.keyResultTasks;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const tasksData = response.functionResults.find((result: any) => result.keyResultTasks);
      if (tasksData && Array.isArray(tasksData.keyResultTasks)) {
        return tasksData.keyResultTasks;
      }
    }
    
    return [];
  }

  /**
   * Extract teams from the API response
   */
  extractTeams(response: any): Team[] {
    console.log('Extracting teams from response:', response);
    
    // Check for the new format where teams are in functionResults.Teams
    if (response?.functionResults?.Teams && Array.isArray(response.functionResults.Teams)) {
      console.log('Found teams in functionResults.Teams:', response.functionResults.Teams);
      return response.functionResults.Teams;
    }
    
    // Direct access (for history data)
    if (response?.Teams && Array.isArray(response.Teams)) {
      return response.Teams;
    }
    
    if (response?.teams && Array.isArray(response.teams)) {
      return response.teams;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const teamsData = response.functionResults.find((result: any) => result.teams);
      if (teamsData && Array.isArray(teamsData.teams)) {
        return teamsData.teams;
      }
    }
    
    return [];
  }

  /**
   * Get total count of items from the API response
   */
  extractTotalCount(response: any): number {
    console.log('Extracting total count from response:', response);
    
    // Check for the new format where count is in functionResults
    if (response?.functionResults) {
      if (response.functionResults.Count !== undefined) {
        return response.functionResults.Count;
      }
      
      // If Count is not available, try to count the arrays
      if (response.functionResults.Sessions && Array.isArray(response.functionResults.Sessions)) {
        return response.functionResults.Sessions.length;
      }
      if (response.functionResults.Objectives && Array.isArray(response.functionResults.Objectives)) {
        return response.functionResults.Objectives.length;
      }
      if (response.functionResults.KeyResults && Array.isArray(response.functionResults.KeyResults)) {
        return response.functionResults.KeyResults.length;
      }
      if (response.functionResults.Tasks && Array.isArray(response.functionResults.Tasks)) {
        return response.functionResults.Tasks.length;
      }
      if (response.functionResults.Teams && Array.isArray(response.functionResults.Teams)) {
        return response.functionResults.Teams.length;
      }
    }
    
    // Direct property access (for history data)
    
    // Teams
    if (response?.Teams && Array.isArray(response.Teams)) {
      return response.Teams.length;
    }
    if (response?.teams && Array.isArray(response.teams)) {
      return response.teams.length;
    }
    if (response?.teamsCount || response?.Count || response?.count) {
      return response.teamsCount || response.Count || response.count;
    }
    
    // Sessions
    if (response?.Sessions && Array.isArray(response.Sessions)) {
      return response.Sessions.length;
    }
    if (response?.sessions && Array.isArray(response.sessions)) {
      return response.sessions.length;
    }
    if (response?.sessionsCount) {
      return response.sessionsCount;
    }
    
    // Objectives
    if (response?.Objectives && Array.isArray(response.Objectives)) {
      return response.Objectives.length;
    }
    if (response?.objectives && Array.isArray(response.objectives)) {
      return response.objectives.length;
    }
    if (response?.objectivesCount) {
      return response.objectivesCount;
    }
    
    // Key Results
    if (response?.KeyResults && Array.isArray(response.KeyResults)) {
      return response.KeyResults.length;
    }
    if (response?.keyResults && Array.isArray(response.keyResults)) {
      return response.keyResults.length;
    }
    if (response?.keyResultsCount) {
      return response.keyResultsCount;
    }
    
    // Tasks
    if (response?.Tasks && Array.isArray(response.Tasks)) {
      return response.Tasks.length;
    }
    if (response?.keyResultTasks && Array.isArray(response.keyResultTasks)) {
      return response.keyResultTasks.length;
    }
    if (response?.keyResultTasksCount) {
      return response.keyResultTasksCount;
    }
    
    // Legacy function results format (for direct API calls)
    if (response?.functionResults && Array.isArray(response.functionResults)) {
      const sessionData = response.functionResults.find((result: any) => result.sessions);
      if (sessionData && sessionData.totalCount !== undefined) {
        return sessionData.totalCount;
      } else if (sessionData && Array.isArray(sessionData.sessions)) {
        return sessionData.sessions.length;
      }

      // Check for objectives
      const objectivesData = response.functionResults.find((result: any) => result.objectives);
      if (objectivesData && objectivesData.totalCount !== undefined) {
        return objectivesData.totalCount;
      } else if (objectivesData && Array.isArray(objectivesData.objectives)) {
        return objectivesData.objectives.length;
      }

      // Check for key results
      const keyResultsData = response.functionResults.find((result: any) => result.keyResults);
      if (keyResultsData && keyResultsData.count !== undefined) {
        return keyResultsData.count;
      } else if (keyResultsData && Array.isArray(keyResultsData.keyResults)) {
        return keyResultsData.keyResults.length;
      }

      // Check for key result tasks
      const keyResultTasksData = response.functionResults.find((result: any) => result.keyResultTasks);
      if (keyResultTasksData && keyResultTasksData.count !== undefined) {
        return keyResultTasksData.count;
      } else if (keyResultTasksData && Array.isArray(keyResultTasksData.keyResultTasks)) {
        return keyResultTasksData.keyResultTasks.length;
      }
      
      // Check for teams
      const teamsData = response.functionResults.find((result: any) => result.teams);
      if (teamsData && teamsData.count !== undefined) {
        return teamsData.count;
      } else if (teamsData && Array.isArray(teamsData.teams)) {
        return teamsData.teams.length;
      }
    }
    
    return 0;
  }

  /**
   * Fetch OKR sessions with pagination
   */
  fetchOkrSessions(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<any> {
    const url = `${this.apiUrl}/sessions`;
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };

    if (searchTerm) {
      Object.assign(params, { title: searchTerm });
    }

    return this.http.get(url, { params });
  }

  /**
   * Fetch objectives with pagination
   */
  fetchObjectives(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<any> {
    const url = `${this.apiUrl}/objectives`;
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };

    if (searchTerm) {
      Object.assign(params, { title: searchTerm });
    }

    return this.http.get(url, { params });
  }

  /**
   * Fetch key results with pagination
   */
  fetchKeyResults(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<any> {
    const url = `${this.apiUrl}/keyresults`;
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };

    if (searchTerm) {
      Object.assign(params, { title: searchTerm });
    }

    return this.http.get(url, { params });
  }

  /**
   * Fetch key result tasks with pagination
   */
  fetchKeyResultTasks(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<any> {
    const url = `${this.apiUrl}/keyresulttasks`;
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };

    if (searchTerm) {
      Object.assign(params, { title: searchTerm });
    }

    return this.http.get(url, { params });
  }

  /**
   * Fetch teams with pagination support
   */
  fetchTeams(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<any> {
    const url = `${this.apiUrl}/teams`;
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };
    
    if (searchTerm) {
      Object.assign(params, { searchTerm });
    }
    
    return this.http.get(url, { params });
  }

  /**
   * Detect the type of data in the response
   */
  detectDataType(response: any): string | null {
    console.log('Detecting data type from response:', response);
    
    // Check for the new format where data is in functionResults
    if (response?.functionResults) {
      if (response.functionResults.Sessions) {
        return 'sessions';
      }
      if (response.functionResults.Objectives) {
        return 'objectives';
      }
      if (response.functionResults.KeyResults) {
        return 'keyresults';
      }
      if (response.functionResults.Tasks) {
        return 'keyresulttasks';
      }
      if (response.functionResults.Teams) {
        return 'teams';
      }
    }
    
    // Direct property detection (useful for data from conversation history)
    if (response) {
      // Check for direct table data properties
      if (response.sessions || response.Sessions) {
        return 'sessions';
      }
      if (response.objectives || response.Objectives) {
        return 'objectives';
      }
      if (response.keyResults || response.KeyResults) {
        return 'keyresults';
      }
      if (response.keyResultTasks || response.Tasks) {
        return 'keyresulttasks';
      }
      if (response.teams || response.Teams) {
        return 'teams';
      }
    }
    
    // Intent-based detection (useful for direct API responses)
    if (response?.intents && Array.isArray(response.intents)) {
      const intents = response.intents.map((i: string) => i.toLowerCase());
      if (intents.some((i: string) => i.includes('session'))) {
        return 'sessions';
      }
      if (intents.some((i: string) => i.includes('objective'))) {
        return 'objectives';
      }
      if (intents.some((i: string) => i.includes('keyresult') && !i.includes('task'))) {
        return 'keyresults';
      }
      if (intents.some((i: string) => i.includes('task'))) {
        return 'keyresulttasks';
      }
      if (intents.some((i: string) => i.includes('team'))) {
        return 'teams';
      }
    }
    
    return null;
  }
} 