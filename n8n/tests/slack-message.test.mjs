import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const code = readFileSync(new URL('../runtime/prepare-slack-message.js', import.meta.url), 'utf8');
const notification = (overrides = {}) => ({
  channel: 'slack',
  destination: 'C123ABC',
  alert_id: '22222222-2222-4222-8222-222222222222',
  alert_name: 'Coastal watch',
  event: {
    contract_version: 1,
    source: 'demo.usgs',
    external_id: 'event-1',
    event_type: 'earthquake',
    title: '<@U123> & coast',
    occurred_at: '2026-09-14T18:00:00+02:00',
    source_url: null,
    data: { magnitude: 5.1 },
  },
  ...overrides,
});
const run = values => JSON.parse(JSON.stringify(runInNewContext(
  `(() => { ${code} })()`,
  { $input: { all: () => values.map(json => ({ json })) } },
)));

test('Slack text renders a safe alert name and UTC timestamp without internal identifiers', () => {
  const [item] = run([notification({ alert_name: '  Coastal   <watch>  ' })]);

  assert.equal(item.json.destination, 'C123ABC');
  assert.match(item.json.text, /^\[SYNTHETIC\] Earthquake M 5\.1: &lt;@U123&gt; &amp; coast/m);
  assert.match(item.json.text, /^Occurred: 2026-09-14T16:00:00\.000Z$/m);
  assert.match(item.json.text, /^Alert: Coastal &lt;watch&gt;$/m);
  assert.doesNotMatch(item.json.text, /event-1|22222222|C123ABC/);
});

test('Slack preparation rejects missing, non-string and unparseable timestamps', () => {
  for (const occurred_at of [undefined, null, '', 'not-a-time', 123]) {
    const input = notification({ event: { ...notification().event, occurred_at } });
    assert.throws(() => run([input]), { message: 'invalid_slack_notification' });
  }
});

test('Slack preparation omits blank, non-string and control-containing alert names', () => {
  for (const alert_name of [null, 123, '   ', '\r\n<@U999>', '\u0085<@U999>']) {
    const [item] = run([notification({ alert_name })]);
    assert.doesNotMatch(item.json.text, /^Alert:/m);
    assert.doesNotMatch(item.json.text, /U999/);
  }
});

test('Slack preparation bounds a safe alert name to 120 characters', () => {
  const [item] = run([notification({ alert_name: 'a'.repeat(121) })]);
  const alertLine = item.json.text.split('\n').find(line => line.startsWith('Alert:'));

  assert.equal(alertLine, `Alert: ${'a'.repeat(120)}`);
});
