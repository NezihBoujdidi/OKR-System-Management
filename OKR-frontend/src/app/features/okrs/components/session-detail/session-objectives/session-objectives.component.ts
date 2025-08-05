import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { OKRSession } from '../../../../../models/okr-session.interface';
import { Objective } from '../../../../../models/objective.interface';
import { KeyResult } from '../../../../../models/key-result.interface';
import { KeyResultTask } from '../../../../../models/key-result-task.interface';
import { ObjectiveService } from '../../../../../services/objective.service';
import { KeyResultService } from '../../../../../services/key-result.service';
import { KeyResultTaskService } from '../../../../../services/key-result-task.service';
import { OKRSessionService } from '../../../../../services/okr-session.service';
import { AuthStateService } from '../../../../../services/auth-state.service';
import { UserService } from '../../../../../services/user.service';
import { forkJoin, map, tap, switchMap, of } from 'rxjs';
import { PdfExportService } from '../../../../../services/pdf-export.service';
import { Status } from '../../../../../models/Status.enum';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { TeamService } from 'src/app/services/team.service';
import { Team } from '../../../../../models/team.interface';
import { RoleType } from '../../../../../models/role-type.enum';
import { User } from '../../../../../models/user.interface';



interface Statistics {
  total: {
    value: number;
    trend: string;
    trendDirection: 'up' | 'down';
  };
  onTrack: {
    value: number;
    percentage: number;
    status: string;
  };
  atRisk: {
    value: number;
    percentage: number;
    status: string;
  };
  completed: {
    value: number;
    percentage: number;
    status: string;
  };
}

// Add this utility function at the top level before the @Component decorator
function toUtcDate(date: Date | string | undefined): Date | undefined {
  if (!date) return undefined;
  
  const convertedDate = typeof date === 'string' ? new Date(date) : date;
  return new Date(convertedDate.getTime() - convertedDate.getTimezoneOffset() * 60000);
}

@Component({
  selector: 'app-session-objectives',
  templateUrl: './session-objectives.component.html'
})
export class SessionObjectivesComponent implements OnInit, OnChanges {
  @Input() okrSession!: OKRSession;
  
  objectives: Objective[] = [];
  teams: Team[] = [];
  objectiveOwners = new Map<string, any>();
  objectiveManagers = new Map<string, User | undefined>();
  objectiveKeyResults = new Map<string, KeyResult[]>();
  objectiveKeyResultTasks = new Map<string, KeyResultTask[]>();
  statistics: Statistics = {
    total: { value: 0, trend: '+0% from last period', trendDirection: 'up' },
    onTrack: { value: 0, percentage: 0, status: 'Good' },
    atRisk: { value: 0, percentage: 0, status: 'Alert' },
    completed: { value: 0, percentage: 0, status: 'Done' }
  };

  showNewObjectiveDrawer = false;
  showNewKeyResultModal = false;
  showNewTaskModal = false;
  isNotificationVisible = false;
  
  selectedObjective: Objective | null = null;
  selectedKeyResult: KeyResult | null = null;
  selectedTask: KeyResultTask | null = null;
  
  notificationMessage = '';
  notificationType: 'success' | 'error' = 'success';
  notificationTimeout: any = null;

  // Expose Status enum to the template
  Status = Status;

  // Add this variable to track the loading state
  isSubmitting = false;

  // Add this variable to store the selected objective ID
  selectedObjectiveId: string = '';

  // Add this variable to track the loading state
  loading = false;

  // Add properties for element highlighting
  highlightElementId: string | null = null;
  highlightElementType: 'objective' | 'keyResult' | 'task' | null = null;
  parentElementId: string | null = null;
  
  // Add a flag to track if highlighting is from alignment diagram navigation
  private isFromAlignmentNavigation = false;

  // Map to store fetched team managers
  teamManagers: { [teamId: string]: User | undefined } = {};

  // Flag to track if managers are being loading
  loadingManagers = false;

  // Add availableTeams property
  availableTeams: Team[] = [];

  // Add property for PDF export modal
  showPdfExportModal = false;

  // Map to store collaborator information
  collaborators: { [id: string]: User } = {};

  // Add properties for confirmation dialogs
  showDeleteConfirmation = false;
  deleteConfirmationTitle = '';
  deleteConfirmationMessage = '';
  deleteConfirmationType: 'objective' | 'keyResult' | 'task' = 'objective';
  itemToDelete: any = null;
  objectiveIdForDelete: string = '';
  keyResultIdForDelete: string = '';
  isDeleting = false;
  
  // Minimum loading time in milliseconds (1 second)
  private readonly minLoadingTime = 1000;

