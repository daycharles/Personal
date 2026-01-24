import { ProjectData } from '../types';
import { demoFirmware } from '../mcu/demoFirmware';

export const demoProject: ProjectData = {
  version: 1,
  nets: [
    { id: 'gnd', name: 'GND' },
    { id: 'vcc', name: 'VCC' },
    { id: 'coil+', name: 'RELAY_COIL+' },
    { id: 'coil-', name: 'RELAY_COIL-' },
    { id: 'motor+', name: 'MOTOR+' },
    { id: 'mcu_d2', name: 'MCU_D2' },
    { id: 'mcu_d3', name: 'MCU_D3' },
    { id: 'mcu_d4', name: 'MCU_D4' },
    { id: 'mcu_d5', name: 'MCU_D5' },
    { id: 'mcu_d6', name: 'MCU_D6' },
    { id: 'mcu_d7', name: 'MCU_D7' },
    { id: 'base', name: 'BJT_BASE' }
  ],
  parts: [
    {
      id: 'v1',
      type: 'voltage',
      position: { x: 120, y: 120 },
      rotation: 0,
      properties: { voltage: 5 },
      pins: [
        { pinId: 'p', netId: 'vcc' },
        { pinId: 'n', netId: 'gnd' }
      ]
    },
    {
      id: 'gnd1',
      type: 'ground',
      position: { x: 120, y: 220 },
      rotation: 0,
      properties: {},
      pins: [{ pinId: 'g', netId: 'gnd' }]
    },
    {
      id: 'mcu1',
      type: 'mcu',
      position: { x: 280, y: 120 },
      rotation: 0,
      properties: { logicLevel: 5 },
      pins: [
        { pinId: 'vcc', netId: 'vcc' },
        { pinId: 'gnd', netId: 'gnd' },
        { pinId: 'd2', netId: 'mcu_d2' },
        { pinId: 'd3', netId: 'mcu_d3' },
        { pinId: 'd4', netId: 'mcu_d4' },
        { pinId: 'd5', netId: 'mcu_d5' },
        { pinId: 'd6', netId: 'mcu_d6' },
        { pinId: 'd7', netId: 'mcu_d7' }
      ]
    },
    {
      id: 'rb1',
      type: 'resistor',
      position: { x: 420, y: 120 },
      rotation: 0,
      properties: { resistance: 1000 },
      pins: [
        { pinId: 'a', netId: 'mcu_d5' },
        { pinId: 'b', netId: 'base' }
      ]
    },
    {
      id: 'q1',
      type: 'npn',
      position: { x: 520, y: 120 },
      rotation: 0,
      properties: { hfe: 100 },
      pins: [
        { pinId: 'c', netId: 'coil-' },
        { pinId: 'b', netId: 'base' },
        { pinId: 'e', netId: 'gnd' }
      ]
    },
    {
      id: 'd1',
      type: 'diode',
      position: { x: 520, y: 40 },
      rotation: 0,
      properties: { vf: 0.7 },
      pins: [
        { pinId: 'a', netId: 'coil-' },
        { pinId: 'k', netId: 'coil+' }
      ]
    },
    {
      id: 'k1',
      type: 'relay',
      position: { x: 620, y: 120 },
      rotation: 0,
      properties: { coilVoltage: 5 },
      pins: [
        { pinId: 'coil+', netId: 'coil+' },
        { pinId: 'coil-', netId: 'coil-' },
        { pinId: 'com', netId: 'motor+' },
        { pinId: 'no', netId: 'vcc' }
      ]
    },
    {
      id: 'm1',
      type: 'motor',
      position: { x: 760, y: 120 },
      rotation: 0,
      properties: { resistance: 5 },
      pins: [
        { pinId: 'p', netId: 'motor+' },
        { pinId: 'n', netId: 'gnd' }
      ]
    },
    {
      id: 'rc1',
      type: 'rc522',
      position: { x: 280, y: 260 },
      rotation: 0,
      properties: { logicLevel: 3.3 },
      pins: [
        { pinId: 'ss', netId: 'mcu_d3' },
        { pinId: 'sck', netId: 'mcu_d2' },
        { pinId: 'mosi', netId: 'mcu_d6' },
        { pinId: 'miso', netId: 'mcu_d7' },
        { pinId: 'rst', netId: 'mcu_d4' },
        { pinId: 'irq', netId: 'mcu_d4' },
        { pinId: 'vcc', netId: 'vcc' },
        { pinId: 'gnd', netId: 'gnd' }
      ]
    }
  ],
  wires: [
    { id: 'w1', points: [{ x: 120, y: 120 }, { x: 280, y: 120 }], netId: 'vcc' }
  ],
  firmware: { source: demoFirmware, boardType: 'demo-mcu' },
  ui: { learningMode: true }
};
