import { useEffect, useMemo, useState } from 'react';
import { useAppStore } from './store';
import { partDefinitionMap } from './parts/partDefinitions';
import { SchematicCanvas } from './ui/SchematicCanvas';
import { Palette } from './ui/Palette';
import { InspectorPanel } from './ui/InspectorPanel';
import { TooltipCard } from './ui/TooltipCard';
import { SerialMonitor } from './ui/SerialMonitor';
import { PcbView } from './ui/PcbView';
import { TagControl } from './ui/TagControl';
import { HintBanner } from './ui/HintBanner';
import { LessonPlayer } from './ui/LessonPlayer';
import { learnEntries } from './learn/learnEntries';
import { evaluateHintRules } from './learn/hintRules';
import { demoLesson } from './learn/lessons';
import { stepSimulation } from './sim/simulator';
import { FirmwareRunner } from './mcu/firmwareRunner';
import { RC522Sim } from './devices/rc522';
import { Point, ProjectData } from './types';

const generateId = () => `id_${Math.random().toString(36).slice(2, 8)}`;

const createNewPart = (type: string, project: ProjectData) => {
  const definition = partDefinitionMap[type];
  const id = generateId();
  const position = { x: 200, y: 200 };
  const netSuffix = Math.random().toString(36).slice(2, 5);
  const newNets = definition.pins.map((pin) => ({ id: `${id}_${pin.id}_${netSuffix}`, name: `${type}_${pin.name}` }));
  const pins = definition.pins.map((pin, index) => ({ pinId: pin.id, netId: newNets[index].id }));

  return {
    part: {
      id,
      type,
      position,
      rotation: 0,
      properties: { ...definition.defaultProperties },
      pins
    },
    nets: newNets
  };
};