  constructor(
    private objectiveService: ObjectiveService,
    private keyResultService: KeyResultService,
    private keyResultTaskService: KeyResultTaskService,
    private okrSessionService: OKRSessionService,
    private teamService: TeamService,
    private userService: UserService,
    public authStateService: AuthStateService,
    private pdfExportService: PdfExportService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    // Subscribe to route query parameters
    this.route.queryParams.subscribe(params => {
      console.log('Query params received:', params);
      
      // Check for the new highlight parameters
      if (params['highlightTarget'] && params['highlightId']) {
        console.log('Direct highlight params detected:', params);
        
        // Set highlight parameters directly from URL
        this.highlightElementType = params['highlightTarget'] as 'objective' | 'keyResult' | 'task';
        this.highlightElementId = params['highlightId'];
        this.parentElementId = params['parentId'] || null;
        this.isFromAlignmentNavigation = true;
        
        console.log('Setting highlight parameters:', {
          type: this.highlightElementType,
          id: this.highlightElementId,
          parent: this.parentElementId
        });
        
        // Wait for objectives to load before highlighting
        if (this.highlightElementId) {
          this.waitForObjectivesAndHighlight();
        }
      }
      // Check for old alignment navigation parameters as a fallback
      else {
        // Check for source param that indicates navigation from alignment diagram
        const fromAlignment = params['from'] === 'alignment' || localStorage.getItem('fromAlignment') === 'true';
        this.isFromAlignmentNavigation = fromAlignment;
        
        // Only set highlight parameters if navigation is from alignment diagram
        if (fromAlignment) {
          // First try to get from URL params, then fall back to localStorage
          this.highlightElementId = params['highlight'] || localStorage.getItem('highlightElementId');
          
          // Get highlight type from URL or localStorage
          if (params['highlight']) {
            this.highlightElementType = this.determineElementType(params['highlight']);
          } else if (localStorage.getItem('highlightElementType')) {
            this.highlightElementType = localStorage.getItem('highlightElementType') as 'objective' | 'keyResult' | 'task';
          }
          
          this.parentElementId = params['parent'] || localStorage.getItem('parentElementId');
          
          // Wait for objectives to load before highlighting
          if (this.highlightElementId) {
            this.waitForObjectivesAndHighlight();
          }
        } else {
          // If not from alignment, clear all highlight-related state
          this.isFromAlignmentNavigation = false;
          this.highlightElementId = null;
          this.highlightElementType = null;
          this.parentElementId = null;
        }
      }
    });

    if (this.okrSession) {
      this.loadObjectives();
    }
    this.teamService.getTeamsByManagerId(this.authStateService.getCurrentUser()?.id || '').subscribe(teams => {
      this.teams = teams;
    });

    // Preload all team managers for objectives (best practice)
    if (this.okrSession) {
      this.objectiveService.getObjectivesBySessionId(this.okrSession.id)
        .subscribe(objectives => {
          const teamIds = Array.from(new Set(objectives.map(obj => obj.responsibleTeamId)));
          teamIds.forEach(teamId => {
            this.objectiveService.getObjectiveManager(teamId).subscribe(manager => {
              this.teamManagers[teamId] = manager;
            });
          });
        });
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['okrSession'] && !changes['okrSession'].firstChange) {
      this.loadObjectives();
    }
  }

  private refreshOKRSessionProgress() {
    if (!this.okrSession?.id) return;
    this.okrSessionService.getOKRSessionById(this.okrSession.id).subscribe((session) => {
      if (session) {
        this.okrSession.progress = session.progress;
      }
    });
  }

  private loadObjectives() {
    if (this.okrSession) {
      // Set loading state
      this.loading = true;
      
      // Step 1: Get all objectives for the session
      this.objectiveService.getObjectivesBySessionId(this.okrSession.id)
        .subscribe({
          next: (objectives) => {
            this.objectives = objectives;
            
            if (objectives.length === 0) {
              this.loading = false;
              this.updateStatistics();
              this.refreshOKRSessionProgress(); // <-- refresh session progress
              return;
            }
            
            // Step 2: Process objectives in batches
            const BATCH_SIZE = 3; // Adjust based on your backend capacity
            this.processObjectiveBatch(objectives, 0, BATCH_SIZE);
          },
          error: (error) => {
            // Silently handle error but set loading to false
            this.loading = false;
            this.refreshOKRSessionProgress(); // <-- refresh session progress on error
          }
        });
    }
  }

  // Process a batch of objectives in parallel
  private processObjectiveBatch(objectives: Objective[], startIndex: number, batchSize: number) {
    if (startIndex >= objectives.length) {
      // All objectives processed
      this.loading = false;
      this.updateStatistics();
      this.refreshOKRSessionProgress(); // <-- refresh session progress
      return;
    }
    
    // Calculate the end index for this batch
    const endIndex = Math.min(startIndex + batchSize, objectives.length);
    
    // Create observables for all owners in this batch
    const batchObservables = [];
    for (let i = startIndex; i < endIndex; i++) {
      const objective = objectives[i];
      
      // Create an observable for processing this objective and its related data
      const objectiveObservable = this.objectiveService.getObjectiveManager(objective.responsibleTeamId)
        .pipe(
          // After getting the owner, process key results
          tap(manager => {
            this.objectiveManagers.set(objective.responsibleTeamId, manager);
          }),
          // Switch to the key results observable
          switchMap(manager => this.keyResultService.getKeyResultsByObjectiveId(objective.id)),
          // Process the key results
          tap(keyResults => {
            this.objectiveKeyResults.set(objective.id, keyResults);
            
            // Process tasks for this objective's key results (max 5 at a time)
            if (keyResults.length > 0) {
              this.processKeyResultBatch(keyResults, 0, 5);
            }
          })
        );
      
      batchObservables.push(objectiveObservable);
    }
    
    // Process this batch in parallel
    forkJoin(batchObservables)
      .subscribe({
        next: () => {
          // When this batch is done, move to the next batch
          this.processObjectiveBatch(objectives, endIndex, batchSize);
        },
        error: (error) => {
          // Continue with next batch even if there's an error
          this.processObjectiveBatch(objectives, endIndex, batchSize);
        }
      });
  }

