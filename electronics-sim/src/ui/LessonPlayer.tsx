import { LessonStep } from '../types';

export const LessonPlayer = ({
  steps,
  currentStep,
  onNext,
  onPrev
}: {
  steps: LessonStep[];
  currentStep: number;
  onNext: () => void;
  onPrev: () => void;
}) => {
  const step = steps[currentStep];
  if (!step) return null;
  return (
    <div className="lesson-player">
      <div className="lesson-title">Guided Lesson</div>
      <h4>{step.title}</h4>
      <p>{step.body}</p>
      <div className="lesson-actions">
        <button onClick={onPrev} disabled={currentStep === 0}>
          Previous
        </button>
        <button onClick={onNext} disabled={currentStep === steps.length - 1}>
          Next
        </button>
      </div>
    </div>
  );
};
