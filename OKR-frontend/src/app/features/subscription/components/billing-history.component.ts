import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { BillingHistoryItem, SubscriptionService } from 'src/app/services/subscription.service';
import { catchError, finalize, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { DatePipe } from '@angular/common';
import { PdfExportService } from 'src/app/services/pdf-export.service';

@Component({
  selector: 'app-billing-history',
  templateUrl: './billing-history.component.html',
  providers: [DatePipe]
})
export class BillingHistoryComponent implements OnInit {
  billingHistory: BillingHistoryItem[] = [];
  isLoading = false;
  error: string | null = null;
  isExporting = false;

  constructor(
    private subscriptionService: SubscriptionService,
    private datePipe: DatePipe,
    private cdr: ChangeDetectorRef,
    private pdfExportService: PdfExportService
  ) { }

  ngOnInit(): void {
    console.log('BillingHistoryComponent initialized');
    // Set a short timeout to ensure component is fully initialized
    setTimeout(() => {
      this.loadBillingHistory();
    }, 0);
  }

  loadBillingHistory(): void {
    console.log('Starting to load billing history, setting isLoading = true');
    this.isLoading = true;
    this.error = null;
    this.cdr.detectChanges();

    this.subscriptionService.getBillingHistory()
      .pipe(
        tap(data => {
          console.log('Data received, length:', data?.length);
        }),
        catchError(err => {
          console.error('Error loading billing history:', err);
          this.error = 'Failed to load billing history. Please try again later.';
          this.isLoading = false;
          this.cdr.detectChanges();
          return of([]);
        })
      )
      .subscribe({
        next: (data) => {
          console.log('Processing billing history data');
          
          if (!data || !Array.isArray(data)) {
            console.error('Invalid billing history data received');
            this.billingHistory = [];
          } else {
            // Ensure dates are properly parsed
            this.billingHistory = data.map(item => ({
              ...item,
              paidAt: this.parseDate(item.paidAt),
              description: item.description || 'Subscription Payment',
              currency: item.currency || 'usd',
              status: item.status || 'unknown'
            }));
          }
          
          // Explicitly set loading to false and trigger change detection
          this.isLoading = false;
          console.log('Setting isLoading = false after data processing');
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Subscription error:', err);
          this.error = 'Error loading data';
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        complete: () => {
          console.log('Request completed, ensuring isLoading = false');
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  // Helper to handle different date formats
  private parseDate(dateValue: any): Date {
    if (!dateValue) return new Date();
    
    try {
      // If it's already a Date
      if (dateValue instanceof Date) return dateValue;
      
      // If it's a string, try to parse it
      if (typeof dateValue === 'string') {
        // Try ISO format first
        const date = new Date(dateValue);
        if (!isNaN(date.getTime())) return date;
      }
      
      // If it's a number (timestamp)
      if (typeof dateValue === 'number') {
        return new Date(dateValue);
      }
      
      // Default fallback
      return new Date();
    } catch (e) {
      console.error('Error parsing date:', e, dateValue);
      return new Date();
    }
  }

  formatCurrency(amount: number, currency: string): string {
    if (amount === undefined || amount === null) {
      return '$0.00';
    }
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: (currency || 'USD').toUpperCase()
    }).format(amount);
  }

  getStatusClass(status: string): string {
    if (!status) return 'bg-gray-100 text-gray-800';
    
    switch (status.toLowerCase()) {
      case 'paid':
        return 'bg-green-100 text-green-800';
      case 'unpaid':
        return 'bg-amber-100 text-amber-800';
      case 'failed':
        return 'bg-red-100 text-red-800';
      case 'draft':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }
  
  formatDate(date: Date): string {
    if (!date) return '';
    try {
      return this.datePipe.transform(date, 'medium') || '';
    } catch (e) {
      console.error('Error formatting date:', e, date);
      return date.toString();
    }
  }

  async exportBillingHistory(): Promise<void> {
    if (this.isLoading || this.billingHistory.length === 0) {
      console.log('Cannot export: data is loading or empty');
      return;
    }
    
    try {
      this.isExporting = true;
      this.cdr.detectChanges();
      
      console.log('Exporting billing history to PDF');
      await this.pdfExportService.exportBillingHistoryToPdf(this.billingHistory);
      console.log('PDF export completed');
    } catch (error) {
      console.error('Error exporting billing history:', error);
    } finally {
      this.isExporting = false;
      this.cdr.detectChanges();
    }
  }
} 