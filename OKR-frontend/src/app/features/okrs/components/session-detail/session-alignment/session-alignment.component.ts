import { Component, Input, OnInit, OnChanges, SimpleChanges, AfterViewInit, ElementRef, ViewChild, NgZone, ChangeDetectorRef } from '@angular/core';
import * as go from 'gojs';
import { OKRSession } from '../../../../../models/okr-session.interface';
import { Objective } from '../../../../../models/objective.interface';
import { KeyResult } from '../../../../../models/key-result.interface';
import { KeyResultTask } from '../../../../../models/key-result-task.interface';
import { ObjectiveService } from '../../../../../services/objective.service';
import { KeyResultService } from '../../../../../services/key-result.service';
import { KeyResultTaskService } from '../../../../../services/key-result-task.service';
import { forkJoin, switchMap, tap, timer, of } from 'rxjs';
import { Status } from '../../../../../models/Status.enum';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthStateService } from '../../../../../services/auth-state.service';
import { OkrAiInsightsService } from 'src/app/shared/services/okr-ai-insights.service';

// Interface for AI insights request
interface SessionInsightsRequest {
  sessionId: string;
  userContext: {
    userId: string;
    userName: string;
    email: string;
    organizationId: string;
    role: string;
    selectedLLMProvider: string;
  };
}

@Component({
  selector: 'app-session-alignment',
  templateUrl: './session-alignment.component.html'
})
export class SessionAlignmentComponent implements OnInit, OnChanges, AfterViewInit {
  @Input() okrSession!: OKRSession;
  objectives: Objective[] = [];
  keyResults: KeyResult[] = [];
  tasks: KeyResultTask[] = [];
  loading = false;
  showPlaceholder = false;
  diagramInitialized = false;
  @ViewChild('okrDiagramDiv', { static: false }) diagramDivRef?: ElementRef;
  private diagram: go.Diagram | null = null;
  private nodeDataArray: any[] = [];
  private prevFills = new Map<go.Shape, go.Brush>(); // Store previous fills

  // Theme colors for the diagram
  private themeColors = {
    background: '#fff',
    text: '#111827',
    textHighlight: '#11a8cd',
    subtext: '#6b7280',
    badge: '#f0fdf4',
    badgeBorder: '#16a34a33',
    badgeText: '#15803d',
    divider: '#e5e7eb',
    shadow: '#9ca3af',
    tooltip: '#1f2937',
    dragOver: '#f0f9ff',
    link: '#9ca3af',
    div: '#f3f4f6',
    levels: [
      '#fbbf24', // Session (yellow/amber)
      '#8b5cf6', // Objective (purple)
      '#10b981', // Key Result (green)
      '#3b82f6'  // Tasks (blue)
    ]
  };

  // Add properties for new UI features
  nodeCount = 0;
  zoomLevel = 100;
  showAiInsights = true;
  sessionProgress = 0;
  objectiveCount = 0;
  objectiveProgress = 0;
  keyResultCount = 0;
  keyResultProgress = 0;
  taskCount = 0; 
  taskProgress = 0;

  // AI insights
  aiInsights: string[] = [];
  currentAiInsight = '';
  aiInsightIndex = 0;
  aiInsightsLoading = false;
  aiInsightsError = '';
  @ViewChild('miniMapDiv', { static: false }) miniMapDivRef?: ElementRef;
  private miniMap: go.Overview | null = null;
  private originalNodeDataArray: any[] = [];

  constructor(
    private objectiveService: ObjectiveService,
    private keyResultService: KeyResultService,
    private keyResultTaskService: KeyResultTaskService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private route: ActivatedRoute,
    private okrAiInsightsService: OkrAiInsightsService,
    private authStateService: AuthStateService,
    private elementRef: ElementRef
  ) {}

