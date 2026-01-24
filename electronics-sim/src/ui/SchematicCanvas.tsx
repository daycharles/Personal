import { useEffect, useRef, useState } from 'react';
import { PartInstance, Point, ProjectData, SimulationState } from '../types';
import { partDefinitionMap } from '../parts/partDefinitions';

const CANVAS_WIDTH = 900;
const CANVAS_HEIGHT = 560;

const partSize = { width: 70, height: 40 };

const getPartBounds = (part: PartInstance) => ({
  x: part.position.x - partSize.width / 2,
  y: part.position.y - partSize.height / 2,
  width: partSize.width,
  height: partSize.height
});

const isPointInBounds = (point: Point, bounds: { x: number; y: number; width: number; height: number }) =>
  point.x >= bounds.x && point.x <= bounds.x + bounds.width && point.y >= bounds.y && point.y <= bounds.y + bounds.height;

const drawPart = (ctx: CanvasRenderingContext2D, part: PartInstance, selected: boolean) => {
  const bounds = getPartBounds(part);
  ctx.save();
  ctx.translate(part.position.x, part.position.y);
  ctx.rotate((part.rotation * Math.PI) / 180);
  ctx.fillStyle = selected ? '#1f4dff' : '#1f2937';
  ctx.strokeStyle = '#90caf9';
  ctx.lineWidth = 2;
  ctx.fillRect(-partSize.width / 2, -partSize.height / 2, partSize.width, partSize.height);
  ctx.strokeRect(-partSize.width / 2, -partSize.height / 2, partSize.width, partSize.height);
  ctx.fillStyle = '#e2e8f0';
  ctx.font = '12px sans-serif';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  const label = partDefinitionMap[part.type]?.name ?? part.type;
  ctx.fillText(label, 0, 0);
  ctx.restore();

  ctx.strokeStyle = '#fbbf24';
  partDefinitionMap[part.type]?.pins.forEach((pin) => {
    const px = part.position.x + pin.offset.x;
    const py = part.position.y + pin.offset.y;
    ctx.beginPath();
    ctx.arc(px, py, 4, 0, Math.PI * 2);
    ctx.stroke();
  });
};

const drawWire = (ctx: CanvasRenderingContext2D, points: Point[] | undefined, phase: number) => {
  if (!points || points.length < 2) return;
  const safePoints = points.filter((point): point is Point => Boolean(point));
  if (safePoints.length < 2) return;
  ctx.strokeStyle = '#38bdf8';
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(safePoints[0].x, safePoints[0].y);
  safePoints.slice(1).forEach((point) => ctx.lineTo(point.x, point.y));
  ctx.stroke();

  const dotCount = 4;
  for (let i = 0; i < dotCount; i += 1) {
    const t = ((phase / 1000 + i / dotCount) % 1) * (safePoints.length - 1);
    const idx = Math.floor(t);
    const local = t - idx;
    const start = safePoints[idx];
    const end = safePoints[idx + 1] ?? start;
    const x = start.x + (end.x - start.x) * local;
    const y = start.y + (end.y - start.y) * local;
    ctx.fillStyle = '#f97316';
    ctx.beginPath();
    ctx.arc(x, y, 3, 0, Math.PI * 2);
    ctx.fill();
  }
};

type Props = {
  project: ProjectData;
  simulation: SimulationState;
  selectedPartId: string | null;
  highlightPartIds?: string[];
  onSelectPart: (id: string | null) => void;
  onHoverPart: (id: string | null) => void;
  onMovePart: (id: string, position: Point) => void;
  onCanvasClick?: (point: Point) => void;
};

export const SchematicCanvas = ({
  project,
  simulation,
  selectedPartId,
  highlightPartIds = [],
  onSelectPart,
  onHoverPart,
  onMovePart,
  onCanvasClick
}: Props) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [dragging, setDragging] = useState<{ id: string; offset: Point } | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);

    project.wires.forEach((wire) => drawWire(ctx, wire.points, simulation.time * 1000));

    project.parts.forEach((part) => {
      const isSelected = part.id === selectedPartId;
      const isHighlighted = highlightPartIds.includes(part.id);
      if (isHighlighted) {
        ctx.strokeStyle = '#facc15';
        ctx.lineWidth = 3;
        const bounds = getPartBounds(part);
        ctx.strokeRect(bounds.x - 4, bounds.y - 4, bounds.width + 8, bounds.height + 8);
      }
      drawPart(ctx, part, isSelected);
    });
  }, [project, selectedPartId, simulation.time, highlightPartIds]);

  const getPartAtPoint = (point: Point) =>
    project.parts.find((part) => isPointInBounds(point, getPartBounds(part)));

  const handleMouseDown = (event: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const point = { x: event.clientX - rect.left, y: event.clientY - rect.top };
    if (onCanvasClick) {
      onCanvasClick(point);
      return;
    }
    const part = getPartAtPoint(point);
    if (part) {
      onSelectPart(part.id);
      setDragging({ id: part.id, offset: { x: point.x - part.position.x, y: point.y - part.position.y } });
    } else {
      onSelectPart(null);
    }
  };

  const handleMouseMove = (event: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const point = { x: event.clientX - rect.left, y: event.clientY - rect.top };

    if (dragging) {
      onMovePart(dragging.id, { x: point.x - dragging.offset.x, y: point.y - dragging.offset.y });
      return;
    }

    const part = getPartAtPoint(point);
    onHoverPart(part ? part.id : null);
  };

  const handleMouseUp = () => {
    setDragging(null);
  };

  const handleMouseLeave = () => {
    setDragging(null);
    onHoverPart(null);
  };

  return (
    <canvas
      ref={canvasRef}
      width={CANVAS_WIDTH}
      height={CANVAS_HEIGHT}
      className="schematic-canvas"
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseLeave}
    />
  );
};
