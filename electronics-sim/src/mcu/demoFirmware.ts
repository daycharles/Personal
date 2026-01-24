export const demoFirmware = `
const ssPin = 'D3';
const ledPin = 'D4';

pinMode(ledPin, 'output');
Serial.print('Booting firmware...');

while (true) {
  const response = await SPI.transfer([0xaa, 0x00, 0x00, 0x00, 0x00]);
  if (response[0] === 0xaa) {
    const uid = response.slice(1).map((b) => b.toString(16).padStart(2, '0')).join(':');
    Serial.print('UID=' + uid);
    digitalWrite(ledPin, 1);
  } else {
    digitalWrite(ledPin, 0);
  }
  await delay(500);
}
`;
