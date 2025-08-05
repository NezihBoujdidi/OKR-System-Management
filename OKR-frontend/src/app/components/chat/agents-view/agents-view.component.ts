import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

interface Agent {
  id: string;
  name: string;
  icon: string;
  description: string;
}

@Component({
  selector: 'app-agents-view',
  templateUrl: './agents-view.component.html',
  styleUrls: ['./agents-view.component.scss']
})
export class AgentsViewComponent implements OnInit {
  @Input() selectedAgentId: string = 'gpt4';
  @Output() agentSelected = new EventEmitter<Agent>();
  @Output() backToChat = new EventEmitter<void>();
  
  availableAgents: Agent[] = [
    {
      id: 'gpt4',
      name: 'GPT-4',
      icon: 'assets/img/agents/gpt4-icon.svg',
      description: 'Advanced AI with superior reasoning and OKR management capabilities'
    },
    {
      id: 'grok',
      name: 'Grok',
      icon: 'assets/img/agents/grok-icon.svg',
      description: 'Real-time AI with up-to-date data on goal tracking and performance metrics'
    },
    {
      id: 'claude',
      name: 'Claude',
      icon: 'assets/img/agents/claude-icon.svg',
      description: 'Balanced assistant for everyday OKR planning and management'
    },
    {
      id: 'cohere',
      name: 'Cohere',
      icon: 'assets/img/agents/cohere-icon.svg',
      description: 'Specialized in structured data analysis and team performance insights'
    }
  ];

  constructor() { }

  ngOnInit(): void {
  }

  selectAgent(agent: Agent): void {
    this.selectedAgentId = agent.id;
    this.agentSelected.emit(agent);
  }

  getSelectedAgentName(): string {
    const agent = this.availableAgents.find(a => a.id === this.selectedAgentId);
    return agent ? agent.name : 'AI Assistant';
  }

  getSelectedAgentIcon(): string {
    const agent = this.availableAgents.find(a => a.id === this.selectedAgentId);
    return agent ? agent.icon : '';
  }
} 