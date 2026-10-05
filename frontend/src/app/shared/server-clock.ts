import { Injectable, signal } from '@angular/core';

// Countdowns must follow the server's clock, not the browser's, because the server decides when
// an auction ends. Every response carries the server time; the difference is kept as an offset.
@Injectable({ providedIn: 'root' })
export class ServerClock {
  private readonly offsetMs = signal(0);

  sync(serverTime: string): void {
    this.offsetMs.set(Date.parse(serverTime) - Date.now());
  }

  now(): number {
    return Date.now() + this.offsetMs();
  }
}
