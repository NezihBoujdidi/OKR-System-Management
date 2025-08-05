export interface Plan {
    id: string;
    planId: string;
    name: string;
    planType: string;
    description: string;
    price: number;
    interval: string;
    features: string[];
    isActive?: boolean;
    
    // Frontend-specific fields (for backward compatibility)
    recommended?: boolean;
    buttonText?: string;
    period?: string; // Alias for interval, for backward compatibility
}