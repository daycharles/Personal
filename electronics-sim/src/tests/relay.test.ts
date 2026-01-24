import { describe, expect, it } from 'vitest';
import { relayCoilEnergized } from '../devices/relay';

describe('relay coil threshold', () => {
  it('energizes when voltage exceeds threshold', () => {
    expect(relayCoilEnergized(4, 3)).toBe(true);
    expect(relayCoilEnergized(2, 3)).toBe(false);
  });
});
