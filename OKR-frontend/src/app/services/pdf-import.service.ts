import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { OKRSession } from '../models/okr-session.interface';
import { Objective } from '../models/objective.interface';
import { KeyResult } from '../models/key-result.interface';
import { KeyResultTask } from '../models/key-result-task.interface';

@Injectable({
  providedIn: 'root'
})
export class PdfImportService {
  private importProgressSubject = new BehaviorSubject<number>(0);
  importProgress$ = this.importProgressSubject.asObservable();
  
  private statusMessageSubject = new BehaviorSubject<string>('');
  statusMessage$ = this.statusMessageSubject.asObservable();

  constructor() {}

  async importPdf(file: File): Promise<{ session: Partial<OKRSession>, objectives: Partial<Objective>[] }> {
    try {
      this.updateStatus('Initializing...', 0);
      await this.delay(500);

      const { getDocument, GlobalWorkerOptions, version } = await import('pdfjs-dist');
      GlobalWorkerOptions.workerSrc = `//cdnjs.cloudflare.com/ajax/libs/pdf.js/${version}/pdf.worker.min.js`;

      this.updateStatus('Reading PDF file...', 10);
      await this.delay(500);
      
      const arrayBuffer = await file.arrayBuffer();
      const pdf = await getDocument(arrayBuffer).promise;
      
      this.updateStatus('Extracting text...', 30);
      
      // Extract text from all pages with proper spacing
      let fullText = '';
      for (let i = 1; i <= pdf.numPages; i++) {
        const page = await pdf.getPage(i);
        const textContent = await page.getTextContent();
        
        // Join text items with proper spacing
        let lastY: number | null = null;
        let text = '';
        textContent.items.forEach((item: any) => {
          // Check if item is a text item (not a marked content item)
          if ('str' in item && 'transform' in item) {
            if (lastY === item.transform[5] || lastY === null) {
              text += item.str;
            } else {
              text += '\n' + item.str;
            }
            lastY = item.transform[5];
          }
        });
        
        fullText += text + '\n';
        
        this.updateStatus(`Processing page ${i}...`, 30 + (40 * (i / pdf.numPages)));
      }

      console.log('Extracted full text:', fullText);

      this.updateStatus('Analyzing content...', 80);
      await this.delay(500);
      
      const { session, objectives } = this.extractOKRData(fullText);
      
      this.updateStatus('Complete!', 100);
      await this.delay(500);
      
      return { session, objectives };
    } catch (error) {
      console.error('Error importing PDF:', error);
      this.statusMessageSubject.next('Error importing PDF');
      throw new Error('Failed to process PDF file. Please make sure it\'s a valid PDF document.');
    }
  }

  private updateStatus(message: string, progress: number) {
    this.statusMessageSubject.next(message);
    this.importProgressSubject.next(progress);
  }

  private extractOKRData(text: string): { 
    session: Partial<OKRSession>, 
    objectives: Partial<Objective>[] 
  } {
    const today = new Date();
    const sessionMatch = text.match(/Session Title:\s*(.*?)(?=\n|$)/);
    const sessionTitle = sessionMatch ? sessionMatch[1].trim() : 'Imported Session';
    
    const session = this.createDefaultSession(sessionTitle);
    const objectives: Partial<Objective>[] = [];

    const objMatches = text.match(/Objective \d+:[\s\S]*?(?=Objective \d+:|$)/g) || [];
    
    objMatches.forEach(objText => {
      const objTitleMatch = objText.match(/Objective \d+:\s*(.*?)(?=Key Result|$)/);
      const objTitle = objTitleMatch ? objTitleMatch[1].trim() : '';
      
      if (objTitle) {
        const objective: Partial<Objective> = {
          name: objTitle,
          startDate: today,
          endDate: new Date(today.getTime() + 90 * 24 * 60 * 60 * 1000), // 90 days from now
          progress: 0,
          isDeleted: false,
          keyResults: [] as KeyResult[] // Initialize as empty array of KeyResult
        };

        const krMatches = objText.match(/Key Result \d+:[\s\S]*?(?=Key Result \d+:|$)/g) || [];
        
        krMatches.forEach(krText => {
          const krTitleMatch = krText.match(/Key Result \d+:\s*(.*?)(?=Task|$)/);
          const krTitle = krTitleMatch ? krTitleMatch[1].trim() : '';
          
          if (krTitle) {
            const keyResult: KeyResult = {  // Create as full KeyResult, not Partial
              id: Date.now().toString(),
              objectiveId: '', // Will be set when Objective is created
              name: krTitle,
              userId: '', // Will be set when creating
              startDate: today,
              endDate: new Date(today.getTime() + 90 * 24 * 60 * 60 * 1000),
              progress: 0,
              isDeleted: false,
              keyResultTasks: [] // Initialize as empty array
            };

            const taskMatches = krText.match(/Task \d+:[\s\S]*?(?=Task \d+:|$)/g) || [];
            
            taskMatches.forEach(taskText => {
              const taskTitleMatch = taskText.match(/Task \d+:\s*(.*?)(?=Task|$)/);
              const taskName = taskTitleMatch ? taskTitleMatch[1].trim() : '';
              
              if (taskName) {
                const task: KeyResultTask = {
                  id: Date.now().toString(),
                  name: taskName,
                  keyResultId: keyResult.id,  // Now we can use keyResult.id
                  userId: '',  // Will be set when creating
                  collaboratorId: '',  // Will be set when creating
                  description: '',
                  startDate: today,
                  dueDate: new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000),
                  status: 'not-started',
                  priority: 'medium',
                  progress: 0,
                  lastActivityDate: today,
                  createdDate: today,
                  isDeleted: false
                };
                keyResult.keyResultTasks.push(task);
              }
            });

            objective.keyResults?.push(keyResult);
          }
        });

        objectives.push(objective);
      }
    });

    return { session, objectives };
  }

  private createDefaultSession(title: string): Partial<OKRSession> {
    const today = new Date();
    return {
      name: title,
      startDate: today,
      endDate: new Date(today.getTime() + 90 * 24 * 60 * 60 * 1000),
      status: 'DRAFT'
    };
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}