import { LessonStep } from '../types';

export const demoLesson: LessonStep[] = [
  {
    id: 'step-1',
    title: 'Power the circuit',
    body: 'The voltage source provides 5V referenced to ground. This powers the MCU, relay coil, and RC522.',
    highlightPartIds: ['v1', 'gnd1', 'mcu1']
  },
  {
    id: 'step-2',
    title: 'Drive the relay',
    body: 'The MCU drives the NPN transistor through a base resistor. The transistor energizes the relay coil.',
    highlightPartIds: ['rb1', 'q1', 'k1']
  },
  {
    id: 'step-3',
    title: 'Protect the coil',
    body: 'The flyback diode clamps the voltage spike from the relay coil.',
    highlightPartIds: ['d1']
  },
  {
    id: 'step-4',
    title: 'Switch the motor',
    body: 'When the relay closes, the motor sees 5V and spins.',
    highlightPartIds: ['m1']
  },
  {
    id: 'step-5',
    title: 'Read the RFID tag',
    body: 'The RC522 communicates over SPI. Present a tag to see the UID logged in the serial monitor.',
    highlightPartIds: ['rc1']
  }
];
