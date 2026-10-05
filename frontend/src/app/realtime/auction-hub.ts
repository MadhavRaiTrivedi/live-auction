import { inject, Injectable, signal } from '@angular/core';
import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { Session } from '../auth/session';
import { AuctionUpdate, OutbidNotice } from '../auctions/auction.model';
import { ConnectionStatus } from './connection-status';

const HUB_URL = '/hubs/auctions';
const WATCH_METHOD = 'Watch';
const UNWATCH_METHOD = 'Unwatch';
const AUCTION_UPDATED_MESSAGE = 'AuctionUpdated';
const OUTBID_MESSAGE = 'Outbid';
const MAX_OUTBID_NOTICES = 5;

@Injectable({ providedIn: 'root' })
export class AuctionHub {
  private readonly session = inject(Session);
  private readonly watched = new Map<string, Subject<AuctionUpdate>>();
  private connection: HubConnection | null = null;
  private starting: Promise<void> | null = null;

  readonly status = signal(ConnectionStatus.Disconnected);
  readonly outbidNotices = signal<OutbidNotice[]>([]);

  // Emits the current state first, then every change. After a reconnect the auction is watched
  // again and a fresh snapshot is emitted, so nothing missed while offline is lost.
  watch(auctionId: string): Observable<AuctionUpdate> {
    return new Observable<AuctionUpdate>((subscriber) => {
      const updates = new Subject<AuctionUpdate>();
      const subscription = updates.subscribe(subscriber);
      this.watched.set(auctionId, updates);
      this.joinAuction(auctionId).catch((error: unknown) => subscriber.error(error));

      return () => {
        subscription.unsubscribe();
        this.watched.delete(auctionId);
        // Leaving is best effort: the server drops a connection's groups when it closes anyway.
        this.connection?.invoke(UNWATCH_METHOD, auctionId).catch(() => undefined);
      };
    });
  }

  async connect(): Promise<void> {
    this.connection ??= this.buildConnection();
    this.starting ??= this.start(this.connection);
    await this.starting;
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.starting = null;
    this.outbidNotices.set([]);
    await connection?.stop();
  }

  dismissNotice(notice: OutbidNotice): void {
    this.outbidNotices.update((notices) => notices.filter((n) => n !== notice));
  }

  private async joinAuction(auctionId: string): Promise<void> {
    await this.connect();
    const snapshot = await this.connection!.invoke<AuctionUpdate>(WATCH_METHOD, auctionId);
    this.watched.get(auctionId)?.next(snapshot);
  }

  private async start(connection: HubConnection): Promise<void> {
    this.status.set(ConnectionStatus.Connecting);
    try {
      await connection.start();
      this.status.set(ConnectionStatus.Connected);
    } catch (error) {
      this.status.set(ConnectionStatus.Disconnected);
      this.starting = null;
      throw error;
    }
  }

  private buildConnection(): HubConnection {
    // WebSockets without the negotiate request: the connection goes straight to whichever API
    // instance the load balancer picks, so no sticky sessions are needed.
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => this.session.current()?.accessToken ?? '',
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on(AUCTION_UPDATED_MESSAGE, (update: AuctionUpdate) =>
      this.watched.get(update.auctionId)?.next(update),
    );
    connection.on(OUTBID_MESSAGE, (notice: OutbidNotice) =>
      this.outbidNotices.update((notices) => [notice, ...notices].slice(0, MAX_OUTBID_NOTICES)),
    );
    connection.onreconnecting(() => this.status.set(ConnectionStatus.Reconnecting));
    connection.onreconnected(() => {
      this.status.set(ConnectionStatus.Connected);
      this.watched.forEach((_, auctionId) =>
        this.joinAuction(auctionId).catch((error: unknown) =>
          this.watched.get(auctionId)?.error(error),
        ),
      );
    });
    connection.onclose(() => {
      this.status.set(ConnectionStatus.Disconnected);
      this.starting = null;
    });

    return connection;
  }
}
