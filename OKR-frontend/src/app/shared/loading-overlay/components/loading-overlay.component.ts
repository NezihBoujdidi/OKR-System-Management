import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-loading-overlay',
  templateUrl: './loading-overlay.component.html'
})
export class LoadingOverlayComponent implements OnInit {
  @Input() show: boolean = false;
  @Input() message?: string;

  particlePositions: Array<{top: number, left: number, delay: number}> = [];

  ngOnInit() {
    // Initialize particle positions once during component initialization
    this.particlePositions = Array(5).fill(0).map(() => ({
      top: this.random(0, 100),
      left: this.random(0, 100),
      delay: this.random(0, 2000)
    }));
  }

  private random(min: number, max: number): number {
    return Math.floor(Math.random() * (max - min + 1) + min);
  }
}