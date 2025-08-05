import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, Renderer2 } from '@angular/core';

@Component({
  selector: 'app-drawer',
  templateUrl: './drawer.component.html'
})
export class DrawerComponent implements OnInit, OnDestroy, OnChanges {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() position: 'left' | 'right' = 'right';
  @Output() closeDrawer = new EventEmitter<void>();

  private scrollPosition = 0;

  constructor(private renderer: Renderer2) {}

  ngOnInit() {
    if (this.isOpen) {
      this.lockScroll();
    }
  }

  ngOnDestroy() {
    this.unlockScroll();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.lockScroll();
      } else {
        this.unlockScroll();
      }
    }
  }

  private lockScroll(): void {
    this.scrollPosition = window.scrollY;
    this.renderer.setStyle(document.body, 'position', 'fixed');
    this.renderer.setStyle(document.body, 'top', `-${this.scrollPosition}px`);
    this.renderer.setStyle(document.body, 'width', '100%');
    this.renderer.setStyle(document.body, 'overflow-y', 'scroll');
  }

  private unlockScroll(): void {
    this.renderer.removeStyle(document.body, 'position');
    this.renderer.removeStyle(document.body, 'top');
    this.renderer.removeStyle(document.body, 'width');
    this.renderer.removeStyle(document.body, 'overflow-y');
    window.scrollTo(0, this.scrollPosition);
  }

  onClose() {
    this.closeDrawer.emit();
  }

  stopPropagation(event: Event) {
    event.stopPropagation();
  }
} 