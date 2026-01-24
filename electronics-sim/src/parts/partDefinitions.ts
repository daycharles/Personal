import { PartDefinition } from '../types';

export const partDefinitions: PartDefinition[] = [
  {
    type: 'voltage',
    name: 'Voltage Source',
    category: 'power',
    learnEntryId: 'voltage',
    pins: [
      { id: 'p', name: '+', offset: { x: 0, y: -20 } },
      { id: 'n', name: '-', offset: { x: 0, y: 20 } }
    ],
    footprint: { id: 'volt', name: '2-pin', pads: [{ x: 0, y: -10 }, { x: 0, y: 10 }] },
    defaultProperties: { voltage: 5 }
  },
  {
    type: 'ground',
    name: 'Ground',
    category: 'power',
    learnEntryId: 'ground',
    pins: [{ id: 'g', name: 'GND', offset: { x: 0, y: 0 } }],
    footprint: { id: 'gnd', name: '1-pin', pads: [{ x: 0, y: 0 }] }
  },
  {
    type: 'resistor',
    name: 'Resistor',
    category: 'passives',
    learnEntryId: 'resistor',
    pins: [
      { id: 'a', name: '1', offset: { x: -20, y: 0 } },
      { id: 'b', name: '2', offset: { x: 20, y: 0 } }
    ],
    footprint: { id: 'res', name: '0603', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }] },
    defaultProperties: { resistance: 1000 }
  },
  {
    type: 'capacitor',
    name: 'Capacitor',
    category: 'passives',
    learnEntryId: 'capacitor',
    pins: [
      { id: 'a', name: '1', offset: { x: -20, y: 0 } },
      { id: 'b', name: '2', offset: { x: 20, y: 0 } }
    ],
    footprint: { id: 'cap', name: '0603', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }] },
    defaultProperties: { capacitance: 0.000001 }
  },
  {
    type: 'diode',
    name: 'Diode',
    category: 'semiconductors',
    learnEntryId: 'diode',
    pins: [
      { id: 'a', name: 'A', offset: { x: -20, y: 0 } },
      { id: 'k', name: 'K', offset: { x: 20, y: 0 } }
    ],
    footprint: { id: 'sod123', name: 'SOD-123', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }] },
    defaultProperties: { vf: 0.7 }
  },
  {
    type: 'led',
    name: 'LED',
    category: 'semiconductors',
    learnEntryId: 'led',
    pins: [
      { id: 'a', name: 'A', offset: { x: -20, y: 0 } },
      { id: 'k', name: 'K', offset: { x: 20, y: 0 } }
    ],
    footprint: { id: 'led', name: '0603', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }] },
    defaultProperties: { vf: 2.0 }
  },
  {
    type: 'npn',
    name: 'NPN Transistor',
    category: 'semiconductors',
    learnEntryId: 'npn',
    pins: [
      { id: 'c', name: 'C', offset: { x: 0, y: -20 } },
      { id: 'b', name: 'B', offset: { x: -20, y: 20 } },
      { id: 'e', name: 'E', offset: { x: 20, y: 20 } }
    ],
    footprint: { id: 'to92', name: 'TO-92', pads: [{ x: -10, y: 10 }, { x: 0, y: 10 }, { x: 10, y: 10 }] },
    defaultProperties: { hfe: 100 }
  },
  {
    type: 'relay',
    name: 'Relay',
    category: 'electromechanical',
    learnEntryId: 'relay',
    pins: [
      { id: 'coil+', name: 'Coil+', offset: { x: -20, y: 0 } },
      { id: 'coil-', name: 'Coil-', offset: { x: 20, y: 0 } },
      { id: 'com', name: 'COM', offset: { x: -20, y: 30 } },
      { id: 'no', name: 'NO', offset: { x: 20, y: 30 } }
    ],
    footprint: { id: 'relay', name: 'Relay', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }, { x: -10, y: 20 }, { x: 10, y: 20 }] },
    defaultProperties: { coilVoltage: 5 }
  },
  {
    type: 'motor',
    name: 'DC Motor',
    category: 'electromechanical',
    learnEntryId: 'motor',
    pins: [
      { id: 'p', name: '+', offset: { x: -20, y: 0 } },
      { id: 'n', name: '-', offset: { x: 20, y: 0 } }
    ],
    footprint: { id: 'motor', name: 'Motor', pads: [{ x: -10, y: 0 }, { x: 10, y: 0 }] },
    defaultProperties: { resistance: 5 }
  },
  {
    type: 'mcu',
    name: 'Microcontroller',
    category: 'microcontrollers',
    learnEntryId: 'mcu',
    pins: [
      { id: 'vcc', name: 'VCC', offset: { x: -20, y: -20 } },
      { id: 'gnd', name: 'GND', offset: { x: -20, y: 20 } },
      { id: 'd2', name: 'D2', offset: { x: 20, y: -20 } },
      { id: 'd3', name: 'D3', offset: { x: 20, y: 0 } },
      { id: 'd4', name: 'D4', offset: { x: 20, y: 20 } },
      { id: 'd5', name: 'D5', offset: { x: 20, y: 40 } },
      { id: 'd6', name: 'D6', offset: { x: 20, y: 60 } },
      { id: 'd7', name: 'D7', offset: { x: 20, y: 80 } }
    ],
    footprint: {
      id: 'mcu',
      name: 'DIP',
      pads: [
        { x: -10, y: -10 },
        { x: -10, y: 10 },
        { x: 10, y: -10 },
        { x: 10, y: 10 },
        { x: 10, y: 20 },
        { x: 10, y: 30 },
        { x: 10, y: 40 },
        { x: 10, y: 50 }
      ]
    },
    defaultProperties: { logicLevel: 5 }
  },
  {
    type: 'rc522',
    name: 'RC522 RFID',
    category: 'sensors',
    learnEntryId: 'rc522',
    pins: [
      { id: 'ss', name: 'SS', offset: { x: -20, y: -20 } },
      { id: 'sck', name: 'SCK', offset: { x: -20, y: -10 } },
      { id: 'mosi', name: 'MOSI', offset: { x: -20, y: 0 } },
      { id: 'miso', name: 'MISO', offset: { x: -20, y: 10 } },
      { id: 'rst', name: 'RST', offset: { x: -20, y: 20 } },
      { id: 'irq', name: 'IRQ', offset: { x: 20, y: -20 } },
      { id: 'vcc', name: 'VCC', offset: { x: 20, y: 0 } },
      { id: 'gnd', name: 'GND', offset: { x: 20, y: 20 } }
    ],
    footprint: { id: 'rfid', name: 'RC522', pads: [{ x: -10, y: -10 }, { x: -10, y: 0 }, { x: -10, y: 10 }, { x: 10, y: 10 }] },
    defaultProperties: { logicLevel: 3.3 }
  }
];

export const partDefinitionMap = Object.fromEntries(
  partDefinitions.map((part) => [part.type, part])
);
