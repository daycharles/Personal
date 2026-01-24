import { LearnEntry } from '../types';

export const TooltipCard = ({
  entry,
  learningMode,
  voltages,
  category,
  pinoutNotes,
  footprint
}: {
  entry: LearnEntry;
  learningMode: boolean;
  voltages?: { pin: string; voltage: number }[];
  category?: string;
  pinoutNotes?: string[];
  footprint?: string;
}) => {
  return (
    <div className="tooltip-card">
      <div className="tooltip-header">
        <div>
          <strong>{entry.title}</strong>
          <span className="tooltip-category">{category ?? 'component'}</span>
        </div>
        <span className="tooltip-category">{learningMode ? 'Learning Mode' : 'Quick View'}</span>
      </div>
      <p>{entry.summary}</p>
      <div className="tooltip-symbols">
        <div>
          <div className="tooltip-symbol-label">US</div>
          <div dangerouslySetInnerHTML={{ __html: entry.symbolUS }} />
        </div>
        <div>
          <div className="tooltip-symbol-label">IEC</div>
          <div dangerouslySetInnerHTML={{ __html: entry.symbolIEC }} />
        </div>
      </div>
      <div className="tooltip-section">
        <strong>Common uses</strong>
        <ul>
          {(learningMode ? entry.typicalUses : entry.typicalUses.slice(0, 2)).map((use) => (
            <li key={use}>{use}</li>
          ))}
        </ul>
      </div>
      <div className="tooltip-section">
        <strong>Gotchas</strong>
        <ul>
          {(learningMode ? entry.gotchas : entry.gotchas.slice(0, 3)).map((gotcha) => (
            <li key={gotcha}>{gotcha}</li>
          ))}
        </ul>
      </div>
      {learningMode && (
        <div className="tooltip-section">
          <strong>Key specs</strong>
          <ul>
            {entry.parameterGlossary.slice(0, 2).map((param) => (
              <li key={param.name}>
                {param.name}: {param.meaning} ({param.typicalRange})
              </li>
            ))}
          </ul>
        </div>
      )}
      {learningMode && pinoutNotes ? (
        <div className="tooltip-section">
          <strong>Pin naming</strong>
          <ul>
            {pinoutNotes.slice(0, 2).map((note) => (
              <li key={note}>{note}</li>
            ))}
          </ul>
        </div>
      ) : null}
      {learningMode && footprint ? (
        <div className="tooltip-section">
          <strong>Common footprint</strong>
          <div>{footprint}</div>
        </div>
      ) : null}
      {voltages && voltages.length > 0 ? (
        <div className="tooltip-section">
          <strong>Node voltages</strong>
          <ul>
            {voltages.map((entry) => (
              <li key={entry.pin}>
                {entry.pin}: {entry.voltage.toFixed(2)}V
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
};