export const App = () => {
  const {
    project,
    simulation,
    viewMode,
    selectedPartId,
    hoveredPartId,
    serialLogs,
    hints,
    loadDemo,
    toggleLearningMode,
    setViewMode,
    setSelectedPart,
    setHoveredPart,
    updatePartPosition,
    rotatePart,
    deletePart,
    addSerialLog,
    setHints
  } = useAppStore();

  const [lessonStep, setLessonStep] = useState(0);
  const [showLesson, setShowLesson] = useState(true);
  const [simQuality, setSimQuality] = useState<'fast' | 'accurate'>('fast');
  const [wireMode, setWireMode] = useState(false);
  const [pendingWire, setPendingWire] = useState<Point[]>([]);
  const rc522 = useMemo(() => new RC522Sim(), []);
  const firmwareRunner = useMemo(
    () =>
      new FirmwareRunner(
        (message) => addSerialLog(message),
        async (bytes) => rc522.handleTransaction(bytes)
      ),
    [addSerialLog, rc522]
  );

  useEffect(() => {
    firmwareRunner.run(project.firmware.source);
  }, [firmwareRunner, project.firmware.source]);

  useEffect(() => {
    let animationFrame = 0;
    let lastTime = performance.now();

    const tick = (time: number) => {
      const delta = (time - lastTime) / 1000;
      lastTime = time;
      const result = stepSimulation(project, simulation, delta);
      useAppStore.setState({ simulation: result.state });
      const newHints = evaluateHintRules(project, result.state, project.ui.learningMode);
      setHints(newHints);
      animationFrame = requestAnimationFrame(tick);
    };

    animationFrame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(animationFrame);
  }, [project, simulation, setHints]);

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if (!selectedPartId) return;
      if (event.key === 'Delete') {
        deletePart(selectedPartId);
      }
      if (event.key.toLowerCase() === 'r') {
        rotatePart(selectedPartId);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [selectedPartId, deletePart, rotatePart]);

  const getLearnEntry = (partId: string | null) => {
    const part = project.parts.find((entry) => entry.id === partId);
    if (!part) return null;
    const definition = partDefinitionMap[part.type];
    if (!definition) return null;
    return learnEntries[definition.learnEntryId] ?? null;
  };

  const hoveredEntry = getLearnEntry(hoveredPartId);
  const hoveredPart = project.parts.find((part) => part.id === hoveredPartId) ?? null;
  const hoveredDefinition = hoveredPart ? partDefinitionMap[hoveredPart.type] : null;
  const hoveredCategory = hoveredDefinition?.category;
  const hoveredVoltages = hoveredPart
    ? hoveredPart.pins.map((pin) => ({
        pin: pin.pinId.toUpperCase(),
        voltage: simulation.nets[pin.netId]?.voltage ?? 0
      }))
    : [];
  const selectedPart = project.parts.find((part) => part.id === selectedPartId) ?? null;
  const selectedEntry = getLearnEntry(selectedPartId);
  const highlightPartIds = showLesson ? demoLesson[lessonStep]?.highlightPartIds ?? [] : [];

  const handleAddPart = (type: string) => {
    const { part, nets } = createNewPart(type, project);
    useAppStore.setState({
      project: {
        ...project,
        parts: [...project.parts, part],
        nets: [...project.nets, ...nets]
      }
    });
  };

  const handlePresentTag = (uid: number[]) => {
    rc522.setTag(uid);
    addSerialLog(`Tag presented: ${uid.map((b) => b.toString(16).padStart(2, '0')).join(':')}`);
  };

  const handleCanvasClick = (point: Point) => {
    if (!wireMode) return;
    const nextPoints = [...pendingWire, point];
    if (nextPoints.length === 2) {
      const netId = `net_${generateId()}`;
      useAppStore.setState({
        project: {
          ...project,
          nets: [...project.nets, { id: netId, name: `Net ${project.nets.length + 1}` }],
          wires: [...project.wires, { id: generateId(), points: nextPoints, netId }]
        }
      });
      setPendingWire([]);
    } else {
      setPendingWire(nextPoints);
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Electronics Simulator MVP</h1>
        <div className="app-actions">
          <button onClick={() => setViewMode('schematic')} className={viewMode === 'schematic' ? 'active' : ''}>
            Schematic View
          </button>
          <button onClick={() => setViewMode('pcb')} className={viewMode === 'pcb' ? 'active' : ''}>
            PCB View
          </button>
          <button onClick={toggleLearningMode}>Learning Mode: {project.ui.learningMode ? 'On' : 'Off'}</button>
          <button onClick={loadDemo}>Load Demo</button>
          <button onClick={() => setShowLesson((value) => !value)}>
            {showLesson ? 'Hide Guided Lesson' : 'Guided Lesson'}
          </button>
          <button onClick={() => setWireMode((value) => !value)} className={wireMode ? 'active' : ''}>
            {wireMode ? 'Exit Wire Tool' : 'Wire Tool'}
          </button>
        </div>
      </header>
      <div className="app-body">
        <Palette onAdd={handleAddPart} />
        <main className="canvas-panel">
          {viewMode === 'schematic' ? (
            <SchematicCanvas
              project={project}
              simulation={simulation}
              selectedPartId={selectedPartId}
              highlightPartIds={highlightPartIds}
              onSelectPart={setSelectedPart}
              onHoverPart={setHoveredPart}
              onMovePart={(id, position) => updatePartPosition(id, position.x, position.y)}
              onCanvasClick={wireMode ? handleCanvasClick : undefined}
            />
          ) : (
            <PcbView project={project} />
          )}
          {hoveredEntry ? (
            <TooltipCard
              entry={hoveredEntry}
              learningMode={project.ui.learningMode}
              voltages={hoveredVoltages}
              category={hoveredCategory}
              pinoutNotes={hoveredEntry?.pinoutNotes}
              footprint={hoveredDefinition?.footprint.name}
            />
          ) : null}
          <HintBanner hints={hints} />
        </main>
        <aside className="right-panel">
          <InspectorPanel part={selectedPart} entry={selectedEntry ?? null} />
          {showLesson ? (
            <LessonPlayer
              steps={demoLesson}
              currentStep={lessonStep}
              onPrev={() => setLessonStep((step) => Math.max(0, step - 1))}
              onNext={() => setLessonStep((step) => Math.min(demoLesson.length - 1, step + 1))}
            />
          ) : null}
          <TagControl onPresent={handlePresentTag} onRemove={() => rc522.clearTag()} />
          <SerialMonitor logs={serialLogs} />
        </aside>
      </div>
      <footer className="app-footer">
        <div>Drag parts to move. Press R to rotate, Delete to remove.</div>
        <label className="sim-quality">
          Simulation quality
          <select value={simQuality} onChange={(event) => setSimQuality(event.target.value as 'fast' | 'accurate')}>
            <option value="fast">Fast</option>
            <option value="accurate">Accurate-ish</option>
          </select>
        </label>
      </footer>
    </div>
  );
};
