import { NgModule, ErrorHandler, Injectable } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CommonModule } from '@angular/common';
import { CoreModule } from './core/core.module';
import { SharedModule } from './shared/shared.module';
import { LayoutsModule } from './layouts/layouts.module';
import { HomeModule } from './features/home/home.module';
import { LoadingOverlayModule } from './shared/loading-overlay/loading-overlay.module';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ToastComponent } from './shared/toast/toast.component';
import { OKRSessionService } from './services/okr-session.service';
import { ChatModule } from './components/chat/chat.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { UsersModule } from '@features/users/users.module';
import { OkrSessionGuard } from './guards/okr-session.guard';
import { TeamsModule } from './features/teams/teams.module';
import { MeetingsModule } from './features/meetings/meetings.module';

// Custom error handler to suppress LockManager errors
@Injectable()
export class CustomErrorHandler implements ErrorHandler {
  handleError(error: any): void {
    // Check if error is related to Navigator LockManager
    if (this.isLockManagerError(error)) {
      // Silently suppress LockManager errors to keep console clean
      return;
    }
    
    // Suppress font loading errors which are related to Content Security Policy
    if (this.isFontLoadingError(error)) {
      return;
    }
    
    // For all other errors, log to console
    console.error('Error occurred:', error);
  }
  
  private isLockManagerError(error: any): boolean {
    if (!error) return false;
    
    // Check various error formats
    const errorString = String(error);
    const errorMessage = error.message ? String(error.message) : '';
    const errorStack = error.stack ? String(error.stack) : '';
    
    const lockTerms = ['LockManager', 'lock "lock:sb-'];
    
    return lockTerms.some(term => 
      errorString.includes(term) || 
      errorMessage.includes(term) || 
      errorStack.includes(term)
    );
  }
  
  private isFontLoadingError(error: any): boolean {
    if (!error) return false;
    
    const errorString = String(error);
    const errorMessage = error.message ? String(error.message) : '';
    
    return errorString.includes('font') || 
           errorMessage.includes('font') ||
           errorString.includes('Content Security Policy') ||
           errorMessage.includes('Content Security Policy');
  }
}

@NgModule({
  declarations: [
    AppComponent,
    ToastComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    RouterModule,
    HttpClientModule,
    CoreModule,
    SharedModule,
    LayoutsModule,
    HomeModule,
    LoadingOverlayModule,
    ChatModule,
    UsersModule,
    TeamsModule,
    MeetingsModule
  ],
  providers: [
    OKRSessionService,
    OkrSessionGuard,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    {
      provide: ErrorHandler,
      useClass: CustomErrorHandler
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
