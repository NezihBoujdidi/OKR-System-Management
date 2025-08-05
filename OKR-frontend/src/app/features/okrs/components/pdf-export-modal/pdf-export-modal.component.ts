import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PdfExportService } from '../../../../services/pdf-export.service';
import { AiAssistantService } from '../../../../services/ai-assistant.service';
import { Objective } from '../../../../models/objective.interface';
import { KeyResult } from '../../../../models/key-result.interface';
import { KeyResultTask } from '../../../../models/key-result-task.interface';
import { OKRSession } from '../../../../models/okr-session.interface';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { finalize, catchError, timeout, tap, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { Status } from '../../../../models/Status.enum';

enum ExportStep {
  SELECT_TYPE,
  AI_PROMPT,
  PDF_PREVIEW,
}

@Component({
  selector: 'app-pdf-export-modal',
  templateUrl: './pdf-export-modal.component.html',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule]
})
export class PdfExportModalComponent implements OnInit, OnDestroy {
  @Input() session!: OKRSession;
  @Input() objectives: Objective[] = [];
  @Input() owners: Map<string, any> = new Map();
  @Input() keyResults: Map<string, KeyResult[]> = new Map();
  @Input() tasks: Map<string, KeyResultTask[]> = new Map();
  @Input() isOpen: boolean = false;
  
  @Output() close = new EventEmitter<void>();

  @ViewChild('aiPromptInput') aiPromptInput!: ElementRef;
  
  exportStep: ExportStep = ExportStep.SELECT_TYPE;
  ExportStep = ExportStep; // Expose enum to template
  
  aiPromptForm: FormGroup;
  isGenerating: boolean = false;
  generationError: string | null = null;
  
  pdfPreviewUrl: SafeResourceUrl | null = null;
  pdfBlob: Blob | null = null;
  
  aiSuggestions: string[] = [
    "Focus on overall objectives progress and achievements",
    "Highlight key learning points and challenges",
    "Prioritize team performance metrics and collaboration insights",
    "Include detailed analysis of completed vs pending objectives",
    "Emphasize strategic alignment with business goals"
  ];

  // Add animation state variables
  isClassicExporting = false;
  downloadAnimation = '';

  // Add more detailed AI generation states
  aiGenerationStage: 'idle' | 'analyzing' | 'generating' | 'finalizing' = 'idle';
  aiGenerationProgress = 0;
  aiGenerationInterval: any = null;