  // Process a batch of key results in parallel
  private processKeyResultBatch(keyResults: KeyResult[], startIndex: number, batchSize: number) {
    if (startIndex >= keyResults.length) {
      // All key results in this set processed
      return;
    }
    
    // Calculate the end index for this batch
    const endIndex = Math.min(startIndex + batchSize, keyResults.length);
    
    // Create observables for all tasks in this batch
    const batchObservables = [];
    for (let i = startIndex; i < endIndex; i++) {
      const keyResult = keyResults[i];
      
      const taskObservable = this.keyResultTaskService.getKeyResultTasksByKeyResultId(keyResult.id)
        .pipe(
          tap(tasks => {
            this.objectiveKeyResultTasks.set(keyResult.id, tasks);
            
            // Load collaborator info for each task
            tasks.forEach(task => {
              if (task.collaboratorId) {
                this.getCollaborator(task.collaboratorId).subscribe();
              }
            });
          })
        );
      
      batchObservables.push(taskObservable);
    }
    
    // Process this batch in parallel
    forkJoin(batchObservables)
      .subscribe({
        next: () => {
          // When this batch is done, move to the next batch
          this.processKeyResultBatch(keyResults, endIndex, batchSize);
        },
        error: (error) => {
          // Continue with next batch even if there's an error
          this.processKeyResultBatch(keyResults, endIndex, batchSize);
        }
      });
  }

  getObjectiveOwner(objectiveId: string) {
    return this.objectiveOwners.get(objectiveId);
  }
  getObjectiveManager(responsibleTeamId: string) {
    var result = this.objectiveManagers.get(responsibleTeamId);
    return result;
  }

  getObjectiveKeyResults(objectiveId: string) {
    return this.objectiveKeyResults.get(objectiveId) || [];
  }

  getKeyResultTasks(keyResultId: string): KeyResultTask[] {
    return this.objectiveKeyResultTasks.get(keyResultId) || [];
  }

  private updateStatistics() {
  // No-op: statistics cards removed, only session progress is shown
}

