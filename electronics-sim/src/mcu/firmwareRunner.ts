import { MCUState } from '../types';

export class FirmwareRunner {
  state: MCUState;
  worker: Worker;
  onSerial: (message: string) => void;
  onSpiTransfer: (bytes: number[]) => Promise<number[]>;

  constructor(onSerial: (message: string) => void, onSpiTransfer: (bytes: number[]) => Promise<number[]>) {
    this.state = { pins: {}, serial: [], spi: [] };
    this.onSerial = onSerial;
    this.onSpiTransfer = onSpiTransfer;
    this.worker = new Worker(new URL('./firmwareWorker.ts', import.meta.url), { type: 'module' });

    this.worker.onmessage = (event: MessageEvent) => {
      const { type } = event.data as { type: string };
      if (type === 'pin-mode') {
        const { pin, mode } = event.data as { pin: string; mode: 'input' | 'output' };
        this.state.pins[pin] = { mode, level: 0 };
      }
      if (type === 'digital-write') {
        const { pin, level } = event.data as { pin: string; level: 0 | 1 };
        const entry = this.state.pins[pin] ?? { mode: 'output', level: 0 };
        entry.level = level;
        this.state.pins[pin] = entry;
      }
      if (type === 'serial') {
        const { message } = event.data as { message: string };
        this.state.serial.push(message);
        this.onSerial(message);
      }
      if (type === 'spi-transfer') {
        const { bytes } = event.data as { bytes: number[] };
        void this.onSpiTransfer(bytes).then((response) => {
          this.worker.postMessage({ type: 'spi-response', response });
        });
      }
    };
  }

  run(source: string) {
    this.worker.postMessage({ type: 'run', source });
  }
}
