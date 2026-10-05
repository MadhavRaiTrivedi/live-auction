# Live Auction: Requirements

## 1. Problem statement

An online auction is only fair if every bidder sees the same current price, two bids arriving at the same moment are ordered consistently, and nobody can bid after the auction ends. A page that saves bids with a plain insert and makes users refresh cannot guarantee any of that during the final seconds of a busy auction.

Live Auction is a backend and web app that accepts bids, decides the leading bid under concurrent load, pushes every change to connected browsers in real time, and closes auctions on time with the server as the only clock that matters.

## 2. Actors

| Actor | Description |
|---|---|
| Seller | Creates auctions, edits them before they start, cancels them if no bids have been placed. |
| Bidder | Watches live auctions, places manual bids, sets a maximum for automatic bidding. |
| Admin | Cancels any scheduled or live auction. |
| System | Background jobs: starting scheduled auctions, closing ended auctions. |

A single user account can act as both seller and bidder, but never bids on its own auction.

## 3. Core concepts

- **Auction**: one item for sale, with a starting price, an optional reserve price, a start time and an end time.
- **Bid**: an offer of an amount by a bidder on an auction. Accepted bids are never edited or deleted.
- **Current price**: the amount of the leading bid, or the starting price if there are no bids.
- **Bid increment**: the minimum amount a new bid must exceed the current price by. It grows in tiers as the price grows.
- **Proxy bid**: the maximum amount a bidder is willing to pay. The system bids on their behalf, one increment at a time, up to that maximum.
- **Reserve price**: a hidden minimum. If the highest bid is below it when the auction ends, the item is not sold.
- **Soft close (anti-sniping)**: a bid placed in the last few seconds pushes the end time back, so other bidders get a chance to respond.
- **Watcher**: a browser connected to an auction's live updates.

## 4. Functional requirements

Priority uses MoSCoW: **M**ust, **S**hould, **C**ould.

### Auctions

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | A seller can create an auction with title, description, starting price, start time and end time. | M |
| FR-02 | A seller can set an optional reserve price, hidden from bidders. Bidders see only whether the reserve has been met. | S |
| FR-03 | A seller can upload images for an auction. | C |
| FR-04 | A seller can edit an auction until it goes live. After that, only an admin can change it, and only by cancelling it. | M |
| FR-05 | Any signed-in user can browse live and upcoming auctions, and search them by title. | M |

### Auction lifecycle

| ID | Requirement | Priority |
|---|---|---|
| FR-06 | An auction has a status: `Scheduled`, `Live`, `Sold`, `Unsold` or `Cancelled`. | M |
| FR-07 | Allowed transitions: `Scheduled → Live`, `Scheduled → Cancelled`, `Live → Sold`, `Live → Unsold`, `Live → Cancelled`. Any other transition is rejected. | M |
| FR-08 | A background job moves auctions to `Live` at their start time and closes them at their end time. An auction closes as `Sold` if it has a bid at or above the reserve, otherwise `Unsold`. | M |
| FR-09 | A seller can cancel a live auction only while it has no bids. An admin can cancel any live auction. | S |

### Bidding

| ID | Requirement | Priority |
|---|---|---|
| FR-10 | A bidder can place a bid on a `Live` auction. The bid is rejected if it is below current price plus increment, if the auction has ended, or if the bidder is the seller. | M |
| FR-11 | Bid increments come from a tier table in configuration (for example ₹10 up to ₹1,000, ₹50 up to ₹10,000, ₹100 above). | M |
| FR-12 | When two bids arrive at the same time, exactly one becomes the leader. The other is re-checked against the new price and accepted or rejected with the reason. On equal amounts the earlier bid keeps the lead. | M |
| FR-13 | **Proxy bidding**: a bidder can set a maximum. The system keeps them in the lead with the smallest bid needed, up to that maximum. When two proxies compete, the higher maximum wins at one increment above the lower maximum. | S |
| FR-14 | **Soft close**: a bid accepted in the last 30 seconds extends the end time to 30 seconds after that bid. Both values come from configuration. | S |
| FR-15 | A bidder can view an auction's full bid history, with bidder names masked. | M |

### Real-time updates

| ID | Requirement | Priority |
|---|---|---|
| FR-16 | Every accepted bid is pushed to all watchers of that auction: new price, leading bidder (masked), new end time if extended, bid count. | M |
| FR-17 | Status changes (`Live`, `Sold`, `Unsold`, `Cancelled`) are pushed to watchers. | M |
| FR-18 | A bidder who loses the lead receives a private outbid notification on any page of the app. | S |
| FR-19 | The countdown shown in the browser is based on server time. The client corrects for the difference between its clock and the server's. | M |
| FR-20 | A browser that reconnects after a dropped connection receives the current state of the auction, not just updates sent after reconnecting. | M |

### History

