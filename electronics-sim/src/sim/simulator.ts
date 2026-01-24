import { PartInstance, ProjectData, SimulationState, StepResult } from '../types';

const getPinNet = (part: PartInstance, pinId: string) =>
  part.pins.find((pin) => pin.pinId === pinId)?.netId;

export const createInitialState = (project: ProjectData): SimulationState => {
  const nets: SimulationState['nets'] = {};
  project.nets.forEach((net) => {
    nets[net.id] = { voltage: 0, current: 0 };
  });
  return { time: 0, nets, faults: [], messages: [], relayStates: {} };
};

const applyVoltageSources = (parts: PartInstance[], state: SimulationState) => {
  parts.forEach((part) => {
    if (part.type === 'voltage') {
      const positiveNet = getPinNet(part, 'p');
      const negativeNet = getPinNet(part, 'n');
      const voltage = Number(part.properties.voltage ?? 5);
      if (positiveNet && state.nets[positiveNet]) {
        state.nets[positiveNet].voltage = voltage;
      }
      if (negativeNet && state.nets[negativeNet]) {
        state.nets[negativeNet].voltage = 0;
      }
    }
    if (part.type === 'ground') {
      const groundNet = getPinNet(part, 'g');
      if (groundNet && state.nets[groundNet]) {
        state.nets[groundNet].voltage = 0;
      }
    }
  });
};

const updateRelayState = (part: PartInstance, state: SimulationState) => {
  const coilPlus = getPinNet(part, 'coil+');
  const coilMinus = getPinNet(part, 'coil-');
  if (!coilPlus || !coilMinus) return;
  const coilVoltage = (state.nets[coilPlus]?.voltage ?? 0) - (state.nets[coilMinus]?.voltage ?? 0);
  const threshold = Number(part.properties.coilVoltage ?? 5) * 0.7;
  state.relayStates[part.id] = coilVoltage >= threshold;
};

const detectFaults = (parts: PartInstance[], state: SimulationState) => {
  state.faults = [];
  parts.forEach((part) => {
    if (part.type === 'voltage') {
      const positiveNet = getPinNet(part, 'p');
      const negativeNet = getPinNet(part, 'n');
      if (positiveNet && negativeNet && positiveNet === negativeNet) {
        state.faults.push(`Short across source ${part.id}`);
      }
    }
  });
};

export const stepSimulation = (project: ProjectData, previous: SimulationState, deltaTime: number): StepResult => {
  const state: SimulationState = {
    ...previous,
    time: previous.time + deltaTime,
    nets: { ...previous.nets },
    relayStates: { ...previous.relayStates },
    faults: [],
    messages: []
  };

  applyVoltageSources(project.parts, state);

  project.parts
    .filter((part) => part.type === 'relay')
    .forEach((part) => updateRelayState(part, state));

  detectFaults(project.parts, state);

  return { state, deltaTime };
};
