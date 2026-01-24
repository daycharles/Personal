import { LearnEntry } from '../types';
import { symbols } from './symbols';

export const learnEntries: Record<string, LearnEntry> = {
  resistor: {
    id: 'resistor',
    title: 'Resistor',
    summary: 'A passive component that limits current and drops voltage.',
    detailedExplanation:
      'Resistors convert electrical energy into heat. They are used to set currents, create voltage dividers, and bias transistors.',
    typicalUses: ['LED current limiting', 'Voltage dividers', 'Pull-up/down networks'],
    gotchas: ['Check power rating when dissipating heat', 'Tolerance affects accuracy', 'Series/parallel changes total resistance'],
    symbolUS: symbols.resistorUS,
    symbolIEC: symbols.resistorIEC,
    pinoutNotes: ['Non-polarized; either pin can be connected to any node.'],
    parameterGlossary: [
      { name: 'Resistance (Ω)', meaning: 'Opposition to current flow.', typicalRange: '1Ω–10MΩ' },
      { name: 'Power (W)', meaning: 'Maximum safe dissipation.', typicalRange: '0.125W–5W' }
    ],
    relatedParts: ['capacitor']
  },
  capacitor: {
    id: 'capacitor',
    title: 'Capacitor',
    summary: 'Stores energy in an electric field and resists voltage changes.',
    detailedExplanation:
      'Capacitors store charge, smoothing power rails and filtering signals. Polarized capacitors must be oriented correctly.',
    typicalUses: ['Decoupling power rails', 'Timing circuits', 'Signal coupling'],
    gotchas: ['Polarized caps can fail if reversed', 'High ripple current warms capacitors', 'ESR affects transient response'],
    symbolUS: symbols.capacitorUS,
    symbolIEC: symbols.capacitorIEC,
    pinoutNotes: ['Polarized capacitors have a marked negative lead.'],
    parameterGlossary: [
      { name: 'Capacitance (F)', meaning: 'Charge storage ability.', typicalRange: '1pF–10mF' },
      { name: 'Voltage rating', meaning: 'Maximum working voltage.', typicalRange: '6.3V–100V' }
    ],
    relatedParts: ['resistor']
  },
  diode: {
    id: 'diode',
    title: 'Diode',
    summary: 'Allows current to flow in one direction only.',
    detailedExplanation:
      'A diode drops voltage (about 0.7V for silicon) when forward-biased and blocks reverse current up to its rating.',
    typicalUses: ['Flyback protection', 'Rectification', 'Polarity protection'],
    gotchas: ['Forward drop causes power loss', 'Reverse voltage can damage the diode', 'Switching diodes have speed limits'],
    symbolUS: symbols.diodeUS,
    symbolIEC: symbols.diodeIEC,
    pinoutNotes: ['Anode to cathode direction matters. Cathode is marked.'],
    parameterGlossary: [
      { name: 'Vf', meaning: 'Forward voltage drop.', typicalRange: '0.2V–1V' },
      { name: 'If', meaning: 'Forward current rating.', typicalRange: '50mA–10A' }
    ],
    relatedParts: ['led', 'relay']
  },
  led: {
    id: 'led',
    title: 'LED',
    summary: 'A diode that emits light when forward biased.',
    detailedExplanation:
      'LEDs emit light proportional to current. They require a resistor or constant-current driver to limit current.',
    typicalUses: ['Indicators', 'Displays', 'Optical signaling'],
    gotchas: ['Always use a current limiter', 'Brightness depends on current', 'Polarity matters'],
    symbolUS: symbols.ledUS,
    symbolIEC: symbols.ledIEC,
    pinoutNotes: ['Long lead is anode, flat edge is cathode.'],
    parameterGlossary: [
      { name: 'Vf', meaning: 'Forward voltage.', typicalRange: '1.8V–3.3V' },
      { name: 'If', meaning: 'Forward current.', typicalRange: '2mA–20mA' }
    ],
    relatedParts: ['resistor']
  },
  npn: {
    id: 'npn',
    title: 'NPN Transistor',
    summary: 'A bipolar transistor used as a current-controlled switch or amplifier.',
    detailedExplanation:
      'Driving current into the base allows current from collector to emitter. In saturation, it acts like a closed switch.',
    typicalUses: ['Switching relays', 'Amplifying signals', 'Level shifting'],
    gotchas: ['Base resistor is required', 'Vce(sat) is not zero', 'Watch hFE at required current'],
    symbolUS: symbols.bjtUS,
    symbolIEC: symbols.bjtIEC,
    pinoutNotes: ['Pins are typically E-B-C or C-B-E depending on package.'],
    parameterGlossary: [
      { name: 'Vce(sat)', meaning: 'Voltage drop when saturated.', typicalRange: '0.1V–0.3V' },
      { name: 'hFE', meaning: 'DC gain.', typicalRange: '50–200' }
    ],
    relatedParts: ['relay', 'resistor']
  },
  relay: {
    id: 'relay',
    title: 'Relay',
    summary: 'Electromechanical switch controlled by a coil.',
    detailedExplanation:
      'A relay uses an energized coil to move contacts that switch another circuit. A flyback diode is required across the coil.',
    typicalUses: ['Isolated switching', 'High-current load control', 'Voltage translation'],
    gotchas: ['Always add a flyback diode', 'Coil current can be high', 'Contact bounce is real'],
    symbolUS: symbols.relayUS,
    symbolIEC: symbols.relayIEC,
    pinoutNotes: ['Coil pins are separate from NO/NC contacts.'],
    parameterGlossary: [
      { name: 'Coil voltage', meaning: 'Voltage required to energize coil.', typicalRange: '5V–24V' },
      { name: 'Contact rating', meaning: 'Max switched voltage/current.', typicalRange: '1A–10A' }
    ],
    relatedParts: ['diode', 'npn']
  },
  motor: {
    id: 'motor',
    title: 'DC Motor',
    summary: 'Converts electrical energy into rotation via a brushed DC motor.',
    detailedExplanation:
      'A DC motor behaves like a resistor plus back-EMF. It draws higher current at startup and can generate noise.',
    typicalUses: ['Robotics', 'Fans', 'Pumps'],
    gotchas: ['Use flyback protection', 'Startup current can be large', 'PWM needs filtering for torque smoothness'],
    symbolUS: symbols.motorUS,
    symbolIEC: symbols.motorIEC,
    pinoutNotes: ['Polarity reverses rotation direction.'],
    parameterGlossary: [
      { name: 'Stall current', meaning: 'Current at zero speed.', typicalRange: '0.5A–5A' },
      { name: 'Rated voltage', meaning: 'Nominal supply voltage.', typicalRange: '3V–24V' }
    ],
    relatedParts: ['relay']
  },
  ground: {
    id: 'ground',
    title: 'Ground',
    summary: 'Reference point for circuit voltages.',
    detailedExplanation:
      'Ground is a shared reference node. All voltages are measured relative to ground.',
    typicalUses: ['Reference for signals', 'Return path for currents'],
    gotchas: ['Multiple grounds can create loops', 'Floating grounds cause undefined voltages'],
    symbolUS: symbols.groundUS,
    symbolIEC: symbols.groundIEC,
    pinoutNotes: ['All ground symbols in a schematic are the same net.'],
    parameterGlossary: [],
    relatedParts: []
  },
  voltage: {
    id: 'voltage',
    title: 'Voltage Source',
    summary: 'Provides a fixed potential difference.',
    detailedExplanation:
      'A voltage source sets a node voltage relative to ground, powering circuits. Current is determined by the load.',
    typicalUses: ['Power rails', 'Reference voltages'],
    gotchas: ['Shorting a source can cause excessive current', 'Noise on supply can affect logic'],
    symbolUS: symbols.voltageUS,
    symbolIEC: symbols.voltageIEC,
    pinoutNotes: ['Positive and negative terminals must be respected.'],
    parameterGlossary: [
      { name: 'Voltage', meaning: 'Output potential difference.', typicalRange: '1V–24V' }
    ],
    relatedParts: ['ground']
  },
  rc522: {
    id: 'rc522',
    title: 'RC522 RFID Module',
    summary: '13.56MHz RFID reader using SPI.',
    detailedExplanation:
      'The RC522 communicates over SPI and can read MIFARE tags. It requires proper SPI wiring and a stable 3.3V supply.',
    typicalUses: ['Access control', 'Tag identification', 'Inventory tracking'],
    gotchas: ['Operate at 3.3V logic', 'SPI chip select is active low', 'Antenna tuning affects range'],
    symbolUS: symbols.rfidUS,
    symbolIEC: symbols.rfidIEC,
    pinoutNotes: ['Pins: SS, SCK, MOSI, MISO, RST, IRQ, VCC, GND.'],
    parameterGlossary: [
      { name: 'Supply', meaning: 'Operating voltage.', typicalRange: '3.3V' },
      { name: 'SPI mode', meaning: 'Clock polarity/phase.', typicalRange: 'Mode 0' }
    ],
    relatedParts: ['mcu']
  },
  mcu: {
    id: 'mcu',
    title: 'Microcontroller',
    summary: 'A programmable IC that reads inputs, controls outputs, and runs firmware.',
    detailedExplanation:
      'Microcontrollers integrate CPU, memory, and peripherals. In this MVP they expose GPIO, SPI, and Serial APIs to drive the simulated circuit.',
    typicalUses: ['Embedded control', 'Sensing + actuation', 'Automation'],
    gotchas: ['Logic levels must match the circuit', 'Ground must be shared', 'GPIO current limits apply'],
    symbolUS: symbols.voltageUS,
    symbolIEC: symbols.voltageIEC,
    pinoutNotes: ['Pin functions vary by board. Configure pinMode before driving outputs.'],
    parameterGlossary: [
      { name: 'Logic level', meaning: 'HIGH/LOW voltage levels.', typicalRange: '3.3V–5V' },
      { name: 'GPIO current', meaning: 'Max per-pin source/sink.', typicalRange: '5mA–20mA' }
    ],
    relatedParts: ['rc522', 'relay']
  }
};
