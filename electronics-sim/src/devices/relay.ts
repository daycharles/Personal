export const relayCoilEnergized = (coilVoltage: number, thresholdVoltage: number) => {
  return coilVoltage >= thresholdVoltage;
};
