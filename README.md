# Live Auction

A real-time auction platform where bidders see every new bid within a fraction of a second, competing bids on the same item are resolved fairly, and auctions close at exactly the advertised time.

## Why I built it

A simple auction site saves bids with a plain insert and makes users refresh the page to see the current price. That breaks in the final seconds of a popular auction: two people bid at the same moment and both think they won, the page shows a price that is already outdated, and bids arrive after the auction should have closed.

I built this project to learn how real-time systems handle that: pushing updates to thousands of connected browsers over WebSockets, scaling those connections across several server instances, resolving concurrent writes to one hot row without locking everyone out, and running time-based state changes (auction close, anti-sniping extensions) reliably when the server, not the client, owns the clock.

## Status

Requirements are written. Implementation has not started yet.

- [Requirements](docs/requirements.md): scope, functional and non-functional requirements, and the decisions taken so far.

## Demo

Added once the frontend runs.

## Planned features

- Sellers create auctions with a starting price, optional reserve price and end time
- Bidders place bids and see the price, bid history and countdown update live without refreshing
- Bid increments that grow with the current price
- Automatic (proxy) bidding up to a maximum the bidder sets
- Anti-sniping: a bid in the last moments extends the auction
- Outbid notifications pushed to the bidder who lost the lead
- Auctions close on time on the server, and the winner is recorded
- Several API instances share live connections through a Redis backplane

## Architecture

Mermaid diagram and the path of one bid from the browser, through the API and database, back out to every watcher. Added with the first working bid flow.

## Planned stack

.NET 10, ASP.NET Core SignalR, PostgreSQL, Redis, Angular, Docker. Each choice will be explained in a tech stack table once it is in the code.

## Project structure

Added with the first projects in the solution.

## Key concepts and patterns

Added as each one lands in the code: optimistic concurrency on bids, the auction state machine, proxy bidding, the close worker, the SignalR backplane.

## Design decisions and tradeoffs

The decisions taken so far are listed in [requirements](docs/requirements.md#8-decisions-taken). They move here as they are implemented.

## Challenges and how I solved them

Added as they happen.

## Performance and benchmarks

Target: 2,000 connected watchers on one auction and 200 bids per second, with bid-to-broadcast p95 under 200 ms. Numbers added once the load test runs.

## Testing

Added with the first tests.

## Running locally

Added with `docker-compose.yml`.

## API reference

Added with the first endpoints.

## Interview notes

Added as the project takes shape.

## What I would do next

Added once the core flows work.