  getStatusColor(status: Status): string {
    switch (status) {
      case Status.Completed:
        return 'bg-blue-100 text-blue-800';
      case Status.InProgress:
        return 'bg-green-100 text-green-800';
      case Status.Overdue:
        return 'bg-yellow-100 text-yellow-800';
      case Status.NotStarted:
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getProgressClass(value: number): string {
    if (value >= 70) return 'bg-green-500';
    if (value >= 40) return 'bg-yellow-500';
    return 'bg-red-500';
  }

  // Objective Management
  addObjective() {
    if (!this.okrSession?.id) return;
    const currentUser = this.authStateService.getCurrentUser();
    if (currentUser && currentUser.role === RoleType.TeamManager) {
      // Only show teams managed by this user
      this.teamService.getTeamsBySessionId(this.okrSession.id).subscribe(teams => {
        this.availableTeams = teams.filter(team => team.teamManagerId === currentUser.id);
        this.selectedObjective = null;
        this.showNewObjectiveDrawer = true;
        this.lockBodyScroll();
      });
    } else {
      // Show all teams for other roles
      this.teamService.getTeamsBySessionId(this.okrSession.id).subscribe(teams => {
        this.availableTeams = teams;
        this.selectedObjective = null;
        this.showNewObjectiveDrawer = true;
        this.lockBodyScroll();
      });
    }
  }
  
  editObjective(objective: Objective) {
    this.selectedObjective = objective;
    this.openObjectiveDrawer();
  }
  // <button *ngIf="canEditObjective(objective)" (click)="editObjective(objective)">Edit</button>

  canDoActionsObjective(objective: Objective): boolean {
    const user = this.authStateService.getCurrentUser();
    if (!user) return false;
    
    // Prevent Collaborators from performing any CRUD operations
    if (user.role === RoleType.Collaborator) return false;
    
    if (user.role !== RoleType.TeamManager) return true;
    // Allow if user is responsible or in responsible team
    return this.teams.some(team => team.id === objective.responsibleTeamId);
  }
 
  deleteObjective(objective: Objective) {
    this.deleteConfirmationType = 'objective';
    this.deleteConfirmationTitle = 'Delete Objective';
    this.deleteConfirmationMessage = `Are you sure you want to delete the objective "${objective.title}"? This action cannot be undone and will also delete all related key results and tasks.`;
    this.itemToDelete = objective;
    this.showDeleteConfirmation = true;
  }

  onObjectiveSubmit(objective: Partial<Objective>) {
    const userId = this.authStateService.getCurrentUser()?.id;
    
    if (!userId) {
      this.showNotification('No user logged in', 'error');
      return;
    }
    
    // Set loading state to true at the start
    this.isSubmitting = true;
    
    // Convert dates to UTC
    const newObjective: Omit<Objective, 'id'> = {
      ...objective as Objective,
      okrSessionId: this.okrSession.id,
      userId: userId,
      status: Status.NotStarted,
      isDeleted: false,
      // Ensure dates are in UTC format
      startedDate: toUtcDate(objective.startedDate) as Date,
      endDate: toUtcDate(objective.endDate) as Date
    };

    this.objectiveService.createObjective(newObjective).subscribe({
      next: (response) => {
        this.loadObjectives();
        this.refreshOKRSessionProgress(); // <-- refresh session progress
        this.closeObjectiveDrawer();
        this.showNotification('Objective created successfully', 'success');
        // Reset loading state on success
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        // Some APIs return success with error codes 200/204 in the error handler
        if (error.status === 200 || error.status === 204) {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.closeObjectiveDrawer();
          this.showNotification('Objective created successfully', 'success');
        } else {
          this.showNotification('Failed to create objective: ' + (error.message || 'Unknown error'), 'error');
        }
        // Reset loading state on error (both success and failure cases)
        this.isSubmitting = false;
      },
      complete: () => {
        // Ensure we always clean up, regardless of success or failure
        // Also reset loading state in complete handler as a fallback
        this.isSubmitting = false;
      }
    });
  }

  onObjectiveUpdate(objective: Partial<Objective>) {
    const userId = this.authStateService.getCurrentUser()?.id;
    
    if (!userId) {
      this.showNotification('No user logged in', 'error');
      return;
    }
    
    if (this.selectedObjective) {
      // Set loading state to true
      this.isSubmitting = true;
      
      // Create a new object with UTC dates and ensure we include okrSessionId and userId
      const updatedObjective: Partial<Objective> = {
        ...objective,
        // These two fields are required by the backend
        okrSessionId: this.okrSession.id,
        userId: userId,
        // Convert dates to UTC if they exist in the update
        ...(objective.startedDate && { startedDate: toUtcDate(objective.startedDate) }),
        ...(objective.endDate && { endDate: toUtcDate(objective.endDate) })
      };

      this.objectiveService.updateObjective(this.selectedObjective.id, updatedObjective)
        .subscribe({
          next: (response) => {
            this.loadObjectives();
            this.refreshOKRSessionProgress(); // <-- refresh session progress
            this.closeObjectiveDrawer();
            this.showNotification('Objective updated successfully', 'success');
            this.isSubmitting = false;
          },
          error: (error: HttpErrorResponse) => {
            // Some APIs return success with error codes 200/204 in the error handler
            if (error.status === 200 || error.status === 204) {
              this.loadObjectives();
              this.refreshOKRSessionProgress(); // <-- refresh session progress
              this.closeObjectiveDrawer();
              this.showNotification('Objective updated successfully', 'success');
            } else {
              this.showNotification('Failed to update objective: ' + (error.message || 'Unknown error'), 'error');
            }
            this.isSubmitting = false;
          },
          complete: () => {
            // Ensure loading state is reset on completion
            this.isSubmitting = false;
          }
        });
    }
  }

  onObjectiveDelete() {
    if (this.selectedObjective) {
      this.deleteObjective(this.selectedObjective);
    }
  }

  // Key Result Management
  addKeyResult(objective: Objective) {
    // First, close the modal to reset the form
    this.closeKeyResultModal();
    
    // Force a delay to ensure component is destroyed and recreated
    setTimeout(() => {
      // Set the objective ID for the new key result
      this.selectedObjectiveId = objective.id;
      
      // Open the modal with a fresh form
      this.showNewKeyResultModal = true;
      this.lockBodyScroll();
    }, 50);
  }

  editKeyResult(objectiveId: string, keyResult: KeyResult) {
    this.selectedObjective = this.objectives.find(obj => obj.id === objectiveId) || null;
    this.selectedKeyResult = keyResult;
    this.showNewKeyResultModal = true;
    this.lockBodyScroll();
  }

  deleteKeyResult(objectiveId: string, keyResult: KeyResult) {
    this.deleteConfirmationType = 'keyResult';
    this.deleteConfirmationTitle = 'Delete Key Result';
    this.deleteConfirmationMessage = `Are you sure you want to delete the key result "${keyResult.title}"? This action cannot be undone and will also delete all related tasks.`;
    this.itemToDelete = keyResult;
    this.objectiveIdForDelete = objectiveId;
    this.showDeleteConfirmation = true;
  }

  onKeyResultSubmit(keyResult: Partial<KeyResult>) {
    // Set loading state
    this.isSubmitting = true;
    
    // Create the key result with all required fields
    const newKeyResult = {
      ...keyResult,
      objectiveId: this.selectedObjectiveId,
      userId: this.currentUserId,
      isDeleted: false,
      startDate: keyResult.startDate || new Date(),
      endDate: keyResult.endDate || new Date(),
      title: keyResult.title || '',
      progress: keyResult.progress || 0
    } as Omit<KeyResult, 'id'>;
    
    this.keyResultService.createKeyResult(newKeyResult).subscribe({
      next: (createdKeyResult) => {
        this.showNotification('Key result created successfully', 'success');
        
        // Close the modal first for better user experience
        this.closeKeyResultModal();
        
        // Then reload the data
        this.loadObjectives();
        this.refreshOKRSessionProgress(); // <-- refresh session progress
      },
      error: (error) => {
        // Double-check in case our service miss-handled any "success-errors"
        if (error.status === 200 || error.status === 204) {
          this.showNotification('Key result created successfully', 'success');
          this.closeKeyResultModal();
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
        } else {
          this.showNotification('Failed to create key result', 'error');
          // Even in case of error, reset the submission state
          this.isSubmitting = false;
        }
      },
      complete: () => {
        // Ensure we reset the state no matter what
        this.isSubmitting = false;
      }
    });
  }

  onKeyResultUpdate(keyResult: Partial<KeyResult>) {
    if (this.selectedKeyResult) {
      this.keyResultService.updateKeyResult(this.selectedKeyResult.id, keyResult)
        .subscribe({
          next: (response) => {
            this.loadObjectives();
            this.refreshOKRSessionProgress(); // <-- refresh session progress
            this.closeKeyResultModal();
            this.showNotification('Key Result updated successfully', 'success');
          },
          error: (error: HttpErrorResponse) => {
            if (error.status === 200 || error.status === 204) {
              this.loadObjectives();
              this.refreshOKRSessionProgress(); // <-- refresh session progress
              this.closeKeyResultModal();
              this.showNotification('Key Result updated successfully', 'success');
            } else {
              console.error('Error updating key result:', error);
              this.showNotification('Failed to update key result', 'error');
            }
          }
        });
    }
  }

  onKeyResultDelete() {
    if (this.selectedKeyResult && this.selectedObjective) {
      this.deleteKeyResult(this.selectedObjective.id, this.selectedKeyResult);
    }
  }

  // Task Management
  addTask(keyResultId: string) {
    const keyResult = this.objectives
      .reduce<KeyResult[]>((acc, obj) => 
        this.getObjectiveKeyResults(obj.id) ? [...acc, ...this.getObjectiveKeyResults(obj.id)] : acc
      , [])
      .find(kr => kr.id === keyResultId);

    if (keyResult) {
      this.selectedKeyResult = keyResult;
      this.selectedTask = null;
      this.showNewTaskModal = true;
      this.lockBodyScroll();
    }
  }

  editTask(objectiveId: string, keyResultId: string, task: KeyResultTask) {
    const keyResult = this.objectives
      .reduce<KeyResult[]>((acc, obj) => 
        this.getObjectiveKeyResults(obj.id) ? [...acc, ...this.getObjectiveKeyResults(obj.id)] : acc
      , [])
      .find(kr => kr.id === keyResultId);

    if (keyResult) {
      this.selectedKeyResult = keyResult;
      this.selectedTask = task;
      this.showNewTaskModal = true;
      this.lockBodyScroll();
    }
  }

  deleteTask(objectiveId: string, keyResultId: string, task: KeyResultTask) {
    this.deleteConfirmationType = 'task';
    this.deleteConfirmationTitle = 'Delete Task';
    this.deleteConfirmationMessage = `Are you sure you want to delete the task "${task.title}"? This action cannot be undone.`;
    this.itemToDelete = task;
    this.objectiveIdForDelete = objectiveId;
    this.keyResultIdForDelete = keyResultId;
    this.showDeleteConfirmation = true;
  }

  onTaskSubmit(task: Partial<KeyResultTask>) {
    if (this.selectedKeyResult) {
      const newTask: Omit<KeyResultTask, 'id'> = {
        ...task as KeyResult,
        keyResultId: this.selectedKeyResult.id,
        userId: this.currentUserId, // Use the current user ID
        collaboratorId: task.collaboratorId || '', // Add the collaboratorId property with a default empty string
        status: task.status || Status.NotStarted, // Use form status or default
        priority: task.priority || 2, // Medium priority default
        progress: task.progress || 0,
        isDeleted: false,
        // Use the dates from the form if provided, otherwise use current date
        startedDate: task.startedDate || new Date(),
        endDate: task.endDate || new Date(),
        // These tracking dates should always be now
        createdDate: new Date()
      };
      
      this.keyResultTaskService.createKeyResultTask(newTask).subscribe({
        next: (response) => {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.closeTaskModal();
          this.showNotification('Task created successfully', 'success');
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 200 || error.status === 204) {
            this.loadObjectives();
            this.refreshOKRSessionProgress(); // <-- refresh session progress
            this.closeTaskModal();
            this.showNotification('Task created successfully', 'success');
          } else {
            this.showNotification('Failed to create task', 'error');
          }
        }
      });
    }
  }

  onTaskUpdate(task: Partial<KeyResultTask>) {
    if (this.selectedTask) {
      this.keyResultTaskService.updateKeyResultTask(this.selectedTask.id, task).subscribe({
        next: (response) => {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.closeTaskModal();
          this.showNotification('Task updated successfully', 'success');
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 200 || error.status === 204) {
            this.loadObjectives();
            this.refreshOKRSessionProgress(); // <-- refresh session progress
            this.closeTaskModal();
            this.showNotification('Task updated successfully', 'success');
          } else {
            console.error('Error updating task:', error);
            this.showNotification('Failed to update task', 'error');
          }
        }
      });
    }
  }

