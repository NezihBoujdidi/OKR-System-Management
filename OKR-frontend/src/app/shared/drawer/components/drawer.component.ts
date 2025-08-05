import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, Renderer2 } from '@angular/core';

@Component({
  selector: 'app-drawer',
  templateUrl: './drawer.component.html',
  styleUrls: ['./drawer.component.scss']
})
export class DrawerComponent implements OnInit, OnDestroy, OnChanges {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() position: 'left' | 'right' = 'right';
  @Output() closeDrawer = new EventEmitter<void>();

  private scrollY = 0;

  constructor(private renderer: Renderer2) {}

  ngOnInit() {
    if (this.isOpen) {
      this.disableBackgroundScroll();
    }
  }

  ngOnDestroy() {
    this.enableBackgroundScroll();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.disableBackgroundScroll();
      } else {
        this.enableBackgroundScroll();
      }
    }
  }

  private disableBackgroundScroll(): void {
    this.scrollY = window.scrollY;
    this.renderer.setStyle(document.body, 'position', 'fixed');
    this.renderer.setStyle(document.body, 'width', '100%');
    this.renderer.setStyle(document.body, 'top', `-${this.scrollY}px`);
  }

  private enableBackgroundScroll(): void {
    this.renderer.removeStyle(document.body, 'position');
    this.renderer.removeStyle(document.body, 'width');
    this.renderer.removeStyle(document.body, 'top');
    window.scrollTo(0, this.scrollY);
  }

  onClose() {
    this.closeDrawer.emit();
  }

  stopPropagation(event: Event) {
    event.stopPropagation();
  }
}