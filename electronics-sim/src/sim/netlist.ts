import { PartInstance, ProjectData } from '../types';

export type Netlist = {
  nets: Record<string, { id: string; name: string; connections: string[] }>;
  parts: PartInstance[];
};

export const buildNetlist = (project: ProjectData): Netlist => {
  const nets: Netlist['nets'] = {};
  project.nets.forEach((net) => {
    nets[net.id] = { id: net.id, name: net.name, connections: [] };
  });

  project.parts.forEach((part) => {
    part.pins.forEach((pin) => {
      const net = nets[pin.netId];
      if (net) {
        net.connections.push(`${part.id}:${pin.pinId}`);
      }
    });
  });

  return { nets, parts: project.parts };
};

export const hasNetConnection = (netlist: Netlist, netId: string, partId: string, pinId: string) => {
  const net = netlist.nets[netId];
  if (!net) return false;
  return net.connections.includes(`${partId}:${pinId}`);
};
