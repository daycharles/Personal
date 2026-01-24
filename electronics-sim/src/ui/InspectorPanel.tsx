import { useState } from 'react';
import { LearnEntry, PartInstance } from '../types';

export const InspectorPanel = ({ part, entry }: { part: PartInstance | null; entry: LearnEntry | null }) => {
  const [tab, setTab] = useState<'inspect' | 'learn'>('inspect');

  if (!part || !entry) {
    return (
      <div className="inspector-panel">
        <h3>Inspector + Learn</h3>
        <p>Select a component to see details.</p>
      </div>
    );
  }

  return (
    <div className="inspector-panel">
      <h3>Inspector + Learn</h3>
      <div className="inspector-tabs">
        <button className={tab === 'inspect' ? 'active' : ''} onClick={() => setTab('inspect')}>
          Inspector
        </button>
        <button className={tab === 'learn' ? 'active' : ''} onClick={() => setTab('learn')}>
          Learn
        </button>
      </div>
      {tab === 'inspect' ? (
        <div className="inspector-content">
          <h4>{entry.title}</h4>
          <p>{entry.summary}</p>
          <div className="inspector-section">
            <strong>Pinout notes</strong>
            <ul>
              {entry.pinoutNotes.map((note) => (
                <li key={note}>{note}</li>
              ))}
            </ul>
          </div>
          <div className="inspector-section">
            <strong>Parameters</strong>
            <ul>
              {entry.parameterGlossary.map((param) => (
                <li key={param.name}>
                  {param.name}: {param.meaning} ({param.typicalRange})
                </li>
              ))}
            </ul>
          </div>
        </div>
      ) : (
        <div className="inspector-content">
          <h4>{entry.title}</h4>
          <p>{entry.detailedExplanation}</p>
          <div className="inspector-section">
            <strong>Typical Uses</strong>
            <ul>
              {entry.typicalUses.map((use) => (
                <li key={use}>{use}</li>
              ))}
            </ul>
          </div>
          <div className="inspector-section">
            <strong>Gotchas</strong>
            <ul>
              {entry.gotchas.map((gotcha) => (
                <li key={gotcha}>{gotcha}</li>
              ))}
            </ul>
          </div>
          {entry.relatedParts.length > 0 ? (
            <div className="inspector-section">
              <strong>Related lessons</strong>
              <ul>
                {entry.relatedParts.map((related) => (
                  <li key={related}>Learn about: {related}</li>
                ))}
              </ul>
            </div>
          ) : null}
          <div className="inspector-symbols">
            <div>
              <div className="tooltip-symbol-label">US Symbol</div>
              <div dangerouslySetInnerHTML={{ __html: entry.symbolUS }} />
            </div>
            <div>
              <div className="tooltip-symbol-label">IEC Symbol</div>
              <div dangerouslySetInnerHTML={{ __html: entry.symbolIEC }} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
