import { ProjectData } from '../types';

export const saveProject = (project: ProjectData) => {
  localStorage.setItem('electronics-sim-project', JSON.stringify(project));
};

export const loadProject = (): ProjectData | null => {
  const data = localStorage.getItem('electronics-sim-project');
  if (!data) return null;
  return JSON.parse(data) as ProjectData;
};
