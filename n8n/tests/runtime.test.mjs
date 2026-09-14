import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { runInNewContext } from 'node:vm';
import { execFileSync } from 'node:child_process';

const root = new URL('..', import.meta.url);
const file = (path) => readFileSync(new URL(path, root), 'utf8');
const fixture = JSON.parse(file('fixtures/earthquakes.json'));
const run = (name, json) => {
  const code = file(`runtime/${name}.js`);
  const result = runInNewContext(`(() => { ${code} })()`, { $input: { first: () => ({ json }) } });
  assert.equal(result.length, 1);
  return JSON.parse(JSON.stringify(result[0].json));
};

test('manual selector defaults to live and reserves fixture namespace', () => {
  assert.deepEqual(run('select-ingest-input', { mode: 'live', fixture_json: '' }), { mode: 'live' });
  const synthetic = run('select-ingest-input', { mode: 'fixture', fixture_json: JSON.stringify(fixture) });
  assert.equal(synthetic.source, 'demo.usgs');
  assert.equal(synthetic.feed.features.length, 3);
  assert.throws(() => run('select-ingest-input', { mode: 'other' }), /invalid_ingest_mode/);
  assert.throws(() => run('select-ingest-input', { mode: 'fixture', fixture_json: '' }), /invalid_fixture_feed/);
});

test('normalizer accepts finite numbers, bounds identity and isolates bad records', () => {
  const feed = structuredClone(fixture);
  feed.features.push({ type: 'Feature', id: 'bad-mag', properties: { type: 'earthquake', mag: '5', time: 1789392000000 } });
  feed.features.push({ id: 'bad-id with space', properties: { mag: 5, time: 1789392000000 } });
  feed.features.push({ type: 'Feature', id: 'bad-time', properties: { type: 'earthquake', mag: 5, time: null } });
  const out = run('normalize-earthquakes', { source: 'demo.usgs', feed });
  assert.equal(out.events.length, 3);
  assert.deepEqual(out.events.map(x => x.data.magnitude), [4.9, 5, 5.1]);
  assert.equal(out.events[0].contract_version, 1);
  assert.equal(out.events[0].title, 'M 4.9 - <@U123> & coast');
  assert.deepEqual(out.diagnostics.map(x => x.code), ['invalid_magnitude', 'invalid_external_id', 'invalid_occurrence']);
  assert.throws(() => run('normalize-earthquakes', { source: 'usgs', feed: { features: [] } }), /invalid_feed/);
});

test('normalizer rejects unsupported feature and out-of-range UTC without discarding valid records', () => {
  const feed = structuredClone(fixture);
  feed.features[0].properties.type = 'quarry blast';
  feed.features[1].properties.time = 253402300800000;
  feed.features[2].properties.title = 'M 5.1 - \u0085Safe';
  feed.features[2].properties.url = 'HTTPS://example.test/quake';
  const out = run('normalize-earthquakes', { source: 'usgs', feed });
  assert.equal(out.events.length, 1);
  assert.equal(out.events[0].title, 'M 5.1 -  Safe');
  assert.equal(out.events[0].source_url, null);
  assert.deepEqual(out.diagnostics.map(x => x.code), ['unsupported_feature', 'invalid_occurrence']);
});

test('normalizer omits invalid optional URL and rejects oversized feed', () => {
  const feed = structuredClone(fixture);
  feed.features[0].properties.url = 'javascript:alert(1)';
  assert.equal(run('normalize-earthquakes', { source: 'usgs', feed }).events[0].source_url, null);
  assert.throws(() => run('normalize-earthquakes', { source: 'usgs', feed: { type: 'FeatureCollection', features: Array(1001).fill(fixture.features[0]) } }), /feed_too_many_features/);
});

const event = (magnitude) => ({ id: '11111111-1111-4111-8111-111111111111', event_type: 'earthquake', data: { magnitude } });
const alert = (overrides = {}) => ({ id: '22222222-2222-4222-8222-222222222222', enabled: true, event_type: 'earthquake', condition_field: 'magnitude', condition_operator: 'gte', condition_value_type: 'number', condition_value: 5, email_destination: 'p@example.test', slack_destination: 'C123ABC', ...overrides });

test('evaluator matches equality and above, not below, with pending channel intents', () => {
  assert.deepEqual(run('evaluate-alerts', { event: event(4.9), alerts: [alert()] }).intents, []);
  for (const magnitude of [5, 5.1]) {
    const out = run('evaluate-alerts', { event: event(magnitude), alerts: [alert()] });
    assert.deepEqual(out.intents, [
      { alert_id: alert().id, channel: 'slack', destination: 'C123ABC', status: 'pending', last_error: null },
      { alert_id: alert().id, channel: 'email', destination: 'p@example.test', status: 'pending', last_error: null }
    ]);
  }
});

