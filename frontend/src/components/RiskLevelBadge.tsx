const COLORS: Record<string, string> = {
  Low: '#16a34a',
  Medium: '#ca8a04',
  High: '#ea580c',
  Critical: '#dc2626',
};

export function RiskLevelBadge({ level }: { level: string }) {
  const color = COLORS[level] ?? '#6b7280';
  return (
    <span className="badge" style={{ backgroundColor: color }}>
      {level}
    </span>
  );
}
