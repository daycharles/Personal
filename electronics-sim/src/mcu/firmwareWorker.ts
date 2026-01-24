const ctx: Worker = self as unknown as Worker;

type FirmwareMessage =
  | { type: 'run'; source: string }
  | { type: 'spi-response'; response: number[] };

let pendingSpiResolve: ((value: number[]) => void) | null = null;

const api = {
  pinMode: (pin: string, mode: 'input' | 'output') => {
    ctx.postMessage({ type: 'pin-mode', pin, mode });
  },
  digitalWrite: (pin: string, level: 0 | 1) => {
    ctx.postMessage({ type: 'digital-write', pin, level });
  },
  delay: (ms: number) => new Promise((resolve) => setTimeout(resolve, ms)),
  SPI: {
    transfer: (bytes: number[]) => {
      ctx.postMessage({ type: 'spi-transfer', bytes });
      return new Promise<number[]>((resolve) => {
        pendingSpiResolve = resolve;
      });
    }
  },
  Serial: {
    print: (message: string) => ctx.postMessage({ type: 'serial', message })
  }
};

const runFirmware = async (source: string) => {
  const runner = new Function('api', `with(api){ return (async ()=>{${source}})(); }`);
  await runner(api);
};

ctx.onmessage = async (event: MessageEvent<FirmwareMessage>) => {
  if (event.data.type === 'run') {
    await runFirmware(event.data.source);
  }
  if (event.data.type === 'spi-response' && pendingSpiResolve) {
    pendingSpiResolve(event.data.response);
    pendingSpiResolve = null;
  }
};
