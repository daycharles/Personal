# Electronics Simulator MVP

A local-first MVP for a schematic-style circuit simulator + PCB view + MCU + RC522 + learning system. This project focuses on an extensible architecture and a working demo circuit.

## How to run

```bash
npm install
npm run dev
```

## Architecture overview

- **UI (React + Vite + TypeScript)**: Components live under `src/ui` and render the palette, canvas, inspector, serial monitor, lesson player, and learning hints.
- **State (Zustand)**: `src/store.ts` manages the project, simulation state, and UI settings.
- **Simulation (Canvas)**: The schematic view uses HTML Canvas for efficient drawing and animation. Canvas was chosen over WebGL for easier 2D overlays (labels, tooltips, current dots) and quick iteration for MVP scope.
- **Simulation Engine**: `src/sim` holds the netlist builder and a simplified time-step loop (explicit Euler-like updates with damping via clamped thresholds).
- **MCU Execution**: A web worker runs a minimal Arduino-like API (digitalWrite, pinMode, delay, SPI.transfer, Serial.print) and communicates with the main thread.
- **Devices**: `src/devices` contains the RC522, relay, and motor models.
- **Learning System**: `src/learn` includes LearnEntries, SVG symbols, a rule-based hint engine, and guided lessons.

## Simulation approximations

- Linear components are treated with constant values (resistors, capacitors, voltage sources) with direct voltage injection for MVP speed.
- Nonlinear devices (diode, BJT, relay, motor) use simplified thresholds and switch-like approximations.
- The solver is intentionally coarse: it prioritizes visual feedback, hints, and learning overlays rather than precise SPICE accuracy.

## Demo circuit

Use **Load Demo** to populate:
- MCU -> NPN transistor -> relay coil with flyback diode.
- Relay contact switches a DC motor.
- RC522 RFID module over SPI with a simulated tag UID.

Use **Present Tag** to fire a UID event and see logs in the serial monitor.

## Limitations (MVP)

- Not a full SPICE simulator.
- PCB autorouting is not included.
- No guarantee of signal integrity (EMI, ringing, trace parasitics).
- RC522 protocol support is minimal and only covers UID demo flow.
- Manual net editing is simplified.

## Next steps

- Improve netlist extraction from wires and pin snapping.
- Add PWM support and more MCU peripherals.
- Expand the device library and PCB routing tools.
- Add power analysis and more precise solvers.
