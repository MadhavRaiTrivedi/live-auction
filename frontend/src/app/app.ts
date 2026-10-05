import { Component, effect, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Session } from './auth/session';
import { AuctionHub } from './realtime/auction-hub';
import { ConnectionStatus } from './realtime/connection-status';
import { PaisePipe } from './shared/paise.pipe';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, PaisePipe],
  templateUrl: './app.html',
})
export class App {
  private readonly router = inject(Router);
  protected readonly session = inject(Session);
  protected readonly hub = inject(AuctionHub);
  protected readonly ConnectionStatus = ConnectionStatus;

  // One connection per signed-in user, opened on any page, so outbid notices arrive wherever they are.
  constructor() {
    effect(() => {
      if (this.session.isSignedIn()) {
        this.hub
          .connect()
          .catch((error: unknown) => console.warn('Live updates unavailable', error));
      } else {
        void this.hub.disconnect();
      }
    });
  }

  protected async signOut(): Promise<void> {
    this.session.signOut();
    await this.router.navigate(['/sign-in']);
  }
}