  onTaskDelete() {
    if (this.selectedTask && this.selectedKeyResult && this.selectedObjective) {
      this.deleteTask(this.selectedObjective.id, this.selectedKeyResult.id, this.selectedTask);
    }
  }

  // Update toggleTaskStatus method to check for collaborator role
  toggleTaskStatus(keyResultId: string, taskId: string): void {
    // Get the task and its current status
    const tasks = this.getKeyResultTasks(keyResultId);
    const task = tasks.find(t => t.id === taskId);
    
    if (!task) {
      return;
    }
    
    // Check if the user has permission to toggle this task
    if (!this.canToggleTask(task)) {
      return;
    }

    // Find the objective ID for this task
    const objective = this.objectives.find(obj => 
      this.getObjectiveKeyResults(obj.id).some(kr => kr.id === keyResultId)
    );

    if (!objective) {
      return;
    }
    
    // Check if the task is currently completed using our consistent method
    const isCurrentlyCompleted = this.isTaskCompleted(task);
    const willComplete = !isCurrentlyCompleted;
    
    // Store the previous state
    const previousStatus = task.status;
    const previousProgress = task.progress;
    
    this.keyResultTaskService.toggleKeyResultTaskStatus(taskId, keyResultId, objective.id, willComplete)
      .subscribe({
        next: (updatedTask) => {
          if (updatedTask && updatedTask.id) {
            // Make sure we apply the update to the right task
            const taskIndex = tasks.findIndex(t => t.id === taskId);
            if (taskIndex !== -1) {
              tasks[taskIndex] = {
                ...tasks[taskIndex],
                status: updatedTask.status,
                progress: updatedTask.progress
              };
              
              // Force the UI to update
              this.objectiveKeyResultTasks.set(keyResultId, [...tasks]);
            }
            this.showNotification('Task status updated successfully', 'success');
          }
          // Reload objectives to update progress
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
        },
        error: (error) => {
          // Revert to previous state
          const taskIndex = tasks.findIndex(t => t.id === taskId);
          if (taskIndex !== -1) {
            tasks[taskIndex] = {
              ...tasks[taskIndex],
              status: previousStatus,
              progress: previousProgress
            };
            
            // Force the UI to update
            this.objectiveKeyResultTasks.set(keyResultId, [...tasks]);
          }
          this.showNotification('Failed to update task status', 'error');
        }
      });
  }

