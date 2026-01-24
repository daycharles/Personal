export const SerialMonitor = ({ logs }: { logs: string[] }) => {
  return (
    <div className="serial-monitor">
      <div className="serial-header">Serial Monitor</div>
      <div className="serial-body">
        {logs.length === 0 ? <div className="serial-empty">No serial output yet.</div> : null}
        {logs.map((log, index) => (
          <div key={`${log}-${index}`}>{log}</div>
        ))}
      </div>
    </div>
  );
};
