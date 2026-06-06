import { useState } from 'react';
import { createVendor, getVendorRisk } from '../api';
import type { CreateVendorRequest, DocFlag, RiskAssessment } from '../types';

interface Props {
  onResult: (result: RiskAssessment) => void;
  onCreated: () => void;
}

const CERT_OPTIONS = ['ISO27001', 'SOC2', 'PCI-DSS'];

type Errors = Partial<Record<'name' | 'financialHealth' | 'slaUptime' | 'majorIncidents', string>>;

function toFlag(v: string): DocFlag {
  if (v === 'true') return true;
  if (v === 'false') return false;
  return null;
}

export function RiskQueryForm({ onResult, onCreated }: Props) {
  const [name, setName] = useState('');
  const [financialHealth, setFinancialHealth] = useState('');
  const [slaUptime, setSlaUptime] = useState('');
  const [majorIncidents, setMajorIncidents] = useState('');
  const [certs, setCerts] = useState<string[]>([]);
  const [contractValid, setContractValid] = useState('');
  const [privacyPolicyValid, setPrivacyPolicyValid] = useState('');
  const [pentestReportValid, setPentestReportValid] = useState('');

  const [errors, setErrors] = useState<Errors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  function validate(): Errors {
    const e: Errors = {};

    if (!name.trim()) e.name = 'Vendor name is required.';

    const intIn = (raw: string, min: number, max: number) => {
      if (raw.trim() === '') return 'Required field.';
      const n = Number(raw);
      if (!Number.isInteger(n)) return 'Enter an integer.';
      if (n < min || n > max) return `Must be between ${min} and ${max}.`;
      return undefined;
    };

    const fh = intIn(financialHealth, 0, 100);
    if (fh) e.financialHealth = fh;
    const sla = intIn(slaUptime, 0, 100);
    if (sla) e.slaUptime = sla;
    const mi = intIn(majorIncidents, 0, 1000);
    if (mi) e.majorIncidents = mi;

    return e;
  }

  function toggleCert(cert: string) {
    setCerts((prev) =>
      prev.includes(cert) ? prev.filter((c) => c !== cert) : [...prev, cert],
    );
  }

  async function handleSubmit(ev: React.FormEvent) {
    ev.preventDefault();
    setSubmitError(null);

    const e = validate();
    setErrors(e);
    if (Object.keys(e).length > 0) return;

    const request: CreateVendorRequest = {
      name: name.trim(),
      financialHealth: Number(financialHealth),
      slaUptime: Number(slaUptime),
      majorIncidents: Number(majorIncidents),
      securityCerts: certs,
      documents: {
        contractValid: toFlag(contractValid),
        privacyPolicyValid: toFlag(privacyPolicyValid),
        pentestReportValid: toFlag(pentestReportValid),
      },
    };

    try {
      setSubmitting(true);
      // 1) persist the vendor → 2) fetch its computed risk by the returned id
      const created = await createVendor(request);
      const assessment = await getVendorRisk(created.id);
      onResult(assessment);
      onCreated();
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Unexpected error.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="card">
      <div className="card-head">
        <h2>Risk Query</h2>
      </div>

      <form className="form" onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label>Vendor Name</label>
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="TechPlus Solutions" />
          {errors.name && <span className="field-error">{errors.name}</span>}
        </div>

        <div className="grid">
          <div className="field">
            <label>Financial Health (0–100)</label>
            <input type="number" value={financialHealth} onChange={(e) => setFinancialHealth(e.target.value)} />
            {errors.financialHealth && <span className="field-error">{errors.financialHealth}</span>}
          </div>
          <div className="field">
            <label>SLA Uptime % (0–100)</label>
            <input type="number" value={slaUptime} onChange={(e) => setSlaUptime(e.target.value)} />
            {errors.slaUptime && <span className="field-error">{errors.slaUptime}</span>}
          </div>
          <div className="field">
            <label>Major Incidents (last 12 months)</label>
            <input type="number" value={majorIncidents} onChange={(e) => setMajorIncidents(e.target.value)} />
            {errors.majorIncidents && <span className="field-error">{errors.majorIncidents}</span>}
          </div>
        </div>

        <div className="field">
          <label>Security Certs</label>
          <div className="checks">
            {CERT_OPTIONS.map((cert) => (
              <label key={cert} className="check">
                <input type="checkbox" checked={certs.includes(cert)} onChange={() => toggleCert(cert)} />
                {cert}
              </label>
            ))}
          </div>
        </div>

        <div className="grid">
          <div className="field">
            <label>Contract</label>
            <select value={contractValid} onChange={(e) => setContractValid(e.target.value)}>
              <option value="">Not specified</option>
              <option value="true">Valid</option>
              <option value="false">Invalid</option>
            </select>
          </div>
          <div className="field">
            <label>Privacy Policy</label>
            <select value={privacyPolicyValid} onChange={(e) => setPrivacyPolicyValid(e.target.value)}>
              <option value="">Not specified</option>
              <option value="true">Valid</option>
              <option value="false">Invalid</option>
            </select>
          </div>
          <div className="field">
            <label>Pentest Report</label>
            <select value={pentestReportValid} onChange={(e) => setPentestReportValid(e.target.value)}>
              <option value="">Not specified</option>
              <option value="true">Valid</option>
              <option value="false">Invalid</option>
            </select>
          </div>
        </div>

        {submitError && <p className="error-banner">{submitError}</p>}

        <button className="btn" type="submit" disabled={submitting}>
          {submitting ? 'Querying…' : 'Run Query'}
        </button>
      </form>
    </div>
  );
}
