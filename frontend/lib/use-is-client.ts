import { useSyncExternalStore } from "react";

const subscribe = () => () => {};

/**
 * Returns false during server rendering and hydration, true afterwards.
 * Replaces the `useEffect(() => setMounted(true), [])` pattern without
 * setting state inside an effect.
 */
export function useIsClient(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false
  );
}
