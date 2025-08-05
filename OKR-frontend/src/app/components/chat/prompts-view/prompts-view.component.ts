import { Component, EventEmitter, OnInit, Output } from '@angular/core';

interface SuggestedPrompt {
  title: string;
  icon: string;
  prompt: string;
}

@Component({
  selector: 'app-prompts-view',
  templateUrl: './prompts-view.component.html',
  styleUrls: ['./prompts-view.component.scss']
})
export class PromptsViewComponent implements OnInit {
  @Output() promptSelected = new EventEmitter<string>();
  @Output() backToChat = new EventEmitter<void>();

  suggestedPrompts: SuggestedPrompt[] = [
    {
      title: 'Create OKR Session',
      icon: 'tasks',
      prompt: 'Create a new OKR session for Q3 2024 with a focus on increasing market share and improving customer satisfaction'
    },
    {
      title: 'Track Progress',
      icon: 'chart-line',
      prompt: 'Help me track progress on my current OKR for improving product development cycle time'
    },
    {
      title: 'OKR Ideas',
      icon: 'lightbulb',
      prompt: 'Suggest some key results for an objective focused on improving team collaboration'
    },
    {
      title: 'Performance Review',
      icon: 'star',
      prompt: 'Help me prepare for my performance review based on my OKR achievements this quarter'
    },
    {
      title: 'OKR Alignment',
      icon: 'link',
      prompt: 'How do I align my team OKRs with the company\'s strategic objectives?'
    },
    {
      title: 'Improve Metrics',
      icon: 'chart-bar',
      prompt: 'Suggest ways to improve our key results metrics for the customer satisfaction objective'
    }
  ];

  constructor() { }

  ngOnInit(): void {
  }

  useSuggestedPrompt(prompt: string): void {
    this.promptSelected.emit(prompt);
  }
} 