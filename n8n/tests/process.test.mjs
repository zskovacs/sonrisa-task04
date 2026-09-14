import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { runInNewContext } from 'node:vm';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
const run = (name, items) => JSON.parse(JSON.stringify(runInNewContext(
  `(() => { ${read(`../runtime/${name}.js`)} })()`,
  { $input: { all: () => items.map(json => ({ json })), first: () => ({ json: items[0] }) } }
).map(item => item.json)));
const fixture = JSON.parse(read('../fixtures/earthquakes.json'));
const alert = (overrides = {}) => ({ id: '22222222-2222-4222-8222-222222222222', enabled: true, event_type: 'earthquake', condition_field: 'magnitude', condition_operator: 'gte', condition_value_type: 'number', condition_value: 5, slack_destination: 'C123ABC', email_destination: 'p@example.test', ...overrides });
const event = (magnitude, external_id = 'test') => ({ contract_version: 1, source: 'demo.usgs', external_id, event_type: 'earthquake', occurred_at: '2026-09-14T16:00:00.000Z', title: '<@U123> & coast', source_url: null, data: { magnitude } });

test('one normalized feed yields three canonical events and one diagnostic envelope', () => {
  const normalized = run('normalize-earthquakes', [{ source: 'demo.usgs', feed: fixture }]);
  assert.equal(normalized.length, 1);
  assert.equal(normalized[0].events.length, 3);
  assert.deepEqual(normalized[0].events.map(x => x.data.magnitude), [4.9, 5, 5.1]);
});

test('evaluator consumes every SQL event envelope with threshold and owner expansion', () => {
  const other = alert({ id: '33333333-3333-4333-8333-333333333333', slack_destination: 'C999ABC', email_destination: null });
  const output = run('evaluate-alerts', [4.9, 5, 5.1].map((m, i) => ({ event: event(m, String(i)), alerts: [alert(), other] })));
  assert.equal(output.filter(x => x.channel === 'slack').length, 4);
  assert.equal(output.filter(x => x.channel === 'email').length, 2);
  assert.deepEqual(output.filter(x => x.channel === 'slack').map(x => x.event.external_id), ['1', '1', '2', '2']);
});

test('malformed event and config emit no notification; channel validation is independent', () => {
  const malformed = run('evaluate-alerts', [{ event: event('5'), alerts: [alert()] }]);
  assert.equal(malformed.filter(x => x.channel === 'slack').length, 0);
  const missingId = run('evaluate-alerts', [{ event: { ...event(5), external_id: undefined }, alerts: [alert()] }]);
  assert.equal(missingId.filter(x => x.channel === 'slack').length, 0);
  const output = run('evaluate-alerts', [{ event: event(5), alerts: [alert({ email_destination: 'a@example.test,evil@example.test' }), alert({ id: '33333333-3333-4333-8333-333333333333', slack_destination: '<@U1>', email_destination: 'safe@example.test' }), alert({ id: '44444444-4444-4444-8444-444444444444', enabled: false }), alert({ id: '55555555-5555-4555-8555-555555555555', condition_field: 'depth' })] }]);
  assert.equal(output.filter(x => x.channel === 'slack').length, 1);
  assert.equal(output.filter(x => x.channel === 'email').length, 1);
  assert.deepEqual(output.filter(x => x.channel === 'diagnostic').map(x => x.code), ['invalid_email_destination', 'invalid_slack_destination', 'unsupported_condition']);
  assert.equal(JSON.stringify(output.filter(x => x.channel === 'diagnostic')).includes('evil@example.test'), false);
});

test('Slack message escapes user text and omits raw destination', () => {
  const result = run('prepare-slack-message', [{ event: event(5), alert_id: alert().id, channel: 'slack', destination: 'C123ABC' }]);
  assert.match(result[0].text, /&lt;@U123&gt; &amp; coast/);
  assert.doesNotMatch(result[0].text, /C123ABC/);
});

test('process builder has no persistence or email transport', () => {
  const sdk = execFileSync(process.execPath, [new URL('../build-workflow.mjs', import.meta.url).pathname, 'process'], { encoding: 'utf8' });
  assert.match(sdk, /removeDuplicateInputItems/);
  assert.match(sdk, /removeItemsSeenInPreviousExecutions/);
  assert.match(sdk, /maxTries: 5/);
  assert.doesNotMatch(sdk, /notification_deliveries|source_events|emailSend|executeOnce: true|__[A-Z0-9_]+__/);
});