test('shared claim selects only successfully updated supported pending rows and reads channel', () => {
  const claim = file('sql/claim-delivery.sql');
  const read = file('sql/select-claimed-delivery.sql');
  assert.match(claim, /WITH claimed AS\s*\(\s*UPDATE public\.notification_deliveries/i);
  assert.match(claim, /WHERE id = \$1::uuid AND channel IN \('slack', 'email'\) AND status = 'pending'/i);
  assert.match(claim, /SELECT id, source_event_id, alert_id, channel, destination FROM claimed/i);
  assert.match(read, /SELECT d\.id, d\.source_event_id, d\.alert_id, d\.channel, d\.destination/i);
  assert.match(read, /d\.channel IN \('slack', 'email'\) AND d\.status = 'processing'/i);
});

test('email outcome queries guard channel and processing status', () => {
  for (const [name, expected] of [
    ['record-email-sent.sql', /status = 'sent', sent_at = now\(\), last_error = NULL/i],
    ['record-email-unknown.sql', /SET last_error = 'delivery_outcome_unknown'/i],
    ['record-email-invalid.sql', /status = 'failed', last_error = 'invalid_email_message'/i]
  ]) {
    const sql = file(`sql/${name}`);
    assert.match(sql, expected);
    assert.match(sql, /WHERE id = \$1::uuid AND channel = 'email' AND status = 'processing'/i);
    assert.match(sql, /RETURNING id/i);
  }
});

test('evaluator keeps event for no alerts, skips malformed configs and preserves other alerts', () => {
  assert.deepEqual(run('evaluate-alerts', { event: event(5), alerts: [] }), { event_id: event(5).id, intents: [], diagnostics: [] });
  const out = run('evaluate-alerts', { event: event(5), alerts: [alert({ enabled: false }), alert({ condition_field: 'depth' }), alert({ condition_value: '5' }), alert({ slack_destination: '<@U1>' }), alert({ id: '33333333-3333-4333-8333-333333333333', email_destination: null })] });
  assert.equal(out.intents.length, 1);
  assert.equal(out.intents[0].alert_id, '33333333-3333-4333-8333-333333333333');
  assert.deepEqual(out.diagnostics.map(x => x.code), ['unsupported_condition', 'invalid_threshold', 'invalid_slack_destination']);
  assert.deepEqual(run('evaluate-alerts', { event: event('5'), alerts: [alert()] }).intents, []);
});

test('delivery ID validation and immutable, plain-text message', () => {
  assert.throws(() => run('validate-delivery-id', { delivery_id: '' }), /invalid_delivery_id/);
  const valid = run('validate-delivery-id', { delivery_id: 'AAAAAAAA-AAAA-4AAA-8AAA-AAAAAAAAAAAA' });
  assert.equal(valid.delivery_id, 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');
  const msg = run('prepare-slack-message', { id: valid.delivery_id, source_event_id: event(5).id, alert_id: alert().id, source: 'demo.usgs', title: '<@U123> & <danger>', occurred_at: '2026-09-14T16:00:00Z', data: { magnitude: 5 }, destination: 'C123ABC' });
  assert.match(msg.text, /&lt;@U123&gt; &amp; &lt;danger&gt;/);
  assert.match(msg.text, /demo\.usgs/);
  assert.doesNotMatch(msg.text, /example\.test/);
});

test('SQL only expands prepared values and preserves completion on empty intent arrays', () => {
  const source = file('sql/insert-source-events.sql');
  const candidate = file('sql/select-event-candidates.sql');
  const intents = file('sql/insert-delivery-intents.sql');
  const claim = file('sql/claim-delivery.sql');
  assert.match(source, /ON CONFLICT \(source, external_id\) DO NOTHING/i);
  assert.match(candidate, /COALESCE\s*\(\s*c\.alerts/i);
  assert.match(candidate, /jsonb_agg/i);
  assert.match(candidate, /FROM public\.alerts/i);
  assert.match(intents, /ON CONFLICT \(source_event_id, alert_id, channel\) DO NOTHING/i);
  assert.match(intents, /SELECT count\(\*\)/i);
  assert.match(claim, /status = 'pending'/);
  assert.match(claim, /id = \$1::uuid/);
});

test('generator defaults are inert and operator overrides stay isolated from future live fetch', () => {
  const build = (...args) => execFileSync(process.execPath, [new URL('../build-workflow.mjs', import.meta.url).pathname, ...args], { encoding: 'utf8' });
  const live = build('ingest');
  const synthetic = build('ingest', '--fixture', 'earthquakes.json');
  assert.match(live, /value: "live"/);
  assert.match(live, /value: ""/);
  assert.match(synthetic, /value: "fixture"/);
  assert.match(synthetic, /demo\.usgs/);
  assert.match(live, /\.onTrue\(fetch\.to\(liveEnvelope/);
  assert.match(live, /\.onFalse\(normalize\)/);
  assert.match(build('deliver'), /value: ""/);
  assert.match(build('deliver', '--delivery-id', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'), /value: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"/);
  assert.doesNotMatch(build('evaluate'), /__CODE_|__SQL_/);
});

test('stdin runner executes exact evaluator body for relational harness', () => {
  const output = execFileSync(process.execPath, [new URL('../run-code.mjs', import.meta.url).pathname, 'evaluate-alerts'], { input: JSON.stringify({ event: event(5), alerts: [alert()] }), encoding: 'utf8' });
  assert.equal(JSON.parse(output).intents.length, 2);
});
