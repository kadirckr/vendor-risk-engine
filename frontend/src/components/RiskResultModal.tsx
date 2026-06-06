import type { RiskAssessment } from '../types';
import { RiskLevelBadge } from './RiskLevelBadge';

interface Props {
  result: RiskAssessment;
  onClose: () => void;
}

export function RiskResultModal({ result, onClose }: Props) {
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-head">
          <h3>Risk Result</h3>
          <button className="modal-close" onClick={onClose} aria-label="Close">×</button>
        </div>

        <dl className="result">
          <dt>Name</dt>
          <dd>{result.vendorName}</dd>

          <dt>Risk Score</dt>
          <dd className="score">{result.riskScore.toFixed(2)}</dd>

          <dt>Risk Level</dt>
          <dd><RiskLevelBadge level={result.riskLevel} /></dd>

          <dt>Reason</dt>
          <dd>{result.reason}</dd>
        </dl>

        <button className="btn" onClick={onClose}>OK</button>
      </div>
    </div>
  );
}
