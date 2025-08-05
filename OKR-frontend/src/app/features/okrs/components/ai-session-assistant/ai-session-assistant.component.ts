import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormControl } from '@angular/forms';
import { debounceTime, finalize } from 'rxjs/operators';
import { AiAssistantService, OkrSuggestion } from '../../../../services/ai-assistant.service';
import { Team } from '../../../../models/team.interface';

@Component({
  selector: 'app-ai-session-assistant',
  templateUrl: './ai-session-assistant.component.html'
})
export class AiSessionAssistantComponent implements OnInit, OnChanges {
  @Input() availableTeams: Team[] = [];
  @Output() suggestionSelected = new EventEmitter<OkrSuggestion>();
  
  promptControl = new FormControl('');
  isLoading = false;
  isExpanded = true; // Start expanded by default
  errorMessage = '';
  suggestions: OkrSuggestion[] = [];
  
  // Sample prompt suggestions based on real-world OKR frameworks
  promptSuggestions = [
    'Revenue growth and market expansion strategy',
    'Customer experience and satisfaction improvement',
    'Digital transformation and innovation',
    'Operational excellence and efficiency',
    'Team performance and talent development'
  ];

  constructor(private aiService: AiAssistantService) {}

  ngOnInit() {
    // Setup the prompt input listener with debounce
    this.promptControl.valueChanges
      .pipe(
        debounceTime(800)
      )
      .subscribe(value => {
        // Only auto-generate if there's a substantial prompt
        if (value && value.length > 12) {
          this.generateSuggestion();
        }
      });
    
    // Generate initial suggestion
    setTimeout(() => {
      if (this.isExpanded && this.availableTeams.length > 0) {
        this.generateQuickSuggestion();
      }
    }, 1000);
  }
  
  ngOnChanges(changes: SimpleChanges) {
    // If teams change and we have teams available, generate a new suggestion
    if (changes['availableTeams'] && !changes['availableTeams'].firstChange) {
      if (this.isExpanded && this.availableTeams.length > 0 && this.suggestions.length === 0) {
        this.generateQuickSuggestion();
      }
    }
  }
  
  /**
   * Get the name of a team by its ID
   * @param teamId The team ID to look up
   * @returns The team name, or 'Unknown Team' if not found
   */
  getTeamName(teamId: string): string {
    const team = this.availableTeams.find(t => t.id === teamId || t.id.toString() === teamId.toString());
    return team ? team.name : 'Unknown Team';
  }
  
  /**
   * Toggle the expanded state of the AI assistant panel
   */
  toggleExpand() {
    this.isExpanded = !this.isExpanded;
    
    // Generate a quick suggestion when expanding if no suggestions exist
    if (this.isExpanded && this.suggestions.length === 0 && this.availableTeams.length > 0) {
      this.generateQuickSuggestion();
    }
  }
  
  /**
   * Handle prompt suggestion click
   * @param suggestion The prompt suggestion text
   */
  usePromptSuggestion(suggestion: string) {
    this.promptControl.setValue(suggestion);
    this.generateSuggestion();
  }
  
  /**
   * Generate a quick suggestion without a specific prompt
   */
  generateQuickSuggestion() {
    this.isLoading = true;
    this.errorMessage = '';
    this.suggestions = [];
    this.aiService.getOkrSessionSuggestions('', this.availableTeams).subscribe({
      next: (suggestion: OkrSuggestion) => {
        this.suggestions = [suggestion];
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = error.message || 'Failed to generate suggestion';
        this.isLoading = false;
      }
    });
  }

  /**
   * Generate a suggestion based on the user's prompt
   */
  generateSuggestion() {
    const prompt = this.promptControl.value || '';
    this.isLoading = true;
    this.errorMessage = '';
    this.suggestions = [];
    this.aiService.getOkrSessionSuggestions(prompt, this.availableTeams).subscribe({
      next: (suggestion: OkrSuggestion) => {
        this.suggestions = [suggestion];
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = error.message || 'Failed to generate suggestion';
        this.isLoading = false;
      }
    });
  }
  
  /**
   * Apply a suggestion to the form
   * @param suggestion The OKR suggestion to apply
   */
  applySuggestion(suggestion: OkrSuggestion) {
    this.suggestionSelected.emit(suggestion);
  }
}