  // Add this method to explicitly check if a task is completed
  isTaskCompleted(task: KeyResultTask): boolean {
    // Explicitly check against Status.Completed (3)
    return task.status === Status.Completed;
  }

  // Update the task status class method for better reliability
  getTaskStatusClass(task: KeyResultTask): string {
    return this.isTaskCompleted(task) ? 'text-gray-500 line-through' : 'text-gray-900';
  }

  // Modal Management
  closeObjectiveDrawer() {
    this.showNewObjectiveDrawer = false;
    this.selectedObjective = null;
    this.isSubmitting = false;
    this.unlockBodyScroll();
  }

  closeKeyResultModal() {
    // First close the modal immediately for UX
    this.showNewKeyResultModal = false;
    
    // Then reset all state variables
    this.selectedKeyResult = null;
    this.selectedObjectiveId = '';
    this.isSubmitting = false;
    this.unlockBodyScroll();
  }

  closeTaskModal() {
    this.showNewTaskModal = false;
    this.selectedTask = null;
    this.unlockBodyScroll();
  }

  private showNotification(message: string, type: 'success' | 'error') {
    // Clear any existing timeout
    if (this.notificationTimeout) {
      clearTimeout(this.notificationTimeout);
    }
    
    this.notificationMessage = message;
    this.notificationType = type;
    this.isNotificationVisible = true;
    
    // Auto-hide notification after 2 seconds
    this.notificationTimeout = setTimeout(() => {
      this.closeNotification();
    }, 2000);
  }

  closeNotification() {
    this.isNotificationVisible = false;
    this.notificationMessage = '';
  }

  // PDF Export
  exportToPdf() {
    // Instead of directly calling the PDF export service, show the modal
    this.showPdfExportModal = true;
  }

  // Add method to close the PDF export modal
  closePdfExportModal() {
    this.showPdfExportModal = false;
  }

  // Add this helper method to check for overdue tasks with the correct type
  isTaskOverdue(dueDate: Date): boolean {
    if (!dueDate) return false;
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0); // Reset time part for date comparison
    
    // If dueDate is already a Date object
    if (dueDate instanceof Date) {
      return dueDate < currentDate;
    }
    