  ngOnInit() {
    console.log("Okr Session Received for the ALIGNMENT PAGE", this.okrSession);
    this.fetchObjectivesAndKeyResults();
    this.fetchAiInsights();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['okrSession'] && !changes['okrSession'].firstChange) {
      this.fetchObjectivesAndKeyResults();
      this.fetchAiInsights();
    }
  }

  ngAfterViewInit() {
    // Wait a bit to ensure the DOM is ready before initializing the diagram
    timer(100).subscribe(() => {
    this.initDiagram();
    this.updateDiagram();
    });
  }

  private fetchObjectivesAndKeyResults() {
    if (!this.okrSession) {
      console.log('[Alignment] No okrSession input.');
      this.objectives = [];
      this.keyResults = [];
      this.tasks = [];
      this.showPlaceholder = true;
      this.loading = false;
      this.updateDiagram();
      return;
    }
    
    this.loading = true;
    this.showPlaceholder = false;
    console.log('[Alignment] Fetching objectives for session:', this.okrSession.id);
    
    // Step 1: Get all objectives for the session
    this.objectiveService.getObjectivesBySessionId(this.okrSession.id).subscribe({
      next: objectives => {
        console.log('[Alignment] Objectives fetched:', objectives);
        this.objectives = objectives;
        
        if (!objectives.length) {
          this.keyResults = [];
          this.tasks = [];
          this.loading = false;
          this.showPlaceholder = true;
          this.updateDiagram();
          return;
        }
        
        // Step 2: Process objectives in batches
        const BATCH_SIZE = 3; // Adjust based on your backend capacity
        this.keyResults = []; // Reset key results array
        this.tasks = []; // Reset tasks array
        this.processObjectiveBatch(objectives, 0, BATCH_SIZE);
      },
      error: error => {
        console.error('[Alignment] Error fetching objectives:', error);
        this.objectives = [];
        this.keyResults = [];
        this.tasks = [];
        this.loading = false;
        this.showPlaceholder = true;
        this.updateDiagram();
      }
    });
  }

  // Process a batch of objectives in parallel
  private processObjectiveBatch(objectives: Objective[], startIndex: number, batchSize: number) {
    if (startIndex >= objectives.length) {
      // All objectives processed
      console.log('[Alignment] All objectives processed');
      this.loading = false;
      this.showPlaceholder = this.keyResults.length === 0;
      this.updateDiagram();
      return;
    }
    
    // Calculate the end index for this batch
    const endIndex = Math.min(startIndex + batchSize, objectives.length);
    console.log(`[Alignment] Processing objective batch ${startIndex} to ${endIndex - 1}`);
    
    // Create observables for key results in this batch
    const batchObservables = [];
    for (let i = startIndex; i < endIndex; i++) {
      const objective = objectives[i];
      
      // Create an observable for processing this objective's key results
      const objectiveObservable = this.keyResultService.getKeyResultsByObjectiveId(objective.id)
        .pipe(
          tap(keyResults => {
            console.log(`[Alignment] Found ${keyResults.length} key results for objective: ${objective.id}`);
            // Add these key results to our collection
            this.keyResults = [...this.keyResults, ...keyResults];
            
            // For each key result, fetch its tasks
            keyResults.forEach(kr => {
              this.keyResultTaskService.getKeyResultTasksByKeyResultId(kr.id).subscribe({
                next: tasks => {
                  console.log(`[Alignment] Found ${tasks.length} tasks for key result: ${kr.id}`);
                  // Add these tasks to our collection
                  this.tasks = [...this.tasks, ...tasks];
                  // Force update when tasks are added
                  this.updateDiagram();
                },
                error: error => {
                  console.error(`[Alignment] Error fetching tasks for key result ${kr.id}`, error);
                }
              });
            });
          })
        );
      
      batchObservables.push(objectiveObservable);
    }
    
    // Process this batch in parallel
    forkJoin(batchObservables)
      .subscribe({
        next: () => {
          // Force change detection
          this.cdr.detectChanges();
          
          // Update diagram with currently loaded data for better UX
          this.updateDiagram();
          
          // When this batch is done, move to the next batch
          this.processObjectiveBatch(objectives, endIndex, batchSize);
        },
        error: (error) => {
          console.error('[Alignment] Error processing objective batch:', error);
          // Continue with next batch even if there's an error
          this.processObjectiveBatch(objectives, endIndex, batchSize);
        }
      });
  }

  private buildDiagramData() {
    if (!this.okrSession) return [];
    
    try {
      // Session node - this will be the root node
      const sessionNode = {
        key: this.okrSession.id,
        name: this.okrSession.title || "OKR Session",
        type: 'session',
        progress: this.okrSession.progress || 0,
        owner: 'OS',
        dept: 'Session',
        level: 0,
        color: this.themeColors.levels[0],
        status: this.getStatusText(this.okrSession.status)
      };
      
      // Create objective nodes
      const objectiveNodes = (this.objectives || [])
        .filter(obj => obj && obj.id) // Only include objectives with valid IDs
        .map((obj, index) => ({
          key: obj.id,
          name: obj.title || "Objective",
          type: 'objective',
          parent: this.okrSession.id,
          progress: obj.progress || 0,
          owner: this.getOwnerInitials(obj),
          dept: this.getStatusText(obj.status),
          level: 1,
          color: this.themeColors.levels[1],
          status: this.getStatusText(obj.status)
        }));
      
      // Create key result nodes
      const keyResultNodes = (this.keyResults || [])
        .filter(kr => kr && kr.id && kr.objectiveId)
        .map((kr, index) => ({
          key: kr.id,
          name: kr.title || "Key Result",
          type: 'keyResult',
          parent: kr.objectiveId,
          progress: kr.progress || 0,
          owner: 'KR',
          dept: this.getStatusText(kr.status),
          level: 2,
          color: this.themeColors.levels[2],
          status: this.getStatusText(kr.status)
        }));
      
      // Create task nodes
      const taskNodes = (this.tasks || [])
        .filter(task => task && task.id && task.keyResultId)
        .map((task, index) => ({
          key: task.id,
          name: task.title || "Task",
          type: 'task',
          parent: task.keyResultId,
          progress: task.progress || 0,
          owner: 'TK',
          dept: this.getStatusText(task.status),
          level: 3,
          color: this.themeColors.levels[3],
          status: this.getStatusText(task.status),
          priority: this.getPriorityText(task.priority)
        }));
      
      console.log('[Alignment] Generated diagram data:', {
        sessionNode,
        objectiveCount: objectiveNodes.length,
        keyResultCount: keyResultNodes.length,
        taskCount: taskNodes.length
      });
      
      this.nodeDataArray = [sessionNode, ...objectiveNodes, ...keyResultNodes, ...taskNodes];
      return this.nodeDataArray;
    } catch (error) {
      console.error('[Alignment] Error building diagram data:', error);
      return [];
    }
  }
  
  private getOwnerInitials(obj: Objective): string {
    // Get initials from user name if available (dummy logic for now)
    return 'OB';
  }
  
  private getStatusText(status?: Status): string {
    if (status === undefined) return 'Not Started';
    
    switch (status) {
      case Status.NotStarted: return 'Not Started';
      case Status.InProgress: return 'In Progress';
      case Status.Completed: return 'Completed';
      case Status.Overdue: return 'Overdue';
      default: return 'Unknown';
    }
  }
  
  private getPriorityText(priority?: number): string {
    if (priority === undefined) return 'Medium';
    
    switch (priority) {
      case 1: return 'Low';
      case 2: return 'Medium';
      case 3: return 'High';
      default: return 'Medium';
    }
  }

  private initDiagram() {
    try {
      // Check if the element exists in the DOM
      const element = document.getElementById('okrDiagramDiv');
      if (!element) {
        console.error('[Alignment] Diagram div not found in DOM');
        setTimeout(() => this.initDiagram(), 200); // Retry after a delay
        return;
      }

      this.zone.runOutsideAngular(() => {
        const $ = go.GraphObject.make;
            
        // Create diagram with horizontal tree layout
        this.diagram = $(go.Diagram, 'okrDiagramDiv', {
          initialAutoScale: go.Diagram.UniformToFill,
          contentAlignment: go.Spot.Center,
          layout: $(go.TreeLayout, {
            treeStyle: go.TreeLayout.StyleLastParents,
            arrangement: go.TreeLayout.ArrangementHorizontal,
            angle: 0,
            layerSpacing: 120,
            nodeSpacing: 40,
            alternateAngle: 0,
            alternateLayerSpacing: 120,
            alternateAlignment: go.TreeLayout.AlignmentBus,
            alternateNodeSpacing: 40
          }),
          "undoManager.isEnabled": false,
          allowDelete: false,
          allowCopy: false,
          maxSelectionCount: 1,
          "InitialLayoutCompleted": (e: go.DiagramEvent) => {
            e.diagram.commandHandler.zoomToFit();
            e.diagram.scale = 0.8; // Show at 80% scale by default
          }
        });
        
        // Common node template
        const nodeTemplate = $(go.Node, "Auto", {
          locationSpot: go.Spot.Center,
          isShadowed: true,
          shadowOffset: new go.Point(0, 2),
          shadowColor: this.themeColors.shadow,
          // Switch back to doubleClick for node interaction
          doubleClick: (e, node) => {
            // Run inside Angular zone to ensure proper change detection
            this.zone.run(() => this.handleNodeClick((node as any).data));
            // Prevent default GoJS behaviors
            e.handled = true;
          },
          toolTip: $("ToolTip",
            $(go.Panel, "Vertical",
              { margin: 8 },
              $(go.TextBlock,
                { 
                  font: "bold 12pt sans-serif", 
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 0, 5, 0)
                },
                new go.Binding("text", "name")
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "status", s => "Status: " + s)
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "progress", p => "Progress: " + p + "%")
              ),
              $(go.TextBlock,
                { 
                  font: "10pt sans-serif", 
                  stroke: this.themeColors.text,
                  visible: false
                },
                new go.Binding("text", "priority", p => "Priority: " + p),
                new go.Binding("visible", "priority", p => p !== undefined)
              ),
              $(go.TextBlock,
                { 
                  font: "italic 9pt sans-serif", 
                  stroke: this.themeColors.textHighlight,
                  margin: new go.Margin(5, 0, 0, 0)
                },
                "Double-click to view details"
              )
            )
          )
        });
        
        // Add binding and visual elements to the template
        nodeTemplate.bind("text", "name");
        
        // Add visual structure
        nodeTemplate.add(
          $(go.Panel, "Spot",
            $(go.Shape, "RoundedRectangle", {
              name: "SHAPE",
              fill: "white",
              stroke: this.themeColors.divider,
              strokeWidth: 1,
              spot1: go.Spot.TopLeft,
              spot2: go.Spot.BottomRight,
              minSize: new go.Size(240, 110)
            }),
            // The colored side bar
            $(go.Shape, "RoundedLeftRectangle", {
              alignment: go.Spot.Left,
              alignmentFocus: go.Spot.Left,
              width: 6,
              stretch: go.GraphObject.Fill,
              fill: "orange", // default color
              strokeWidth: 0
            }).bind("fill", "color"),
            
            // Content panel - with title, badge, etc.
            $(go.Panel, "Table", {
              defaultAlignment: go.Spot.Left,
              margin: new go.Margin(12, 12, 12, 12),
              defaultColumnSeparatorStroke: this.themeColors.divider,
              defaultRowSeparatorStroke: this.themeColors.divider
            })
            .add(
              // First row with name and badge
              $(go.Panel, "Horizontal", { 
                row: 0, 
                columnSpan: 2, 
                alignment: go.Spot.Left,
                stretch: go.GraphObject.Horizontal,
                maxSize: new go.Size(220, NaN)
              },
                $(go.TextBlock, {
                  font: "500 0.875rem sans-serif",
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 8, 0, 0),
                  maxSize: new go.Size(140, NaN),
                  minSize: new go.Size(50, NaN),
                  overflow: go.TextBlock.OverflowEllipsis,
                  wrap: go.TextBlock.WrapFit,
                  textAlign: "left"
                }).bind("text", "name"),
                $(go.Panel, "Auto", { margin: new go.Margin(0, 0, 0, 4) },
                  $(go.Shape, "Capsule", {
                    fill: this.themeColors.badge,
                    stroke: this.themeColors.badgeBorder
                  }),
                  $(go.TextBlock, {
                    font: "500 0.75rem sans-serif",
                    stroke: this.themeColors.badgeText,
                    margin: new go.Margin(2, 6, 2, 6),
                    maxSize: new go.Size(60, NaN),
                    overflow: go.TextBlock.OverflowEllipsis
                  }).bind("text", "dept")
                )
              ),
              
              // Priority badge (only for tasks)
              $(go.Panel, "Horizontal", { 
                row: 1, 
                columnSpan: 2,
                margin: new go.Margin(4, 0, 0, 0),
                alignment: go.Spot.Left,
                visible: false
              },
              new go.Binding("visible", "priority", p => p !== undefined),
                $(go.Panel, "Auto", { margin: new go.Margin(0, 0, 4, 0) },
                  $(go.Shape, "Capsule", {
                    fill: "#f3f4f6",
                    stroke: "#d1d5db"
                  }),
                  $(go.TextBlock, {
                    font: "500 0.7rem sans-serif",
                    stroke: "#4b5563",
                    margin: new go.Margin(1, 6, 1, 6)
                  }).bind("text", "priority", p => "Priority: " + p)
                )
              ),
              
              // Second row with owner circle and progress
              $(go.Panel, "Horizontal", { 
                row: 2, 
                columnSpan: 2,
                margin: new go.Margin(8, 0, 0, 0),
                alignment: go.Spot.Left,
                defaultAlignment: go.Spot.Left
              },
                // Owner circle
                $(go.Panel, "Auto", { 
                  width: 28, 
                  height: 28,
                  margin: new go.Margin(0, 10, 0, 0) 
                },
                  $(go.Shape, "Circle", { 
                    fill: "gray",
                    stroke: null 
                  }).bind("fill", "color"),
                  $(go.TextBlock, { 
                    font: "bold 0.7rem sans-serif", 
                    stroke: "white", 
                    textAlign: "center" 
                  }).bind("text", "owner")
                ),
                
                // Progress indicator
                $(go.Panel, "Horizontal", { 
                  alignment: go.Spot.Right,
                  margin: new go.Margin(0, 0, 0, 20)
                },
                  $(go.Shape, "Rectangle", {
                    fill: "#e5e7eb",
                    stroke: null,
                    width: 70,
                    height: 8
                  }),
                  $(go.Shape, "Rectangle", {
                    alignment: go.Spot.Left,
                    fill: "#10b981", // default green
                    stroke: null,
                    width: 0,
                    height: 8
                  }).bind("width", "progress", (p: number) => p * 0.7), // 70px * percentage
                  $(go.TextBlock, {
                    margin: new go.Margin(0, 0, 0, 8),
                    font: "bold 0.75rem sans-serif",
                    stroke: this.themeColors.text
                  }).bind("text", "progress", (p: number) => p + "%")
                )
              )
            )
          )
        );
        
        // Task template - add click handler here too
        const taskTemplate = $(go.Node, "Auto", {
          locationSpot: go.Spot.Center,
          isShadowed: true,
          shadowOffset: new go.Point(0, 2),
          shadowColor: this.themeColors.shadow,
          // Switch back to doubleClick
          doubleClick: (e, node) => {
            this.zone.run(() => this.handleNodeClick((node as any).data));
            e.handled = true;
          },
          toolTip: $("ToolTip",
            $(go.Panel, "Vertical",
              { margin: 8 },
              $(go.TextBlock,
                { 
                  font: "bold 12pt sans-serif", 
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 0, 5, 0)
                },
                new go.Binding("text", "name")
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "status", s => "Status: " + s)
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "progress", p => "Progress: " + p + "%")
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "priority", p => "Priority: " + p)
              ),
              $(go.TextBlock,
                { 
                  font: "italic 9pt sans-serif", 
                  stroke: this.themeColors.textHighlight,
                  margin: new go.Margin(5, 0, 0, 0)
                },
                "Double-click to navigate to task"
              )
            )
          )
        });

        // Add the same structure as the node template but with smaller dimensions
        taskTemplate.add(
          $(go.Panel, "Spot",
            $(go.Shape, "RoundedRectangle", {
              name: "SHAPE",
              fill: "white",
              stroke: this.themeColors.divider,
              strokeWidth: 1,
              spot1: go.Spot.TopLeft,
              spot2: go.Spot.BottomRight,
              minSize: new go.Size(220, 110) // Slightly smaller
            }),
            // The colored side bar
            $(go.Shape, "RoundedLeftRectangle", {
              alignment: go.Spot.Left,
              alignmentFocus: go.Spot.Left,
              width: 6,
              stretch: go.GraphObject.Fill,
              fill: this.themeColors.levels[3], // Use task color
              strokeWidth: 0
            }),
            
            // Content panel - with title, badge, etc.
            $(go.Panel, "Table", {
              defaultAlignment: go.Spot.Left,
              margin: new go.Margin(12, 12, 12, 12),
              defaultColumnSeparatorStroke: this.themeColors.divider,
              defaultRowSeparatorStroke: this.themeColors.divider
            })
            .add(
              // First row with name and badge
              $(go.Panel, "Horizontal", { 
                row: 0, 
                columnSpan: 2, 
                alignment: go.Spot.Left,
                stretch: go.GraphObject.Horizontal,
                maxSize: new go.Size(220, NaN)
              },
                $(go.TextBlock, {
                  font: "500 0.875rem sans-serif",
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 8, 0, 0),
                  maxSize: new go.Size(120, NaN), // Smaller for more space for the badge
                  minSize: new go.Size(50, NaN),
                  overflow: go.TextBlock.OverflowEllipsis,
                  wrap: go.TextBlock.WrapFit,
                  textAlign: "left"
                }).bind("text", "name"),
                $(go.Panel, "Auto", { margin: new go.Margin(0, 0, 0, 4) },
                  $(go.Shape, "Capsule", {
                    fill: this.themeColors.badge,
                    stroke: this.themeColors.badgeBorder
                  }),
                  $(go.TextBlock, {
                    font: "500 0.75rem sans-serif",
                    stroke: this.themeColors.badgeText,
                    margin: new go.Margin(2, 6, 2, 6),
                    maxSize: new go.Size(60, NaN),
                    overflow: go.TextBlock.OverflowEllipsis
                  }).bind("text", "dept")
                )
              ),
              
              // Priority badge row
              $(go.Panel, "Horizontal", {
                row: 1,
                columnSpan: 2,
                margin: new go.Margin(4, 0, 0, 0),
                alignment: go.Spot.Left
              },
                $(go.Panel, "Auto", { margin: new go.Margin(0, 0, 4, 0) },
                  $(go.Shape, "Capsule", {
                    fill: "#f3f4f6",
                    stroke: "#d1d5db"
                  }),
                  $(go.TextBlock, {
                    font: "500 0.7rem sans-serif",
                    stroke: "#4b5563",
                    margin: new go.Margin(1, 6, 1, 6)
                  }).bind("text", "priority", p => "Priority: " + p)
                )
              ),
              
              // Owner and progress row
              $(go.Panel, "Horizontal", { 
                row: 2, 
                columnSpan: 2,
                margin: new go.Margin(8, 0, 0, 0),
                alignment: go.Spot.Left,
                defaultAlignment: go.Spot.Left
              },
                // Owner circle
                $(go.Panel, "Auto", { 
                  width: 24, // Slightly smaller 
                  height: 24, // Slightly smaller
                  margin: new go.Margin(0, 10, 0, 0) 
                },
                  $(go.Shape, "Circle", { 
                    fill: this.themeColors.levels[3],
                    stroke: null 
                  }),
                  $(go.TextBlock, { 
                    font: "bold 0.65rem sans-serif", // Smaller font
                    stroke: "white", 
                    textAlign: "center" 
                  }).bind("text", "owner")
                ),
                
                // Progress indicator
                $(go.Panel, "Horizontal", { 
                  alignment: go.Spot.Right,
                  margin: new go.Margin(0, 0, 0, 20)
                },
                  $(go.Shape, "Rectangle", {
                    fill: "#e5e7eb",
                    stroke: null,
                    width: 60, // Smaller width
                    height: 6   // Smaller height
                  }),
                  $(go.Shape, "Rectangle", {
                    alignment: go.Spot.Left,
                    fill: this.themeColors.levels[3],
                    stroke: null,
                    width: 0,
                    height: 6
                  }).bind("width", "progress", (p: number) => p * 0.6), // 60px * percentage
                  $(go.TextBlock, {
                    margin: new go.Margin(0, 0, 0, 8),
                    font: "bold 0.7rem sans-serif", // Smaller font
                    stroke: this.themeColors.text
                  }).bind("text", "progress", (p: number) => p + "%")
                )
              )
            )
          )
        );
        
        // Session template - add click handler here too
        const sessionTemplate = $(go.Node, "Auto", {
          locationSpot: go.Spot.Center,
          isShadowed: true,
          shadowOffset: new go.Point(0, 3),
          shadowColor: this.themeColors.shadow,
          // Switch back to doubleClick
          doubleClick: (e, node) => {
            this.zone.run(() => this.handleNodeClick((node as any).data));
            e.handled = true;
          },
          toolTip: $("ToolTip",
            $(go.Panel, "Vertical",
              { margin: 8 },
              $(go.TextBlock,
                { 
                  font: "bold 12pt sans-serif", 
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 0, 5, 0)
                },
                new go.Binding("text", "name")
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "status", s => "Status: " + s)
              ),
              $(go.TextBlock,
                { font: "10pt sans-serif", stroke: this.themeColors.text },
                new go.Binding("text", "progress", p => "Progress: " + p + "%")
              ),
              $(go.TextBlock,
                { 
                  font: "italic 9pt sans-serif", 
                  stroke: this.themeColors.textHighlight,
                  margin: new go.Margin(5, 0, 0, 0)
                },
                "Double-click to view session details"
              )
            )
          )
        });
        
        // Add visual structure for session nodes (similar to nodeTemplate but with special styling)
        sessionTemplate.add(
          $(go.Panel, "Spot",
            $(go.Shape, "RoundedRectangle", {
              name: "SHAPE",
              fill: this.themeColors.levels[0] + "15", // Light version of the session color
              stroke: this.themeColors.levels[0],
              strokeWidth: 2,
              spot1: go.Spot.TopLeft,
              spot2: go.Spot.BottomRight,
              minSize: new go.Size(300, 110)
            }),
            // The colored bar
            $(go.Shape, "RoundedLeftRectangle", {
              alignment: go.Spot.Left,
              alignmentFocus: go.Spot.Left,
              width: 8, // Slightly wider for emphasis
              stretch: go.GraphObject.Fill,
              fill: this.themeColors.levels[0],
              strokeWidth: 0
            }),
            
            // Content panel - with title, badge, etc.
            $(go.Panel, "Table", {
              defaultAlignment: go.Spot.Left,
              margin: new go.Margin(12, 12, 12, 12),
              defaultColumnSeparatorStroke: this.themeColors.divider,
              defaultRowSeparatorStroke: this.themeColors.divider
            })
            .add(
              // First row with name and badge
              $(go.Panel, "Horizontal", { 
                row: 0, 
                columnSpan: 2, 
                alignment: go.Spot.Left,
                stretch: go.GraphObject.Horizontal,
                maxSize: new go.Size(280, NaN)
              },
                $(go.TextBlock, {
                  font: "600 1rem sans-serif", // Bolder and larger for session title
                  stroke: this.themeColors.text,
                  margin: new go.Margin(0, 8, 0, 0),
                  maxSize: new go.Size(200, NaN),
                  minSize: new go.Size(50, NaN),
                  overflow: go.TextBlock.OverflowEllipsis,
                  wrap: go.TextBlock.WrapFit,
                  textAlign: "left"
                }).bind("text", "name"),
                $(go.Panel, "Auto", { margin: new go.Margin(0, 0, 0, 4) },
                  $(go.Shape, "Capsule", {
                    fill: this.themeColors.badge,
                    stroke: this.themeColors.badgeBorder
                  }),
                  $(go.TextBlock, {
                    font: "500 0.75rem sans-serif",
                    stroke: this.themeColors.badgeText,
                    margin: new go.Margin(2, 6, 2, 6),
                    maxSize: new go.Size(60, NaN),
                    overflow: go.TextBlock.OverflowEllipsis
                  }).bind("text", "dept")
                )
              ),
              
              // Second row with owner circle and progress
              $(go.Panel, "Horizontal", { 
                row: 1, 
                columnSpan: 2,
                margin: new go.Margin(8, 0, 0, 0),
                alignment: go.Spot.Left,
                defaultAlignment: go.Spot.Left
              },
                // Owner circle
                $(go.Panel, "Auto", { 
                  width: 32, // Slightly bigger for emphasis
                  height: 32, // Slightly bigger for emphasis
                  margin: new go.Margin(0, 10, 0, 0) 
                },
                  $(go.Shape, "Circle", { 
                    fill: this.themeColors.levels[0],
                    stroke: null 
                  }),
                  $(go.TextBlock, { 
                    font: "bold 0.75rem sans-serif", 
                    stroke: "white", 
                    textAlign: "center" 
                  }).bind("text", "owner")
                ),
                
                // Progress indicator
                $(go.Panel, "Horizontal", { 
                  alignment: go.Spot.Right,
                  margin: new go.Margin(0, 0, 0, 20)
                },
                  $(go.Shape, "Rectangle", {
                    fill: "#e5e7eb",
                    stroke: null,
                    width: 80, // Wider for emphasis
                    height: 10  // Taller for emphasis
                  }),
                  $(go.Shape, "Rectangle", {
                    alignment: go.Spot.Left,
                    fill: this.themeColors.levels[0],
                    stroke: null,
                    width: 0,
                    height: 10
                  }).bind("width", "progress", (p: number) => p * 0.8), // 80px * percentage
                  $(go.TextBlock, {
                    margin: new go.Margin(0, 0, 0, 8),
                    font: "bold 0.875rem sans-serif", // Larger for emphasis
                    stroke: this.themeColors.text
                  }).bind("text", "progress", (p: number) => p + "%")
                )
              )
            )
          )
        );
        
        // Set up template map for different node types
        const templmap = new go.Map<string, go.Node>();
        templmap.add("session", sessionTemplate);
        templmap.add("task", taskTemplate);
        templmap.add("", nodeTemplate); // Default template
        
        this.diagram.nodeTemplateMap = templmap;
        this.diagram.nodeTemplate = nodeTemplate;  // default template
        
        // Define the link template
        this.diagram.linkTemplate = $(go.Link, {
          routing: go.Link.Orthogonal,
          corner: 15,
          selectable: false,
          relinkableFrom: false,
          relinkableTo: false
        })
        .add(
          $(go.Shape, {
            stroke: this.themeColors.link,
            strokeWidth: 2
          })
        )
        .add(
          $(go.Shape, {
            toArrow: "Triangle",
            fill: this.themeColors.link,
            stroke: null,
            scale: 1.2
          })
        );
      });

      this.diagramInitialized = true;
      console.log('[Alignment] Diagram initialized successfully');
      
      // Initial update
      this.updateDiagram();
      
    } catch (error) {
      console.error('[Alignment] Error initializing diagram:', error);
    }
  }

  private updateDiagram() {
    try {
      // Run outside Angular's zone for better performance
      this.zone.runOutsideAngular(() => {
        if (!this.diagram) {
          console.log('[Alignment] Diagram not initialized yet');
          if (!this.diagramInitialized) {
            this.initDiagram();
          }
          return;
        }
        
        if (!this.okrSession || !this.objectives.length) {
          this.showPlaceholder = true;
          return;
        }
        
        // Even if there are no key results, we should still show the session and objectives
        this.showPlaceholder = false;
        const data = this.buildDiagramData();
        this.originalNodeDataArray = [...data]; // Store a copy of the original data
        
        console.log('[Alignment] Updating diagram with data:', this.originalNodeDataArray);
        
        if (this.originalNodeDataArray.length > 0) {
          // Try to apply the new model
          try {
            // Update node count - should reflect the displayed nodes
            this.nodeCount = this.originalNodeDataArray.length;
            
            // Create a new model
            const model = new go.TreeModel();
            model.nodeDataArray = this.originalNodeDataArray;
            
            // Apply the model
            this.diagram.model = model;
            
            // Layout and zoom to fit
            this.diagram.layoutDiagram(true);
            
            // Make sure all content is visible
            this.diagram.contentAlignment = go.Spot.Center;
            setTimeout(() => {
              if (this.diagram) {
                this.diagram.zoomToFit();
                this.diagram.scale = Math.min(0.9, this.diagram.scale); // Limit maximum zoom
                this.updateZoomLevel();
                
                // Initialize minimap if needed
                if (!this.miniMap) {
                  this.initMiniMap();
                }
              }
            }, 100);
            
            console.log('[Alignment] Diagram updated successfully');
            
            // Update statistics
            this.updateStats();
          } catch (e) {
            console.error('[Alignment] Error updating diagram model:', e);
          }
        }
      });
      
      // Run in Angular zone to update UI
      this.zone.run(() => {
        this.loading = false;
        this.cdr.detectChanges();
      });
      
    } catch (error) {
      console.error('[Alignment] Error in updateDiagram:', error);
      
      // Make sure to update UI state even if there's an error
      this.zone.run(() => {
        this.loading = false;
        this.cdr.detectChanges();
      });
    }
  }

  // Public method to force redraw the diagram - can be called from template if needed
  public redrawDiagram(): void {
    console.log('[Alignment] Manually redrawing diagram');
    if (this.diagram) {
      this.diagram.layoutDiagram(true);
      this.diagram.zoomToFit();
    } else {
      this.initDiagram();
    }
  }

  // Add methods for zoom controls
  zoomIn() {
    if (this.diagram) {
      this.diagram.commandHandler.increaseZoom();
      this.updateZoomLevel();
    }
  }

  zoomOut() {
    if (this.diagram) {
      this.diagram.commandHandler.decreaseZoom();
      this.updateZoomLevel();
    }
  }

  private updateZoomLevel() {
    if (this.diagram) {
      this.zoomLevel = Math.round(this.diagram.scale * 100);
      this.zone.run(() => this.cdr.detectChanges());
    }
  }

  // Smart layout
  applySmartLayout() {
    if (!this.diagram) return;
    
    this.diagram.startTransaction("Smart Layout");
    
    // Make layout more hierarchical with better spacing
    const layout = this.diagram.layout as go.TreeLayout;
    layout.angle = 0;
    layout.layerSpacing = 150; // More space between levels
    layout.nodeSpacing = 50;   // More space between nodes
    layout.arrangement = go.TreeLayout.ArrangementHorizontal;
    
    // Apply animation
    this.diagram.animationManager.isEnabled = true;
    this.diagram.commitTransaction("Smart Layout");
    
    // Force relayout
    this.diagram.layoutDiagram(true);
    
    // Center and fit content
    setTimeout(() => {
      if (this.diagram) {
        this.diagram.zoomToFit();
        this.updateZoomLevel();
      }
    }, 100);
  }

  // AI Insights
  toggleAiInsights() {
    this.showAiInsights = !this.showAiInsights;
  }

  getNextInsight() {
    this.aiInsightIndex = (this.aiInsightIndex + 1) % this.aiInsights.length;
    this.currentAiInsight = this.aiInsights[this.aiInsightIndex];
  }

  fetchAiInsights() {
    if (!this.okrSession || !this.okrSession.id) {
      this.aiInsights = [];
      this.currentAiInsight = '';
      this.aiInsightIndex = 0;
      return;
    }
    this.aiInsightsLoading = true;
    this.aiInsightsError = '';
    const currentUser = this.authStateService.getCurrentUser();
    const userContext = {
      userId: currentUser?.id || '',
      userName: currentUser ? `${currentUser.firstName} ${currentUser.lastName}` : '',
      email: currentUser?.email || '',
      organizationId: currentUser?.organizationId || '',
      role: currentUser?.role || '',
      selectedLLMProvider: 'azureopenai'
    };
    const req: SessionInsightsRequest = {
      sessionId: this.okrSession.id,
      userContext
    };
    this.okrAiInsightsService.getSessionInsights(req).subscribe({
      next: (res: { insights: string[] }) => {
        this.aiInsights = res.insights || [];
        this.aiInsightIndex = 0;
        this.currentAiInsight = this.aiInsights[0] || '';
        this.aiInsightsLoading = false;
      },
      error: (err: any) => {
        this.aiInsights = [];
        this.currentAiInsight = '';
        this.aiInsightIndex = 0;
        this.aiInsightsLoading = false;
        this.aiInsightsError = 'Failed to load AI insights.';
      }
    });
  }

  // Initialize minimap
  private initMiniMap() {
    if (!this.diagram || !this.miniMapDivRef?.nativeElement) return;
    
    const $ = go.GraphObject.make;
    
    this.miniMap = $(go.Overview, this.miniMapDivRef.nativeElement, {
      observed: this.diagram,
      contentAlignment: go.Spot.Center
    });
  }

  // Add the methods to handle node clicking
  private handleNodeClick(nodeData: any) {
    if (!nodeData) return;
    
    console.log('Node clicked:', nodeData);
    
    // Get session info for navigation
    const sessionId = this.okrSession.id;
    const organizationId = this.okrSession.organizationId;
    
    // Add a loading indication
    this.loading = true;
    
    // Use a direct navigation approach without relying on localStorage
    switch (nodeData.type) {
      case 'session':
        // Just navigate to session
        this.directNavigateToSession(sessionId, organizationId);
        break;
      case 'objective':
        // Navigate directly to objective
        this.directNavigateToObjective(nodeData.key, sessionId, organizationId);
        break;
      case 'keyResult':
        // Navigate directly to key result
        this.directNavigateToKeyResult(nodeData.key, nodeData.parent, sessionId, organizationId);
        break;
      case 'task':
        // Navigate directly to task
        this.directNavigateToTask(nodeData.key, nodeData.parent, sessionId, organizationId);
        break;
      default:
        // If type is not recognized, navigate to session
        console.warn(`Unknown node type: ${nodeData.type}, defaulting to session navigation`);
        this.directNavigateToSession(sessionId, organizationId);
        break;
    }
  }

  // Direct navigation methods that bypass localStorage issues
  directNavigateToSession(sessionId: string, organizationId?: string) {
    console.log(`Directly navigating to session: ${sessionId}`);
    
    // Navigate to session details with objectives tab explicitly specified
    this.zone.run(() => {
      if (organizationId) {
        this.router.navigate(['/organizations', organizationId, 'okrs', sessionId], 
          { queryParams: { tab: 'objectives' } });
      } else {
        this.router.navigate(['/okrs', sessionId], 
          { queryParams: { tab: 'objectives' } });
      }
    });
  }

  directNavigateToObjective(objectiveId: string, sessionId: string, organizationId?: string) {
    console.log(`Directly navigating to objective: ${objectiveId}`);
    
    // Use custom URL fragment to highlight element
    this.zone.run(() => {
      if (organizationId) {
        this.router.navigate(['/organizations', organizationId, 'okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'objective',
              highlightId: objectiveId
            } 
          });
      } else {
        this.router.navigate(['/okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'objective',
              highlightId: objectiveId
            } 
          });
      }
    });
  }

  directNavigateToKeyResult(keyResultId: string, objectiveId: string, sessionId: string, organizationId?: string) {
    console.log(`Directly navigating to key result: ${keyResultId}, objective: ${objectiveId}`);
    
    // Use custom URL fragment to highlight element
    this.zone.run(() => {
      if (organizationId) {
        this.router.navigate(['/organizations', organizationId, 'okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'keyResult',
              highlightId: keyResultId,
              parentId: objectiveId
            } 
          });
      } else {
        this.router.navigate(['/okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'keyResult',
              highlightId: keyResultId,
              parentId: objectiveId
            } 
          });
      }
    });
  }

  directNavigateToTask(taskId: string, keyResultId: string, sessionId: string, organizationId?: string) {
    console.log(`Directly navigating to task: ${taskId}, key result: ${keyResultId}`);
    
    // Use custom URL fragment to highlight element
    this.zone.run(() => {
      if (organizationId) {
        this.router.navigate(['/organizations', organizationId, 'okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'task',
              highlightId: taskId,
              parentId: keyResultId
            } 
          });
      } else {
        this.router.navigate(['/okrs', sessionId], 
          { 
            queryParams: { 
              tab: 'objectives',
              highlightTarget: 'task',
              highlightId: taskId,
              parentId: keyResultId
            } 
          });
      }
    });
  }

  // Update statistics
  private updateStats() {
    // Calculate session progress
    this.sessionProgress = this.okrSession.progress || 0;
    
    // Count objectives and their progress
    this.objectiveCount = this.objectives.length;
    if (this.objectiveCount > 0) {
      let total = 0;
      this.objectives.forEach(obj => total += obj.progress || 0);
      this.objectiveProgress = Math.round(total / this.objectiveCount);
    } else {
      this.objectiveProgress = 0;
    }
    
    // Count key results and their progress
    this.keyResultCount = this.keyResults.length;
    if (this.keyResultCount > 0) {
      let total = 0;
      this.keyResults.forEach(kr => total += kr.progress || 0);
      this.keyResultProgress = Math.round(total / this.keyResultCount);
    } else {
      this.keyResultProgress = 0;
    }
    
    // Count tasks and their progress
    this.taskCount = this.tasks.length;
    if (this.taskCount > 0) {
      let total = 0;
      this.tasks.forEach(task => total += task.progress || 0);
      this.taskProgress = Math.round(total / this.taskCount);
    } else {
      this.taskProgress = 0;
    }
    
    // Update UI
    this.zone.run(() => this.cdr.detectChanges());
  }
}