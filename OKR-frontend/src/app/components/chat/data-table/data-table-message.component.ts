import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { TableConfig, OkrSession, Objective, KeyResult, KeyResultTask } from './table.models';
import { TableDataService } from './table-data.service';

@Component({
  selector: 'app-data-table-message',
  templateUrl: './data-table-message.component.html',
  styleUrls: ['./data-table-message.component.scss']
})
export class DataTableMessageComponent implements OnInit, OnChanges {
  @Input() response: any;
  
  dataType: string | null = null;
  tableData: any[] = [];
  tableConfig: TableConfig = { columns: [] };
  
  // Pagination state
  currentPage: number = 1;
  pageSize: number = 5;
  totalItems: number = 0;
  
  // Flag for server pagination
  useServerPagination: boolean = false;
  
  constructor(private tableDataService: TableDataService) { }

  ngOnInit(): void {
    this.processResponse();
  }
  
  ngOnChanges(): void {
    this.processResponse();
  }
  
  private processResponse(): void {
    if (!this.response) {
      console.log('No response data provided to data-table-message component');
      return;
    }
    
    console.log('Data-table-message processing response:', this.response);
    
    // Detect data type in response
    this.dataType = this.tableDataService.detectDataType(this.response);
    console.log(`Detected data type: ${this.dataType || 'none'}`);
    
    if (this.dataType === 'sessions') {
      this.tableData = this.tableDataService.extractOkrSessions(this.response);
      // Normalize data to ensure it matches the expected properties
      this.tableData = this.normalizeData(this.tableData, 'sessions');
      this.tableConfig = this.tableDataService.getOkrSessionTableConfig();
      this.totalItems = this.tableDataService.extractTotalCount(this.response);
      console.log(`Extracted ${this.tableData.length} OKR sessions from response`);
    } 
    else if (this.dataType === 'objectives') {
      this.tableData = this.tableDataService.extractObjectives(this.response);
      this.tableData = this.normalizeData(this.tableData, 'objectives');
      this.tableConfig = this.tableDataService.getObjectivesTableConfig();
      this.totalItems = this.tableDataService.extractTotalCount(this.response);
      console.log(`Extracted ${this.tableData.length} objectives from response`);
    }
    else if (this.dataType === 'keyresults') {
      this.tableData = this.tableDataService.extractKeyResults(this.response);
      this.tableData = this.normalizeData(this.tableData, 'keyresults');
      this.tableConfig = this.tableDataService.getKeyResultsTableConfig();
      this.totalItems = this.tableDataService.extractTotalCount(this.response);
      console.log(`Extracted ${this.tableData.length} key results from response`);
    }
    else if (this.dataType === 'keyresulttasks') {
      this.tableData = this.tableDataService.extractKeyResultTasks(this.response);
      this.tableData = this.normalizeData(this.tableData, 'keyresulttasks');
      this.tableConfig = this.tableDataService.getKeyResultTasksTableConfig();
      this.totalItems = this.tableDataService.extractTotalCount(this.response);
      console.log(`Extracted ${this.tableData.length} key result tasks from response`);
    }
    else if (this.dataType === 'teams') {
      this.tableData = this.tableDataService.extractTeams(this.response);
      // Normalize team data to ensure it matches the expected properties
      this.tableData = this.normalizeData(this.tableData, 'teams');
      this.tableConfig = this.tableDataService.getTeamsTableConfig();
      this.totalItems = this.tableDataService.extractTotalCount(this.response);
      console.log(`Extracted ${this.tableData.length} teams from response`);
      
      // Log team data to help debug
      if (this.tableData && this.tableData.length > 0) {
        console.log('First team in the list (after normalization):', this.tableData[0]);
      } else {
        console.log('No teams were extracted from response. Raw response data:', JSON.stringify(this.response));
        
        // Try to debug further - check for possible team data structures
        if (this.response.Teams) {
          console.log('Teams data found as response.Teams:', this.response.Teams);
        }
        if (this.response.teams) {
          console.log('Teams data found as response.teams:', this.response.teams);
        }
        if (this.response.tableData && this.response.tableData.Teams) {
          console.log('Teams data found as response.tableData.Teams:', this.response.tableData.Teams);
        }
        if (this.response.tableData && this.response.tableData.teams) {
          console.log('Teams data found as response.tableData.teams:', this.response.tableData.teams);
        }
      }
    }
    else {
      console.warn('No recognized data type in response. Raw response:', this.response);
      // If we don't recognize the data type, log more details to help diagnose
      console.log('Response properties:', Object.keys(this.response));
      
      // Check common properties that might indicate type
      if (this.response.intents) {
        console.log('Intents found:', this.response.intents);
      }
      if (this.response.functionName) {
        console.log('Function name found:', this.response.functionName);
      }
      if (this.response.functionOutput) {
        console.log('Function output found, type:', typeof this.response.functionOutput);
        try {
          const outputData = typeof this.response.functionOutput === 'string'
            ? JSON.parse(this.response.functionOutput)
            : this.response.functionOutput;
          console.log('Parsed function output:', outputData);
        } catch (error) {
          console.error('Error parsing function output:', error);
        }
      }
    }
  }
  
