import { PartInstance, ProjectData, SimulationState } from '../types';

const getPinNet = (part: PartInstance, pinId: string) =>
  part.pins.find((pin) => pin.pinId === pinId)?.netId;

const diodeAcross = (diode: PartInstance, netA: string, netB: string) => {
  const a = getPinNet(diode, 'a');
  const k = getPinNet(diode, 'k');
  return (a === netA && k === netB) || (a === netB && k === netA);
};

export const evaluateHintRules = (project: ProjectData, state: SimulationState, learningMode: boolean) => {
  if (!learningMode) return [] as string[];
  const hints: string[] = [];

  const relay = project.parts.find((part) => part.type === 'relay');
  if (relay) {
    const coilPlus = getPinNet(relay, 'coil+');
    const coilMinus = getPinNet(relay, 'coil-');
    if (coilPlus && coilMinus) {
      const diode = project.parts.find((part) => part.type === 'diode' && diodeAcross(part, coilPlus, coilMinus));
      if (!diode) {
        hints.push('Add a flyback diode across the relay coil to protect the transistor/MCU.');
      }
    }
  }

  const npn = project.parts.find((part) => part.type === 'npn');
  if (npn) {
    const baseNet = getPinNet(npn, 'b');
    if (baseNet) {
      const hasResistor = project.parts.some((part) =>
        part.type === 'resistor' && part.pins.some((pin) => pin.netId === baseNet)
      );
      if (!hasResistor) {
        hints.push('Add a base resistor to limit current into the NPN transistor.');
      }
    }
  }

  if (relay && state.relayStates[relay.id]) {
    hints.push('Relay coil energized: verify the motor load is connected through COM and NO contacts.');
  }

  return hints;
};
