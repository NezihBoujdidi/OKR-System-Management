import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Location } from '@angular/common';

@Component({
  selector: 'app-okr-best-practices',
  templateUrl:'./okr-best-practices.component.html'
})
export class OkrBestPracticesComponent {
  constructor(
    private router: Router,
    private location: Location
  ) {}

  goBack() {
    // Check if we have a previous state
    if (this.location.getState()) {
      this.location.back();
    } else {
      // If no previous state, check if current URL contains organizationAdmin
      if (this.router.url.includes('organizationAdmin')) {
        this.router.navigate(['/home/organizationAdmin']);
      } else {
        this.router.navigate(['/home']);
      }
    }
  }

  bestPractices = [
    {
      title: 'Keep Objectives Simple & Clear',
      description: 'Write objectives that are easy to understand and remember. Avoid jargon and complex language.',
      icon: 'M13 10V3L4 14h7v7l9-11h-7z' // Lightning bolt
    },
    {
      title: 'Make Key Results Measurable',
      description: 'Ensure your key results are quantifiable and have specific metrics to track progress.',
      icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' // Chart
    },
    {
      title: 'Set Ambitious but Achievable Goals',
      description: 'Aim for goals that push your team while remaining within the realm of possibility.',
      icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6' // Trending up
    },
    {
      title: 'Align with Company Strategy',
      description: 'Ensure your OKRs support and align with overall company objectives.',
      icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4' // Building
    }
  ];

  examples = [
    {
      objective: 'Improve Customer Satisfaction',
      keyResults: [
        'Increase NPS score from 30 to 50',
        'Reduce customer support response time from 24h to 6h',
        'Achieve 95% customer satisfaction rating'
      ]
    },
    {
      objective: 'Accelerate Product Development',
      keyResults: [
        'Reduce sprint cycle time from 2 weeks to 1 week',
        'Increase deployment frequency from 2/week to 10/week',
        'Maintain 99.9% uptime during changes'
      ]
    }
  ];
} 