export const HintBanner = ({ hints }: { hints: string[] }) => {
  if (hints.length === 0) return null;
  return (
    <div className="hint-banner">
      <strong>Learning hints:</strong>
      <ul>
        {hints.map((hint) => (
          <li key={hint}>{hint}</li>
        ))}
      </ul>
    </div>
  );
};
