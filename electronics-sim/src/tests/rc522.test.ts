import { describe, expect, it } from 'vitest';
import { RC522Sim } from '../devices/rc522';

describe('RC522 present tag flow', () => {
  it('returns UID bytes on read command', () => {
    const rc522 = new RC522Sim();
    rc522.setTag([0x01, 0x02, 0x03, 0x04]);
    const response = rc522.handleTransaction([0xaa, 0x00, 0x00, 0x00, 0x00]);
    expect(response.slice(0, 5)).toEqual([0xaa, 0x01, 0x02, 0x03, 0x04]);
  });
});
