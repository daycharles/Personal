import { ProjectData } from '../types';
import { partDefinitionMap } from '../parts/partDefinitions';

export const PcbView = ({ project }: { project: ProjectData }) => {
  return (
    <div className="pcb-view">
      <div className="pcb-grid">
        {project.parts.map((part) => {
          const footprint = partDefinitionMap[part.type]?.footprint;
          return (
            <div
              key={part.id}
              className="pcb-part"
              style={{
                left: part.position.x,
                top: part.position.y
              }}
            >
              <div className="pcb-part-label">{part.type}</div>
              <div className="pcb-pads">
                {footprint?.pads.map((pad, index) => (
                  <span
                    key={`${part.id}-${index}`}
                    className="pcb-pad"
                    style={{ left: pad.x, top: pad.y }}
                  />
                ))}
              </div>
            </div>
          );
        })}
        {project.wires.map((wire) => (
          <div key={wire.id} className="ratsnest">
            {wire.netId}
          </div>
        ))}
      </div>
      <div className="pcb-legend">Ratsnest lines represent net connectivity (manual routing optional).</div>
    </div>
  );
};
