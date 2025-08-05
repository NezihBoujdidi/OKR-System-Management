import { Component, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { PdfImportService } from '../../../../services/pdf-import.service';
import { OKRSessionService } from '../../../../services/okr-session.service';
import { ObjectiveService } from '../../../../services/objective.service';
import { OKRSession } from '../../../../models/okr-session.interface';

@Component({
  selector: 'app-import-okr',
  templateUrl: './import-okr.component.html'
})
export class ImportOKRComponent {
  @ViewChild('fileInput') fileInput!: ElementRef;
  
  isImporting = false;
  importProgress = 0;
  statusMessage = '';
  error: string | null = null;
  isDragging = false;

  constructor(
    private pdfImportService: PdfImportService,
    private okrSessionService: OKRSessionService,
    private objectiveService: ObjectiveService,
    private router: Router
  ) {
    this.pdfImportService.importProgress$.subscribe(
      progress => this.importProgress = progress
    );
    this.pdfImportService.statusMessage$.subscribe(
      message => this.statusMessage = message
    );
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  async handleFile(file: File) {
    if (!file) return;

    if (file.type !== 'application/pdf') {
      this.error = 'Please select a PDF file';
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.error = 'File size exceeds 10MB limit';
      return;
    }

    try {
      this.isImporting = true;
      this.error = null;

      const { session, objectives } = await this.pdfImportService.importPdf(file);
      
      if (!session || !objectives) {
        throw new Error('Invalid PDF format');
      }

      // Create the session
      const newSession = await this.okrSessionService.createOKRSession(session as Omit<OKRSession, 'id'>).toPromise();
      
      if (newSession) {
        // Create the objectives
        for (const objective of objectives) {
          await this.objectiveService.createObjective(objective);
        }

        // Navigate to the new session
        this.router.navigate(['/okrs', newSession.id]);
      }
      
    } catch (error: any) {
      console.error('Error importing PDF:', error);
      this.error = error.message || 'Error importing PDF. Please try again.';
    } finally {
      this.isImporting = false;
    }
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.handleFile(file);
    }
  }

  triggerFileInput() {
    this.fileInput.nativeElement.click();
  }
}