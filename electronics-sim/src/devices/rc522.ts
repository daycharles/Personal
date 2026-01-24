export type Rc522State = {
  uid: number[];
  present: boolean;
};

export class RC522Sim {
  state: Rc522State;

  constructor() {
    this.state = { uid: [0xde, 0xad, 0xbe, 0xef], present: false };
  }

  setTag(uid: number[]) {
    this.state.uid = uid;
    this.state.present = true;
  }

  clearTag() {
    this.state.present = false;
  }

  handleTransaction(bytes: number[]): number[] {
    if (!this.state.present) {
      return bytes.map(() => 0x00);
    }
    if (bytes[0] === 0xaa) {
      return [0xaa, ...this.state.uid];
    }
    return bytes.map(() => 0xff);
  }
}
