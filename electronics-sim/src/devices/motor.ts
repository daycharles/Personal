export const motorBackEmf = (speedRpm: number, kv: number) => {
  return (speedRpm / 60) * kv;
};
