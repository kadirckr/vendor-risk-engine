// Mirrors the API contracts (responses are camelCased by ASP.NET Core).

export interface VendorSummary {
  id: number;
  name: string;
  financialHealth: number;
  slaUptime: number;
  majorIncidents: number;
  securityCerts: string[];
  riskScore: number;
  riskLevel: string;
  reason: string;
}

export interface ListVendorsResult {
  vendors: VendorSummary[];
}

/** null = not assessed, true = valid, false = invalid/failed (a risk). */
export type DocFlag = boolean | null;

export interface CreateVendorRequest {
  name: string;
  financialHealth: number;
  slaUptime: number;
  majorIncidents: number;
  securityCerts: string[];
  documents: {
    contractValid: DocFlag;
    privacyPolicyValid: DocFlag;
    pentestReportValid: DocFlag;
  };
}

export interface CreateVendorResult {
  id: number;
  name: string;
}

export interface RiskAssessment {
  vendorId: number;
  vendorName: string;
  riskScore: number;
  riskLevel: string;
  reason: string;
}
