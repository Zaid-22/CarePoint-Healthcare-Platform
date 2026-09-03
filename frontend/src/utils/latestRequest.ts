export interface LatestRequestGuard {
  begin: () => number;
  isCurrent: (version: number) => boolean;
  invalidate: () => void;
}

export const createLatestRequestGuard = (): LatestRequestGuard => {
  let currentVersion = 0;
  return {
    begin: () => ++currentVersion,
    isCurrent: (version) => version === currentVersion,
    invalidate: () => { currentVersion += 1; },
  };
};
