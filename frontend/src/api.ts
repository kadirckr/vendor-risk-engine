import type {
  CreateVendorRequest,
  CreateVendorResult,
  ListVendorsResult,
  RiskAssessment,
  VendorSummary,
} from './types';

// Relative path → Vite dev server proxies "/api" to the backend (see vite.config.ts).
const BASE = '/api/vendor';

async function parse<T>(res: Response): Promise<T> {
  if (res.ok) {
    return (await res.json()) as T;
  }

  // Surface validation/problem details in a readable way.
  let message = `Request failed (HTTP ${res.status})`;
  try {
    const body = await res.json();
    if (body?.errors) {
      message = Object.values(body.errors as Record<string, string[]>)
        .flat()
        .join(' ');
    } else if (body?.detail || body?.title) {
      message = body.detail ?? body.title;
    }
  } catch {
    // non-JSON error body — keep the default message
  }
  throw new Error(message);
}

export async function listVendors(): Promise<VendorSummary[]> {
  const res = await fetch(BASE);
  const data = await parse<ListVendorsResult>(res);
  return data.vendors;
}

export async function createVendor(req: CreateVendorRequest): Promise<CreateVendorResult> {
  const res = await fetch(BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  return parse<CreateVendorResult>(res);
}

export async function getVendorRisk(id: number): Promise<RiskAssessment> {
  const res = await fetch(`${BASE}/${id}/risk`);
  return parse<RiskAssessment>(res);
}
