import { useState } from 'react';

export const TagControl = ({ onPresent, onRemove }: { onPresent: (uid: number[]) => void; onRemove: () => void }) => {
  const [uid, setUid] = useState('DE:AD:BE:EF');

  const handlePresent = () => {
    const bytes = uid.split(':').map((part) => Number.parseInt(part, 16)).filter((num) => !Number.isNaN(num));
    if (bytes.length === 4) {
      onPresent(bytes);
    }
  };

  return (
    <div className="tag-control">
      <div className="tag-header">RC522 Tag Simulator</div>
      <input value={uid} onChange={(event) => setUid(event.target.value)} className="tag-input" />
      <div className="tag-actions">
        <button onClick={handlePresent}>Present Tag</button>
        <button onClick={onRemove}>Remove Tag</button>
      </div>
    </div>
  );
};
