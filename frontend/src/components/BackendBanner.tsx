import { useState, useEffect, useCallback } from 'react';

export default function BackendBanner() {
  const [offline, setOffline] = useState(false);
  const [checking, setChecking] = useState(false);

  const check = useCallback(async () => {
    setChecking(true);
    try {
      const res = await fetch('/health', { signal: AbortSignal.timeout(4000) });
      setOffline(!res.ok);
    } catch {
      setOffline(true);
    } finally {
      setChecking(false);
    }
  }, []);

  useEffect(() => { check(); }, [check]);

  if (!offline) return null;

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, zIndex: 999,
      background: 'color-mix(in srgb, var(--red) 18%, transparent)',
      borderBottom: '1px solid color-mix(in srgb, var(--red) 40%, transparent)',
      color: 'var(--red)',
      padding: '10px 20px',
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      fontSize: 13,
      fontWeight: 500,
    }}>
      <span>⚠ Backend offline — start the .NET API on <code style={{ fontFamily: 'monospace' }}>localhost:1731</code></span>
      <button
        className="btn btn-danger btn-sm"
        style={{ marginLeft: 'auto' }}
        onClick={check}
        disabled={checking}
      >
        {checking ? 'Checking…' : '↻ Retry'}
      </button>
    </div>
  );
}
