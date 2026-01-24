import { describe, expect, it } from 'vitest';
import { buildNetlist, hasNetConnection } from '../sim/netlist';
import { demoProject } from '../persistence/demoProject';

describe('netlist connectivity', () => {
  it('connects relay coil to voltage source net', () => {
    const netlist = buildNetlist(demoProject);
    expect(hasNetConnection(netlist, 'coil+', 'k1', 'coil+')).toBe(true);
    expect(hasNetConnection(netlist, 'vcc', 'v1', 'p')).toBe(true);
  });
});
