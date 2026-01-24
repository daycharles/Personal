import { create } from 'zustand';
import { ProjectData, SimulationState, Wire } from './types';
import { demoProject } from './persistence/demoProject';
import { createInitialState } from './sim/simulator';

export type ViewMode = 'schematic' | 'pcb';

type AppState = {
  project: ProjectData;
  simulation: SimulationState;
  viewMode: ViewMode;
  selectedPartId: string | null;
  hoveredPartId: string | null;
  serialLogs: string[];
  hints: string[];
  loadDemo: () => void;
  toggleLearningMode: () => void;
  setViewMode: (mode: ViewMode) => void;
  setSelectedPart: (id: string | null) => void;
  setHoveredPart: (id: string | null) => void;
  updatePartPosition: (id: string, x: number, y: number) => void;
  rotatePart: (id: string) => void;
  deletePart: (id: string) => void;
  addWire: (wire: Wire) => void;
  addSerialLog: (message: string) => void;
  setHints: (hints: string[]) => void;
};

export const useAppStore = create<AppState>((set) => ({
  project: demoProject,
  simulation: createInitialState(demoProject),
  viewMode: 'schematic',
  selectedPartId: null,
  hoveredPartId: null,
  serialLogs: [],
  hints: [],
  loadDemo: () =>
    set(() => ({
      project: demoProject,
      simulation: createInitialState(demoProject),
      selectedPartId: null,
      hoveredPartId: null,
      serialLogs: []
    })),
  toggleLearningMode: () =>
    set((state) => ({
      project: { ...state.project, ui: { learningMode: !state.project.ui.learningMode } }
    })),
  setViewMode: (mode) => set(() => ({ viewMode: mode })),
  setSelectedPart: (id) => set(() => ({ selectedPartId: id })),
  setHoveredPart: (id) => set(() => ({ hoveredPartId: id })),
  updatePartPosition: (id, x, y) =>
    set((state) => ({
      project: {
        ...state.project,
        parts: state.project.parts.map((part) =>
          part.id === id ? { ...part, position: { x, y } } : part
        )
      }
    })),
  rotatePart: (id) =>
    set((state) => ({
      project: {
        ...state.project,
        parts: state.project.parts.map((part) =>
          part.id === id ? { ...part, rotation: (part.rotation + 90) % 360 } : part
        )
      }
    })),
  deletePart: (id) =>
    set((state) => ({
      project: {
        ...state.project,
        parts: state.project.parts.filter((part) => part.id !== id)
      },
      selectedPartId: state.selectedPartId === id ? null : state.selectedPartId
    })),
  addWire: (wire) =>
    set((state) => ({
      project: {
        ...state.project,
        wires: [...state.project.wires, wire]
      }
    })),
  addSerialLog: (message) =>
    set((state) => ({ serialLogs: [...state.serialLogs, message] })),
  setHints: (hints) => set(() => ({ hints }))
}));