    // If dueDate is a string (ISO format)
    try {
      const date = new Date(dueDate);
      return date < currentDate;
    } catch (e) {
      return false;
    }
  }

  // Add this getter to the SessionObjectivesComponent class
  get currentUserId(): string {
    return this.authStateService.getCurrentUser()?.id || '';
  }

  // Wait for objectives to load before highlighting
  private waitForObjectivesAndHighlight() {
    // Only proceed if we're coming from alignment navigation
    if (!this.isFromAlignmentNavigation) {
      return;
    }
    
    // Start looking for the element after a small delay to allow components to render
    setTimeout(() => {
      // Check if directly available first
      if (this.objectives.length > 0 && !this.loading) {
        this.highlightElement();
        return;
      }
      
      // Create a more reliable observer to check when loading is complete
      const observer = new MutationObserver((mutations, obs) => {
        if (this.objectives.length > 0 && !this.loading) {
          this.highlightElement();
          obs.disconnect(); // Stop observing once we've found our element
          return;
        }
        
        // Also check directly for the element
        if (this.checkAndHighlightElement()) {
          obs.disconnect();
        }
      });
      
      // Start observing the document with the configured parameters
      observer.observe(document.body, { 
        childList: true, 
        subtree: true 
      });
      
      // Fallback with interval checks
      let attempts = 0;
      const maxAttempts = 20;
      const interval = setInterval(() => {
        attempts++;
        if (this.objectives.length > 0 && !this.loading) {
          this.highlightElement();
          clearInterval(interval);
        }
        if (attempts >= maxAttempts) {
          this.highlightElement();
          clearInterval(interval);
        }
      }, 500);
    }, 500);
  }

  // Add this method to check if the current user can toggle a specific task
  canToggleTask(task: KeyResultTask): boolean {
    const currentUser = this.authStateService.getCurrentUser();
    if (!currentUser) return false;
    
    // Only the assigned collaborator can toggle their own tasks
    return task.collaboratorId === currentUser.id;
  }

  // Add these methods to handle body scroll
  private lockBodyScroll(): void {
    document.body.style.overflow = 'hidden';
  }

  private unlockBodyScroll(): void {
    document.body.style.overflow = 'auto';
  }

  // Add method to open objective drawer
  openObjectiveDrawer() {
    this.showNewObjectiveDrawer = true;
    this.lockBodyScroll();
  }

  // Add method to determine element type
  private determineElementType(elementId: string | null): 'objective' | 'keyResult' | 'task' | null {
    if (!elementId) return null;
    
    // Try to determine type based on the parent element ID
    if (this.parentElementId) {
      // If there's a parent, it's likely a key result or task
      return 'keyResult';
    }
    // Without parent, assume it's an objective
    return 'objective';
  }

  // Add method to check and highlight element
  private checkAndHighlightElement(): boolean {
    console.log('Checking for element to highlight:', {
      type: this.highlightElementType,
      id: this.highlightElementId
    });
    
    if (!this.highlightElementType || !this.highlightElementId) {
      return false;
    }
    
    let selector = '';
    switch (this.highlightElementType) {
      case 'objective':
        selector = `#objective-${this.highlightElementId}`;
        break;
      case 'keyResult':
        selector = `#key-result-${this.highlightElementId}`;
        break;
      case 'task':
        selector = `#task-${this.highlightElementId}`;
        break;
    }
    
    if (!selector) {
      return false;
    }
    
    const element = document.querySelector(selector);
    if (element) {
      console.log('Element found, highlighting:', element);
      
      // Add highlight class
      element.classList.add('highlight-element');
      
      // Scroll to element
      element.scrollIntoView({ 
        behavior: 'smooth', 
        block: 'center' 
      });
      
      // Remove highlight after delay
      setTimeout(() => {
        element.classList.remove('highlight-element');
      }, 5000);
      
      return true;
    }
    
    return false;
  }

  // Add method to highlight element
  private highlightElement() {
    console.log('Attempting to highlight element:', {
      type: this.highlightElementType,
      id: this.highlightElementId,
      parent: this.parentElementId
    });
    
    // Make sure we have a valid element type and ID
    if (!this.highlightElementType || !this.highlightElementId) {
      console.error('Missing required highlight parameters');
      return;
    }
    
    // Wait a bit for the DOM to fully render
    setTimeout(() => {
      let elementSelector = '';
      
      switch (this.highlightElementType) {
        case 'objective':
          elementSelector = `#objective-${this.highlightElementId}`;
          break;
        case 'keyResult':
          elementSelector = `#key-result-${this.highlightElementId}`;
          break;
        case 'task':
          elementSelector = `#task-${this.highlightElementId}`;
          break;
      }
      
      if (!elementSelector) {
        console.error('Could not determine element selector for highlighting');
        return;
      }
      
      // Try to find the element
      const element = document.querySelector(elementSelector);
      if (!element) {
        console.error(`Element with selector ${elementSelector} not found in DOM`);
        
        // If it's a key result or task, we might need to expand its parent first
        if (this.highlightElementType === 'keyResult' && this.parentElementId) {
          const objective = document.querySelector(`#objective-${this.parentElementId}`);
          if (objective) {
            // Try to find and click the expand button
            const expandButton = objective.querySelector('.expand-button');
            if (expandButton && expandButton instanceof HTMLElement) {
              expandButton.click();
              // Try again after a delay
              setTimeout(() => this.highlightElement(), 500);
              return;
            }
          }
        } else if (this.highlightElementType === 'task' && this.parentElementId) {
          const keyResult = document.querySelector(`#key-result-${this.parentElementId}`);
          if (keyResult) {
            // Try to find and click the expand button
            const expandButton = keyResult.querySelector('.expand-button');
            if (expandButton && expandButton instanceof HTMLElement) {
              expandButton.click();
              // Try again after a delay
              setTimeout(() => this.highlightElement(), 500);
              return;
            }
          }
        }
        
        return;
      }
      
      console.log('Found element to highlight:', element);
      
      // Add highlight class
      element.classList.add('highlight-element');
      
      // Scroll to the element
      element.scrollIntoView({ 
        behavior: 'smooth', 
        block: 'center' 
      });
      
      // Remove the highlight after a delay
      setTimeout(() => {
        element.classList.remove('highlight-element');
      }, 5000); // Remove after 5 seconds
    }, 300); // Wait for DOM to be ready
  }

  // Add an isCollaborator helper method to check if the current user is a collaborator
  isCollaborator(): boolean {
    const user = this.authStateService.getCurrentUser();
    return user?.role === RoleType.Collaborator;
  }

  // Add this method to get collaborator information
  getCollaborator(collaboratorId: string): Observable<User | undefined> {
    // Check if we already have this collaborator in our cache
    if (this.collaborators[collaboratorId]) {
      return of(this.collaborators[collaboratorId]);
    }
    
    // If not in cache, fetch from the service
    return this.userService.getUserById(collaboratorId).pipe(
      tap(user => {
        if (user) {
          // Store in cache for future use
          this.collaborators[collaboratorId] = user;
        }
      })
    );
  }
  
  // Add method to get collaborator name
  getCollaboratorName(collaboratorId: string): string {
    const collaborator = this.collaborators[collaboratorId];
    if (collaborator) {
      return `${collaborator.firstName} ${collaborator.lastName}`;
    }
    return 'Unknown Collaborator';
  }

  // Add this method to check if user can perform actions on key results
  canDoActionsKeyResult(objectiveId: string): boolean {
    // Find the objective this key result belongs to
    const objective = this.objectives.find(obj => obj.id === objectiveId);
    if (!objective) return false;
    
    // Use the same permission logic as objectives
    return this.canDoActionsObjective(objective);
  }

  // Add this method to check if user can perform actions on tasks
  canDoActionsTask(objectiveId: string): boolean {
    // Reuse the key result permission logic
    return this.canDoActionsKeyResult(objectiveId);
  }

  // New method to handle confirmed deletions
  confirmDelete() {
    // Set loading state to true
    this.isDeleting = true;
    
    switch (this.deleteConfirmationType) {
      case 'objective':
        this.performObjectiveDelete();
        break;
      case 'keyResult':
        this.performKeyResultDelete();
        break;
      case 'task':
        this.performTaskDelete();
        break;
    }
  }

  // New method to cancel deletion
  cancelDelete() {
    this.showDeleteConfirmation = false;
    this.itemToDelete = null;
    this.isDeleting = false;
  }

  // Update deletion methods to handle loading state
  private performObjectiveDelete() {
    if (!this.itemToDelete) {
      this.isDeleting = false;
      return;
    }
    
    // Record start time
    const startTime = Date.now();
    
    this.objectiveService.deleteObjective(this.itemToDelete.id).subscribe({
      next: () => {
        // Calculate elapsed time
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        // Ensure loading is shown for minimum time
        setTimeout(() => {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.closeObjectiveDrawer(); // Close the drawer after successful deletion
          this.showNotification('Objective deleted successfully', 'success');
          this.showDeleteConfirmation = false;
          this.isDeleting = false;
        }, remainingTime);
      },
      error: (error) => {
        // Add delay for error case too
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        setTimeout(() => {
          this.showNotification('Failed to delete objective', 'error');
          this.isDeleting = false;
        }, remainingTime);
      }
    });
  }

  private performKeyResultDelete() {
    if (!this.itemToDelete) {
      this.isDeleting = false;
      return;
    }
    
    // Record start time
    const startTime = Date.now();
    
    this.keyResultService.deleteKeyResult(this.itemToDelete.id).subscribe({
      next: () => {
        // Calculate elapsed time
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        // Ensure loading is shown for minimum time
        setTimeout(() => {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.showNotification('Key Result deleted successfully', 'success');
          this.showDeleteConfirmation = false;
          this.isDeleting = false;
        }, remainingTime);
      },
      error: (error) => {
        // Add delay for error case too
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        setTimeout(() => {
          this.showNotification('Failed to delete key result', 'error');
          this.isDeleting = false;
        }, remainingTime);
      }
    });
  }

  private performTaskDelete() {
    if (!this.itemToDelete) {
      this.isDeleting = false;
      return;
    }
    
    // Record start time
    const startTime = Date.now();
    
    this.keyResultTaskService.deleteKeyResultTask(this.itemToDelete.id).subscribe({
      next: () => {
        // Calculate elapsed time
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        // Ensure loading is shown for minimum time
        setTimeout(() => {
          this.loadObjectives();
          this.refreshOKRSessionProgress(); // <-- refresh session progress
          this.showNotification('Task deleted successfully', 'success');
          this.showDeleteConfirmation = false;
          this.isDeleting = false;
        }, remainingTime);
      },
      error: (error) => {
        // Add delay for error case too
        const elapsedTime = Date.now() - startTime;
        const remainingTime = Math.max(0, this.minLoadingTime - elapsedTime);
        
        setTimeout(() => {
          this.showNotification('Failed to delete task', 'error');
          this.isDeleting = false;
        }, remainingTime);
      }
    });
  }
}