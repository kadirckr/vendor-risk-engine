import type { VendorSummary } from '../types';
import { RiskLevelBadge } from './RiskLevelBadge';

interface Props {
  vendors: VendorSummary[];
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}

export function VendorList({ vendors, loading, error, onRefresh }: Props) {
  return (
    <div className="card">
      <div className="card-head">
        <h2>Vendor Risk Scores</h2>
        <button className="btn-ghost" onClick={onRefresh} disabled={loading}>
          {loading ? 'Loading…' : 'Refresh'}
        </button>
      </div>

      {error && <p className="error-banner">{error}</p>}

      {!error && (
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th className="num">Risk Score</th>
              <th>Risk Level</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>
            {vendors.length === 0 && !loading && (
              <tr>
                <td colSpan={4} className="empty">No records.</td>
              </tr>
            )}
            {vendors.map((v) => (
              <tr key={v.id}>
                <td className="name">{v.name}</td>
                <td className="num">{v.riskScore.toFixed(2)}</td>
                <td><RiskLevelBadge level={v.riskLevel} /></td>
                <td className="reason">{v.reason}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
