// Many watchers on one auction while bids arrive at a fixed rate. Measures how long it takes from
// the server accepting a bid to a watcher receiving it over the SignalR hub.
//
// Run with: docker compose --profile load-test run --rm k6
import exec from 'k6/execution';
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';
import { WebSocket } from 'k6/websockets';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const WS_URL = BASE_URL.replace(/^http/, 'ws');
const WATCHERS = Number(__ENV.WATCHERS || 2000);
const BIDS_PER_SECOND = Number(__ENV.BIDS_PER_SECOND || 200);
const DURATION_SECONDS = Number(__ENV.DURATION_SECONDS || 60);
const WATCHER_RAMP_SECONDS = 20;
const BIDDER_COUNT = 100;
const AUCTION_LENGTH_MS = 60 * 60 * 1000;
const MAX_SECONDS_TO_GO_LIVE = 30;
const STARTING_PRICE_IN_PAISE = 10_000;
// Larger than the biggest bid increment, so a bid that arrives in order is always high enough.
const BID_STEP_IN_PAISE = 20_000;

// SignalR JSON hub protocol: every message ends with the record separator character.
const RECORD_SEPARATOR = '\u001e';
const INVOCATION = 1;
const PING = 6;
const PING_INTERVAL_MS = 15_000;

const broadcastLatency = new Trend('bid_broadcast_latency', true);
const updatesReceived = new Counter('auction_updates_received');
const bidsAccepted = new Counter('bids_accepted');
const bidsRejected = new Counter('bids_rejected');

export const options = {
  setupTimeout: '2m',
  scenarios: {
    watchers: {
      executor: 'ramping-vus',
      exec: 'watch',
      startVUs: 0,
      stages: [
        { duration: `${WATCHER_RAMP_SECONDS}s`, target: WATCHERS },
        { duration: `${DURATION_SECONDS}s`, target: WATCHERS },
      ],
      gracefulRampDown: '5s',
    },
    bidders: {
      executor: 'constant-arrival-rate',
      exec: 'bid',
      startTime: `${WATCHER_RAMP_SECONDS}s`,
      rate: BIDS_PER_SECOND,
      timeUnit: '1s',
      duration: `${DURATION_SECONDS}s`,
      preAllocatedVUs: 100,
      maxVUs: 400,
    },
  },
  thresholds: {
    bid_broadcast_latency: ['p(95)<200'],
    'http_req_duration{scenario:bidders}': ['p(95)<200'],
  },
};

function devToken() {
  const response = http.post(`${BASE_URL}/api/auth/dev-token`, JSON.stringify({ role: 'User' }), {
    headers: { 'Content-Type': 'application/json' },
  });
  return response.json('accessToken');
}

function authorized(token) {
  return { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } };
}

export function setup() {
  const sellerToken = devToken();
  const startsAt = new Date();
  const endsAt = new Date(startsAt.getTime() + AUCTION_LENGTH_MS);
  const created = http.post(
    `${BASE_URL}/api/auctions`,
    JSON.stringify({
      title: 'Load test auction',
      description: '',
      startingPriceInPaise: STARTING_PRICE_IN_PAISE,
      startsAt: startsAt.toISOString(),
      endsAt: endsAt.toISOString(),
    }),
    authorized(sellerToken),
  );
  const auctionId = created.json('id');

  for (let second = 0; second < MAX_SECONDS_TO_GO_LIVE; second++) {
    if (http.get(`${BASE_URL}/api/auctions/${auctionId}`, authorized(sellerToken)).json('status') === 'Live') {
      break;
    }
    sleep(1);
  }

  const bidderTokens = Array.from({ length: BIDDER_COUNT }, devToken);
  return { auctionId, watcherToken: devToken(), bidderTokens };
}

export function watch({ auctionId, watcherToken }) {
  const socket = new WebSocket(`${WS_URL}/hubs/auctions?access_token=${watcherToken}`);
  let pinger;

  socket.onopen = () => {
    socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + RECORD_SEPARATOR);
    socket.send(
      JSON.stringify({ type: INVOCATION, invocationId: '1', target: 'Watch', arguments: [auctionId] }) +
        RECORD_SEPARATOR,
    );
    pinger = setInterval(() => socket.send(JSON.stringify({ type: PING }) + RECORD_SEPARATOR), PING_INTERVAL_MS);
  };

  socket.onmessage = (event) => {
    const receivedAt = Date.now();
    for (const frame of String(event.data).split(RECORD_SEPARATOR)) {
      if (!frame) {
        continue;
      }
      const message = JSON.parse(frame);
      if (message.type !== INVOCATION || message.target !== 'AuctionUpdated') {
        continue;
      }
      updatesReceived.add(1);
      for (const bid of message.arguments[0].bids) {
        broadcastLatency.add(receivedAt - Date.parse(bid.placedAt));
      }
    }
  };

  socket.onclose = () => clearInterval(pinger);

  // Stay connected for the rest of the test; the scenario's ramp-down closes the VU.
  setTimeout(() => socket.close(), (WATCHER_RAMP_SECONDS + DURATION_SECONDS) * 1000);
}

export function bid({ auctionId, bidderTokens }) {
  const iteration = exec.scenario.iterationInTest;
  const token = bidderTokens[iteration % bidderTokens.length];
  const amountInPaise = STARTING_PRICE_IN_PAISE + iteration * BID_STEP_IN_PAISE;

  // A bid that arrives after a higher one is rejected as too low; that is correct behaviour.
  const response = http.post(
    `${BASE_URL}/api/auctions/${auctionId}/bids`,
    JSON.stringify({ amountInPaise }),
    { ...authorized(token), responseCallback: http.expectedStatuses(200, 409, 422) },
  );

  check(response, { 'bid answered': (r) => [200, 409, 422].includes(r.status) });
  (response.status === 200 ? bidsAccepted : bidsRejected).add(1);
}