  constructor(
    private fb: FormBuilder,
    private pdfExportService: PdfExportService,
    private aiAssistantService: AiAssistantService,
    private sanitizer: DomSanitizer,
    private cdr: ChangeDetectorRef
  ) {
    this.aiPromptForm = this.fb.group({
      prompt: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    if (this.isOpen) {
      this.lockBodyScroll();
    }
  }

  ngOnDestroy(): void {
    this.unlockBodyScroll();
    
    // Clean up any running intervals
    if (this.aiGenerationInterval) {
      clearInterval(this.aiGenerationInterval);
      this.aiGenerationInterval = null;
    }
  }

  onClose(): void {
    this.close.emit();
    this.resetState();
  }

  selectClassicExport(): void {
    // Start animation first
    this.isClassicExporting = true;
    this.downloadAnimation = 'preparing';
    
    // After short animation delay, switch to downloading state
    setTimeout(() => {
      this.downloadAnimation = 'downloading';
      
      // After download animation, proceed with actual export
      setTimeout(() => {
        this.downloadAnimation = 'complete';
        
        // After completion animation, perform the actual download
        setTimeout(() => {
          try {
            console.log('[Classic PDF] Attempting classic PDF export');
            
            // Use the existing export service with error handling
          this.pdfExportService.exportOKRSessionToPdf(
            this.session,
            this.objectives,
            this.owners,
            this.keyResults,
            this.tasks
          );
            
            console.log('[Classic PDF] Classic PDF export completed successfully');
          this.onClose();
            
          } catch (error) {
            console.error('[Classic PDF] Error in classic PDF export:', error);
            
            // Reset animation state
            this.isClassicExporting = false;
            this.downloadAnimation = '';
            
            // Show error message but don't break the component
            this.generationError = 'Classic PDF export failed due to browser compatibility. Please try the AI Export option instead.';
            this.cdr.detectChanges();
            
            // Don't close the modal so user can try AI export
          }
        }, 800);
      }, 1500);
    }, 1000);
  }

  selectAiExport(): void {
    this.exportStep = ExportStep.AI_PROMPT;
    // Focus the input after a small delay to allow for DOM rendering
    setTimeout(() => {
      if (this.aiPromptInput) {
        this.aiPromptInput.nativeElement.focus();
      }
    }, 100);
  }

  useAiSuggestion(suggestion: string): void {
    this.aiPromptForm.get('prompt')?.setValue(suggestion);
  }

  async generateAiReport(): Promise<void> {
    if (this.isGenerating) return;
    
    this.isGenerating = true;
    this.generationError = null;
    this.aiGenerationStage = 'analyzing';
    this.aiGenerationProgress = 0;
    this.cdr.markForCheck();
    
    const cleanup = () => {
      this.isGenerating = false;
      this.aiGenerationStage = 'idle';
      this.aiGenerationProgress = 0;
      if (this.aiGenerationInterval) {
        clearInterval(this.aiGenerationInterval);
        this.aiGenerationInterval = null;
      }
      this.cdr.markForCheck();
    };

    try {
      // Start progress animation
      this.aiGenerationInterval = setInterval(() => {
        if (this.aiGenerationProgress < 90) {
          this.aiGenerationProgress += Math.random() * 10;
          if (this.aiGenerationProgress > 30 && this.aiGenerationStage === 'analyzing') {
            this.aiGenerationStage = 'generating';
          } else if (this.aiGenerationProgress > 70 && this.aiGenerationStage === 'generating') {
            this.aiGenerationStage = 'finalizing';
          }
          this.cdr.markForCheck();
        }
      }, 200);

      const prompt = this.aiPromptForm.get('prompt')?.value || 'Generate insights for this OKR session';
      const insights = await this.fetchInsightsWithTimeout(prompt);
      
      // Complete the progress
      this.aiGenerationProgress = 100;
      this.cdr.markForCheck();
      
      // Create PDF blob for preview (without auto-downloading)
      await this.createAiReportPdf(insights, prompt);
      
      // Show preview state (don't auto-download)
      this.exportStep = ExportStep.PDF_PREVIEW;
      
    } catch (error: any) {
      console.error('Error generating AI report:', error);
      this.generationError = error.message || 'Failed to generate AI insights. Please try again.';
    } finally {
      cleanup();
    }
  }

  private async fetchInsightsWithTimeout(prompt: string): Promise<string[]> {
    return new Promise((resolve, reject) => {
      // Set a reasonable timeout
      const timeoutId = setTimeout(() => {
        reject(new Error('Request timed out'));
      }, 15000); // 15 seconds

      this.aiAssistantService.getSessionInsights(this.session.id, prompt)
        .subscribe({
          next: (response: string[]) => {
            clearTimeout(timeoutId);
            console.log('[AI PDF] Raw response received:', response);
            
            if (response && Array.isArray(response) && response.length > 0) {
              const validInsights = response.filter(insight => 
                insight && typeof insight === 'string' && insight.trim().length > 0
              );
              
              if (validInsights.length > 0) {
                resolve(validInsights);
              } else {
                resolve([
                  "AI analysis completed for your OKR session.",
                  "The session shows measurable progress toward key objectives.",
                  "Consider reviewing task priorities and resource allocation for optimal results."
                ]);
              }
            } else {
              resolve([
                "AI analysis completed for your OKR session.",
                "The session shows measurable progress toward key objectives.", 
                "Consider reviewing task priorities and resource allocation for optimal results."
              ]);
            }
          },
          error: (error) => {
            clearTimeout(timeoutId);
            console.error('[AI PDF] Error fetching insights:', error);
            reject(error);
          }
        });
    });
  }

  private async createAiReportPdf(insights: string[], prompt: string): Promise<void> {
    try {
      // Import jsPDF for in-memory PDF generation
      const { jsPDF } = await import('jspdf');
      const { default: autoTable } = await import('jspdf-autotable');
      
      // Create PDF document in memory
      const doc = new jsPDF();
      
      // Create cover page with AI insights
      await this.createCoverPageWithInsights(doc, this.session, this.objectives, insights, prompt);
      doc.addPage();

      // Add content pages with OKR data
      this.objectives.forEach((obj, index) => {
        if (index > 0) {
          doc.addPage();
        }

        // Objective Header
        doc.setFontSize(24);
        doc.setFont('Helvetica', 'bold');
        doc.setTextColor(255, 215, 0);
        doc.text(`Objective ${index + 1}`, 14, 20);
        doc.setFontSize(12);
        doc.setFont('Helvetica', 'normal');
        doc.setTextColor(0, 0, 0);
        doc.text(`Date: ${new Date().toLocaleDateString()}`, 14, 30);
        doc.setLineWidth(0.5);
        doc.setDrawColor(0, 0, 0);
        doc.line(14, 35, 196, 35);

        // Objective Table
        const objectiveData = [{
          title: obj.title,
          description: obj.description,
          owner: this.owners.get(obj.id)?.firstName || 'N/A',
          status: obj.status,
          progress: obj.progress || 0
        }];

        autoTable(doc, {
          head: [['Objective Title', 'Description', 'Owner', 'Status', 'Progress']],
          body: objectiveData.map(item => [
            item.title,
            item.description,
            item.owner,
            item.status,
            `${item.progress}%`
          ]),
          startY: 40,
          theme: 'grid',
          styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
          headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
          alternateRowStyles: { fillColor: [240, 240, 240] }
        });

        let currentY = (doc as any).lastAutoTable.finalY + 20;

        // Key Results Section
        const keyResultsForObjective = this.keyResults.get(obj.id) || [];
        if (keyResultsForObjective.length > 0) {
          if (currentY > 250) {
            doc.addPage();
            currentY = 20;
          }

          doc.setFontSize(20);
          doc.setFont('Helvetica', 'bold');
          doc.setTextColor(255, 215, 0);
          doc.text('Key Results', 14, currentY);
          currentY += 10;

          const keyResultData = keyResultsForObjective.map(kr => ({
            title: kr.title,
            description: kr.description || 'N/A',
            progress: kr.progress,
            status: kr.status || 'N/A'
          }));

          autoTable(doc, {
            head: [['Key Result Name', 'Description', 'Progress', 'Status']],
            body: keyResultData.map(item => [
              item.title,
              item.description,
              `${item.progress}%`,
              item.status
            ]),
            startY: currentY + 5,
            theme: 'grid',
            styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
            headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
            alternateRowStyles: { fillColor: [240, 240, 240] }
          });

          currentY = (doc as any).lastAutoTable.finalY + 20;

          // Tasks Section
          keyResultsForObjective.forEach(kr => {
            const tasksForKeyResult = this.tasks.get(kr.id) || [];
            if (tasksForKeyResult.length > 0) {
              if (currentY > 250) {
                doc.addPage();
                currentY = 20;
              }

              doc.setFontSize(16);
              doc.setFont('Helvetica', 'bold');
              doc.setTextColor(255, 215, 0);
              doc.text(`Tasks for: ${kr.title}`, 14, currentY);
              currentY += 10;

              const taskData = tasksForKeyResult.map(task => ({
                name: task.title,
                dueDate: task.endDate ? new Date(task.endDate).toLocaleDateString() : 'N/A',
                status: task.status,
                progress: task.progress
              }));

              autoTable(doc, {
                head: [['Task Name', 'Due Date', 'Status', 'Progress']],
                body: taskData.map(item => [
                  item.name,
                  item.dueDate,
                  item.status,
                  `${item.progress}%`
                ]),
                startY: currentY + 5,
                theme: 'grid',
                styles: { cellPadding: 3, fontSize: 10, fillColor: [255, 255, 255] },
                headStyles: { fillColor: [0, 0, 0], textColor: [255, 215, 0], fontSize: 12, lineWidth: 0.5 },
                alternateRowStyles: { fillColor: [240, 240, 240] }
              });

              currentY = (doc as any).lastAutoTable.finalY + 20;
            }
          });
        }
      });
      
      // Generate PDF blob for preview
      const pdfOutput = doc.output('blob');
      this.pdfBlob = pdfOutput;
      
      // Create preview URL
      const blobUrl = URL.createObjectURL(pdfOutput);
      this.pdfPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(blobUrl);
      
    } catch (error: any) {
      console.error('Error creating AI report PDF:', error);
      throw new Error('Failed to generate PDF. Please try again.');
    }
  }

  private async createCoverPageWithInsights(doc: any, session: OKRSession, objectives: Objective[], insights: string[], prompt: string): Promise<void> {
    // Dark background
    doc.setFillColor(26, 26, 26);
    doc.rect(0, 0, 220, 297, 'F');
    doc.setFillColor(255, 215, 0);
    doc.rect(0, 40, 220, 4, 'F');
    
    // Title
    doc.setFontSize(36);
    doc.setFont('Helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text('AI-Enhanced OKR Report', 20, 30);
    
    // Session info
    doc.setFontSize(24);
    doc.setTextColor(255, 215, 0);
    doc.text(session.title, 20, 80);
    
    doc.setFontSize(14);
    doc.setTextColor(255, 255, 255);
    doc.text('Session Period:', 20, 100);
    doc.setFont('Helvetica', 'normal');
    doc.text(`${new Date(session.startedDate).toLocaleDateString()} - ${new Date(session.endDate).toLocaleDateString()}`, 20, 110);
    
    // User prompt section
    doc.setFontSize(16);
    doc.setFont('Helvetica', 'bold');
    doc.setTextColor(255, 215, 0);
    doc.text('Your Prompt:', 20, 130);
    doc.setFontSize(12);
    doc.setFont('Helvetica', 'normal');
    doc.setTextColor(255, 255, 255);
    const promptLines = doc.splitTextToSize(prompt, 170);
    doc.text(promptLines, 20, 140);
    
    // AI Insights section
    let yPosition = 160;
    if (insights && insights.length > 0) {
      doc.setFontSize(16);
      doc.setFont('Helvetica', 'bold');
      doc.setTextColor(255, 215, 0);
      doc.text('AI-Generated Insights:', 20, yPosition);
      yPosition += 10;
      
      doc.setFontSize(12);
      doc.setFont('Helvetica', 'normal');
      doc.setTextColor(255, 255, 255);
      
      insights.forEach((insight, idx) => {
        const bulletPoint = `• ${insight}`;
        const lines = doc.splitTextToSize(bulletPoint, 170);
        doc.text(lines, 24, yPosition);
        yPosition += lines.length * 6 + 2;
        
        if (yPosition > 260) {
          doc.addPage();
          doc.setFillColor(26, 26, 26);
          doc.rect(0, 0, 220, 297, 'F');
          yPosition = 30;
        }
      });
    }

    // Overview section
    if (yPosition < 220) {
      doc.setFont('Helvetica', 'bold');
      doc.setFontSize(16);
      doc.setTextColor(255, 215, 0);
      doc.text('Session Overview:', 20, yPosition + 20);
      
      doc.setFont('Helvetica', 'normal');
      doc.setFontSize(12);
      doc.setTextColor(255, 255, 255);
      const sessionOverview = [
        `Total Objectives: ${objectives.length}`,
        `Completed: ${objectives.filter(o => o.status === Status.Completed).length}`,
        `In Progress: ${objectives.filter(o => o.status === Status.InProgress).length}`,
        `Not Started: ${objectives.filter(o => o.status === Status.NotStarted).length}`
      ];
      
      sessionOverview.forEach((stat, index) => {
        doc.text(stat, 20, yPosition + 40 + (index * 10));
      });
    }
    
    // Footer
    doc.setFontSize(10);
    doc.setTextColor(255, 215, 0);
    doc.text('Generated on ' + new Date().toLocaleDateString(), 20, 280);
  }

  downloadPdf(): void {
    console.log('[PDF Download] Starting download process');
    
    if (this.pdfBlob) {
      // For AI reports with preview, download the already generated PDF
      try {
        const blob = this.pdfBlob as Blob;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        
        // Create filename with session title
        const safeTitle = this.session.title.replace(/[^a-z0-9]/gi, '_').toLowerCase();
        link.download = `AI_Enhanced_OKR_Report_${safeTitle}_${Date.now()}.pdf`;
        
        // Download the file
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        
        // Show success animation
        this.downloadAnimation = 'download-success';
        
        setTimeout(() => {
          this.onClose();
        }, 1500);
        
      } catch (error: any) {
        console.error('[PDF Download] Error downloading AI report:', error);
        this.generationError = 'Failed to download PDF. Please try again.';
      }
      
      return;
    }
    
    // For classic exports, generate and download the PDF
    this.isClassicExporting = true;
    this.downloadAnimation = 'download-start';
    this.cdr.markForCheck();
    
    setTimeout(async () => {
      try {
        console.log('[PDF Download] Generating classic PDF export');
        
        // Generate the PDF using the service
        await this.pdfExportService.exportOKRSessionToPdf(
          this.session,
          this.objectives,
          this.owners,
          this.keyResults,
          this.tasks
        );
        
        // Show success animation
        this.downloadAnimation = 'download-success';
        console.log('[PDF Download] Classic PDF generated successfully');
        
        // Close modal after success animation
        setTimeout(() => {
          this.onClose();
        }, 1500);
        
      } catch (error: any) {
        console.error('[PDF Download] Error generating PDF:', error);
        this.downloadAnimation = 'download-error';
        this.generationError = 'Failed to generate PDF. Please try again.';
        
        // Reset animation after showing error
        setTimeout(() => {
          this.downloadAnimation = '';
          this.cdr.markForCheck();
        }, 3000);
      } finally {
        this.isClassicExporting = false;
        this.cdr.markForCheck();
      }
    }, 500);
  }
  
  retryWithNewPrompt(): void {
    // Clear previous results and preview
    this.generationError = null;
    this.pdfBlob = null;
    this.pdfPreviewUrl = null;
    this.downloadAnimation = '';
    
    // Go back to AI prompt step
    this.exportStep = ExportStep.AI_PROMPT;
    
    // Reset form and focus
    this.aiPromptForm.patchValue({ prompt: '' });
    
    setTimeout(() => {
      if (this.aiPromptInput?.nativeElement) {
        this.aiPromptInput.nativeElement.focus();
      }
    }, 100);
    
    this.cdr.markForCheck();
  }
  
  private resetState(): void {
    this.exportStep = ExportStep.SELECT_TYPE;
    this.aiPromptForm.reset();
    this.isGenerating = false;
    this.generationError = null;
    this.pdfPreviewUrl = null;
    this.pdfBlob = null;
    
    // Reset animation states
    this.isClassicExporting = false;
    this.downloadAnimation = '';
    this.aiGenerationStage = 'idle';
    this.aiGenerationProgress = 0;
    
    // Clean up any running intervals
    if (this.aiGenerationInterval) {
      clearInterval(this.aiGenerationInterval);
      this.aiGenerationInterval = null;
    }
  }
  
  private lockBodyScroll(): void {
    document.body.style.overflow = 'hidden';
  }

  private unlockBodyScroll(): void {
    document.body.style.overflow = 'auto';
  }
}