| ID | Requirement | Priority |
|---|---|---|
| FR-21 | A bidder can see the auctions they have bid on, won and lost. | S |
| FR-22 | A seller can see their auctions with final price and winner. | S |

## 5. Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-01 | **Bid consistency**: under any number of concurrent bids on one auction, accepted bids get a gap-free sequence number, their amounts never decrease, and the leading bid is always the highest accepted one (the earliest on a tie). No accepted bid is lost. |
| NFR-02 | **Server-owned time**: whether a bid is in time is decided by the server timestamp at acceptance, never by the client. A bid after the end time is rejected even if the close job has not run yet. |
| NFR-03 | **Exactly-once close**: an auction is closed once, even with several API instances running the close job. |
| NFR-04 | **Immutability**: accepted bids are never updated or deleted. |
| NFR-05 | **Money representation**: amounts are stored as integer minor units (paise). INR only. |
| NFR-06 | **Scale-out**: the API runs as two or more instances behind a load balancer. A watcher connected to any instance receives every update for its auction (SignalR Redis backplane). |
| NFR-07 | **Performance target**: 2,000 connected watchers on a single auction and 200 bids per second, with p95 time from bid accepted to update received under 200 ms on a single developer machine, verified with a load test. |
| NFR-08 | **Abuse limits**: bids are rate-limited per user. Limits come from configuration. |
| NFR-09 | **Observability**: structured logs with correlation IDs, metrics for bids per second, rejected bids by reason, connected watchers and broadcast latency. |
| NFR-10 | **Security**: JWT authentication. Hub connections are authenticated. Users can edit only their own auctions. |
| NFR-11 | **Testability**: domain rules (increments, proxy bidding, soft close, transitions) covered by unit tests; bidding under concurrency, closing and hub broadcasts covered by integration tests against real PostgreSQL and Redis (Testcontainers). |
| NFR-12 | **Local setup**: the whole system, including two API instances and a load balancer, runs with `docker compose up`. |

## 6. Out of scope

- Payment and checkout. The winner and final price are recorded; collecting money is a separate concern (it could place a hold through the Payment Ledger project later).
- Shipping, returns, disputes and seller ratings.
- Auction types other than English ascending (Dutch, sealed-bid, buy-it-now).
- Email, SMS and mobile push notifications. Notifications are in-app only.
- Full identity provider (OAuth, SSO). Tokens are issued by a simple development endpoint.
- Multiple currencies.

## 7. Glossary

| Term | Meaning |
|---|---|
| English auction | Open ascending auction: bidders see the current price and outbid each other. |
| Increment | Minimum step between the current price and the next valid bid. |
| Proxy bid | A hidden maximum the system bids up to on the bidder's behalf. |
| Reserve price | Hidden minimum below which the seller is not obliged to sell. |
| Sniping | Bidding in the final seconds so others have no time to respond. |
| Soft close | Extending the end time when a bid arrives near the end, to defeat sniping. |
| SignalR hub | The server endpoint browsers connect to for real-time messages, over WebSockets with fallbacks. |
| Backplane | A shared channel (Redis pub/sub) that forwards hub messages between API instances. |
| Optimistic concurrency | Writing only if the row has not changed since it was read, and retrying if it has. |

## 8. Decisions taken

These defaults were chosen to keep scope realistic.

| Decision | Choice | Reason |
|---|---|---|
| Concurrent bids | Optimistic concurrency on the auction row (PostgreSQL `xmin`), retry on conflict | Payment Ledger uses pessimistic locks; this project shows the other approach. Bids are short single-row writes, and a conflict is a natural moment to re-check the bid against the new price. |
| Closing auctions | Polling job using `FOR UPDATE SKIP LOCKED`, hosted in every API instance | Safe with several instances, survives restarts, no in-memory timers to lose. NFR-02 makes bid validity independent of job lag. Running it inside the API means status changes go out through the same SignalR hub, with no separate worker process to wire into the backplane. |
| Real-time transport | ASP.NET Core SignalR with Redis backplane | Built into .NET, handles reconnects, and the backplane is the standard way to scale it out. |
| Placing bids | HTTP `POST`, not a hub method | Bids get ProblemDetails errors, rate limiting and normal HTTP tests. The hub only pushes updates out. |
| Source of truth | PostgreSQL for auctions and bids; Redis only for the backplane | One place to reason about consistency. Redis state can be lost without losing a bid. |
| Message broker | None | Payment Ledger already covers RabbitMQ and the outbox. In-process events are enough until something outside the API needs them. |
| Proxy bidding, soft close | Should-have, built after manual bidding works | Strong interview topics, but the core flow comes first. |
| Auth | Simple JWT, one user role plus `Admin` | Enough to show hub authentication and ownership rules. |
| Frontend | Angular | Same stack as Payment Ledger's dashboard. |