  /**
   * Generic data normalization to ensure it has the properties expected by the respective table configs
   */
  private normalizeData(data: any[], dataType: string): any[] {
    if (!data || !Array.isArray(data)) {
      console.log(`No valid ${dataType} array provided for normalization`);
      return [];
    }
    
    if (data.length === 0) {
      return [];
    }
    
    console.log(`Normalizing ${dataType} data, first item before:`, data[0]);
    
    return data.map(item => {
      // Create a new object to hold the normalized data
      const normalizedItem = { ...item };
      
      // Common properties that might have capitalization issues
      const propertyMappings: { [key: string]: string } = {
        'title': 'Title',
        'description': 'Description',
        'status': 'Status',
        'progress': 'Progress',
        'startDate': 'StartDate',
        'endDate': 'EndDate',
        'createdAt': 'CreatedAt',
        'name': 'Name',
        'teamId': 'TeamId',
        'okrSessionId': 'OkrSessionId',
        'objectiveId': 'ObjectiveId',
        'keyResultId': 'KeyResultId'
      };
      
      // Copy properties with correct casing
      Object.entries(propertyMappings).forEach(([lowerCase, upperCase]) => {
        if (normalizedItem[upperCase] !== undefined && normalizedItem[lowerCase] === undefined) {
          normalizedItem[lowerCase] = normalizedItem[upperCase];
        }
      });
      
      // Type-specific normalization
      if (dataType === 'teams') {
        // Ensure members array exists
        if (!normalizedItem.members) {
          if (normalizedItem.Members) {
            normalizedItem.members = normalizedItem.Members;
          } else {
            normalizedItem.members = [];
          }
        }
      }
      
      // Ensure progress is a number
      if (normalizedItem.progress === undefined || normalizedItem.progress === null) {
        normalizedItem.progress = 0;
      }
      
      console.log(`Normalized ${dataType} item:`, normalizedItem);
      return normalizedItem;
    });
  }
  
  /**
   * @deprecated Use normalizeData instead
   */
  private normalizeSessionData(sessions: any[]): any[] {
    return this.normalizeData(sessions, 'sessions');
  }
  
  /**
   * @deprecated Use normalizeData instead
   */
  private normalizeTeamData(teams: any[]): any[] {
    return this.normalizeData(teams, 'teams');
  }
  
  onRowClick(row: any): void {
    console.log('Row clicked:', row);
    // Handle row click - might open a detail view or perform an action
  }
  
  onServerPageChange(event: {page: number, pageSize: number}): void {
    if (!this.useServerPagination) return;
    
    this.currentPage = event.page;
    this.pageSize = event.pageSize;
    
    // Only activate server pagination for subsequent page changes if API is available
    if (this.dataType === 'sessions') {
      this.tableDataService.fetchOkrSessions(event.page, event.pageSize)
        .subscribe({
          next: (response) => {
            this.tableData = this.tableDataService.extractOkrSessions(response);
            this.totalItems = this.tableDataService.extractTotalCount(response);
          },
          error: (error) => {
            console.error('Error fetching paginated data:', error);
          }
        });
    }
    else if (this.dataType === 'objectives') {
      this.tableDataService.fetchObjectives(event.page, event.pageSize)
        .subscribe({
          next: (response) => {
            this.tableData = this.tableDataService.extractObjectives(response);
            this.totalItems = this.tableDataService.extractTotalCount(response);
          },
          error: (error) => {
            console.error('Error fetching objectives data:', error);
          }
        });
    }
    else if (this.dataType === 'keyresults') {
      this.tableDataService.fetchKeyResults(event.page, event.pageSize)
        .subscribe({
          next: (response) => {
            this.tableData = this.tableDataService.extractKeyResults(response);
            this.totalItems = this.tableDataService.extractTotalCount(response);
          },
          error: (error) => {
            console.error('Error fetching key results data:', error);
          }
        });
    }
    else if (this.dataType === 'keyresulttasks') {
      this.tableDataService.fetchKeyResultTasks(event.page, event.pageSize)
        .subscribe({
          next: (response) => {
            this.tableData = this.tableDataService.extractKeyResultTasks(response);
            this.totalItems = this.tableDataService.extractTotalCount(response);
          },
          error: (error) => {
            console.error('Error fetching key result tasks data:', error);
          }
        });
    }
    else if (this.dataType === 'teams') {
      this.tableDataService.fetchTeams(event.page, event.pageSize)
        .subscribe({
          next: (response) => {
            this.tableData = this.tableDataService.extractTeams(response);
            this.totalItems = this.tableDataService.extractTotalCount(response);
          },
          error: (error) => {
            console.error('Error fetching teams data:', error);
          }
        });
    }
  }
} 