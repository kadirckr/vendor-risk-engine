import { useCallback, useEffect, useState } from 'react';
import './App.css';
import { listVendors } from './api';
import type { RiskAssessment, VendorSummary } from './types';
import { VendorList } from './components/VendorList';
import { RiskQueryForm } from './components/RiskQueryForm';
import { RiskResultModal } from './components/RiskResultModal';

type Tab = 'list' | 'query';

function App() {
  const [tab, setTab] = useState<Tab>('list');
  const [vendors, setVendors] = useState<VendorSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modalResult, setModalResult] = useState<RiskAssessment | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setVendors(await listVendors());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load the list.');
    } finally {
      setLoading(false);
    }
  }, []);

  // Load the already-computed values on startup.
  useEffect(() => {
    void refresh();
  }, [refresh]);

  return (
    <div className="app">
      <header className="app-header">
        <h1>Vendor Risk Scoring</h1>
        <nav className="tabs">
          <button className={tab === 'list' ? 'tab active' : 'tab'} onClick={() => setTab('list')}>
            List
          </button>
          <button className={tab === 'query' ? 'tab active' : 'tab'} onClick={() => setTab('query')}>
            Risk Query
          </button>
        </nav>
      </header>

      <main className="app-main">
        {tab === 'list' ? (
          <VendorList vendors={vendors} loading={loading} error={error} onRefresh={refresh} />
        ) : (
          <RiskQueryForm onResult={setModalResult} onCreated={refresh} />
        )}
      </main>

      {modalResult && <RiskResultModal result={modalResult} onClose={() => setModalResult(null)} />}
    </div>
  );
}

export default App;
