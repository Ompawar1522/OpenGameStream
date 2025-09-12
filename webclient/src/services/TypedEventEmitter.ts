export default class TypedEventEmitter<TEvents extends Record<string, (...args: any[]) => void>> {
  private listeners: {
    [K in keyof TEvents]?: Set<TEvents[K]>;
  } = {};

  on<K extends keyof TEvents>(event: K, listener: TEvents[K]) {
    this.listeners[event] ??= new Set();
    this.listeners[event]!.add(listener);
  }

  off<K extends keyof TEvents>(event: K, listener: TEvents[K]) {
    this.listeners[event]?.delete(listener);
  }

  emit<K extends keyof TEvents>(event: K, ...args: Parameters<TEvents[K]>) {
    this.listeners[event]?.forEach((listener) => listener(...args));
  }
}