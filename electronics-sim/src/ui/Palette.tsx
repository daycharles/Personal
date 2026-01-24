import { partDefinitions } from '../parts/partDefinitions';

export const Palette = ({ onAdd }: { onAdd: (type: string) => void }) => {
  const grouped = partDefinitions.reduce<Record<string, typeof partDefinitions>>((acc, part) => {
    acc[part.category] = acc[part.category] ?? [];
    acc[part.category].push(part);
    return acc;
  }, {});

  return (
    <div className="palette">
      <input className="palette-search" placeholder="Search parts" />
      {Object.entries(grouped).map(([category, parts]) => (
        <div key={category} className="palette-group">
          <div className="palette-category">{category}</div>
          {parts.map((part) => (
            <button key={part.type} className="palette-item" onClick={() => onAdd(part.type)}>
              {part.name}
            </button>
          ))}
        </div>
      ))}
    </div>
  );
};
