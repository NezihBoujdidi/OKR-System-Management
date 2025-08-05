/**
 * Simplified polyfill for Navigator.locks API
 */

export function applyLocksPolyfill(): void {
  // Only apply if navigator.locks is not supported
  if (!('locks' in navigator)) {
    console.debug('Navigator.locks API not supported, applying simple polyfill');
    
    // Provide minimal implementation
    (navigator as any).locks = {
      request: async (name: string, options: any = {}, callback?: Function): Promise<any> => {
        // Handle different argument patterns
        if (typeof options === 'function') {
          callback = options;
          options = {};
        }
        
        try {
          // Simply execute callback without actual lock management
          return callback ? await Promise.resolve(callback()) : null;
        } catch (e) {
          console.error('Error in locks polyfill', e);
          throw e;
        }
      },
      
      query: async (): Promise<{held: any[], pending: any[]}> => {
        return { held: [], pending: [] };
      }
    };
  }
} 