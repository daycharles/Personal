export type Point = { x: number; y: number };

export type PinDefinition = {
  id: string;
  name: string;
  offset: Point;
};

export type FootprintDefinition = {
  id: string;
  name: string;
  pads: Point[];
};

export type PartDefinition = {
  type: string;
  name: string;
  category: string;
  pins: PinDefinition[];
  footprint: FootprintDefinition;
  learnEntryId: string;
  defaultProperties?: Record<string, number | string | boolean>;
};

export type LearnEntry = {
  id: string;
  title: string;
  summary: string;
  detailedExplanation: string;
  typicalUses: string[];
  gotchas: string[];
  symbolUS: string;
  symbolIEC: string;
  pinoutNotes: string[];
  parameterGlossary: { name: string; meaning: string; typicalRange: string }[];
  relatedParts: string[];
};

export type LessonStep = {
  id: string;
  title: string;
  body: string;
  highlightPartIds: string[];
  hintNetIds?: string[];
};

export type HintRule = {
  id: string;
  description: string;
  evaluate: (state: SimulationState) => string | null;
};

export type Net = {
  id: string;
  name: string;
};

export type Node = {
  id: string;
  netId: string;
  point: Point;
};

export type Connection = {
  id: string;
  from: string;
  to: string;
};

export type PartInstance = {
  id: string;
  type: string;
  position: Point;
  rotation: number;
  properties: Record<string, number | string | boolean>;
  pins: { pinId: string; netId: string }[];
};

export type Wire = {
  id: string;
  points: Point[];
  netId: string;
};

export type SimulationState = {
  time: number;
  nets: Record<string, { voltage: number; current: number }>;
  faults: string[];
  messages: string[];
  relayStates: Record<string, boolean>;
};

export type StepResult = {
  state: SimulationState;
  deltaTime: number;
};

export type MCUState = {
  pins: Record<string, { mode: 'input' | 'output'; level: 0 | 1 }>;
  serial: string[];
  spi: SPITransaction[];
};

export type SPITransaction = {
  id: string;
  bytes: number[];
  response: number[];
  deviceId: string;
};

export type ProjectData = {
  version: number;
  parts: PartInstance[];
  wires: Wire[];
  nets: Net[];
  firmware: { source: string; boardType: string };
  ui: { learningMode: boolean };
};
