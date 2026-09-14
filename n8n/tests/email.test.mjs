import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const read = name => readFileSync(new URL(`../runtime/${name}.js`, import.meta.url), 'utf8');
const notification = (overrides = {}) => ({
  channel: 'email', destination: 'owner@example.test', alert_id: '22222222-2222-4222-8222-222222222222',
  alert_name: 'Coastal watch',
  event: { contract_version: 1, source: 'demo.usgs', external_id: 'event-1', event_type: 'earthquake',
    title: 'Offshore quake', occurred_at: '2026-09-14T16:00:00.000Z', source_url: 'https://example.test/event', data: { magnitude: 5.1 } },
  ...overrides,
});
const run = (name, values, origin = notification()) => JSON.parse(JSON.stringify(runInNewContext(
  `(() => { ${read(name)} })()`,
  { $input: { all: () => values.map(json => ({ json })) }, $: () => ({ item: { json: origin } }) },
)));

test('Email preparation makes bounded user-facing plain text with valid alert name and source URL', () => {
  const [item] = run('prepare-email-message', [notification()]);
  assert.equal(item.json.destination, 'owner@example.test');
  assert.equal(item.json.subject, '[SYNTHETIC] Sonrisa alert: Offshore quake');
  assert.match(item.json.text, /\[SYNTHETIC\]/);
  assert.match(item.json.text, /Coastal watch/);
  assert.match(item.json.text, /M 5\.1/);
  assert.match(item.json.text, /2026-09-14T16:00:00\.000Z/);
  assert.match(item.json.text, /https:\/\/example\.test\/event/);
  assert.match(item.json.text, /Source: USGS \(synthetic test\)/);
  assert.doesNotMatch(item.json.subject + item.json.text, /22222222|event-1|owner@example\.test/);
  assert.deepEqual(item.pairedItem, { item: 0 });
});

test('Email preparation strips controls from untrusted display fields and omits unsafe optional values', () => {
  const input = notification({ alert_name: '\r\nBcc: bad@example.test', event: { ...notification().event, title: 'Quake\r\nBcc: bad@example.test', source_url: 'javascript:alert(1)' } });
  const [item] = run('prepare-email-message', [input]);
  assert.doesNotMatch(item.json.subject, /[\r\n]/);
  assert.doesNotMatch(item.json.text, /javascript:/);
  assert.doesNotMatch(item.json.text, /Alert: Bcc:/);
  assert.ok(item.json.subject.length <= 180);
});

test('Email preparation keeps a valid message when alert name is missing', () => {
  const [item] = run('prepare-email-message', [notification({ alert_name: null })]);
  assert.doesNotMatch(item.json.text, /Alert:/);
  assert.match(item.json.text, /Earthquake: M 5\.1/);
});

test('Email preparation omits malformed HTTP authorities', () => {
  for (const source_url of ['https://./x', 'https://-bad.test/x', 'https://ok.test:99999/x', 'https://user@ok.test/x']) {
    const [item] = run('prepare-email-message', [notification({ event: { ...notification().event, source_url } })]);
    assert.doesNotMatch(item.json.text, /Event URL:/);
  }
});

test('Email preparation rejects invalid single-recipient and malformed notification with a fixed error', () => {
  for (const value of ['owner@example.test,evil@example.test', 'Owner <owner@example.test>', 'owner@example.test\r\nBcc:evil@example.test', '', '.owner@example.test', 'owner.@example.test', 'owner@-example.test', 'owner@example-.test']) {
    assert.throws(() => run('prepare-email-message', [notification({ destination: value })]), { message: 'invalid_email_notification' });
  }
  assert.throws(() => run('prepare-email-message', [notification({ channel: 'slack' })]), { message: 'invalid_email_notification' });
  assert.throws(() => run('prepare-email-message', [notification({ event: { ...notification().event, data: { magnitude: '5' } } })]), { message: 'invalid_email_notification' });
});

test('Email result accepts exactly one recipient and matching optional envelope', () => {
  const [item] = run('record-email-result', [{ accepted: ['owner@example.test'], rejected: [], envelope: { to: ['owner@example.test'] } }]);
  assert.deepEqual(item.json, { channel: 'diagnostic', code: 'email_accepted', source: 'demo.usgs', external_id: 'event-1', alert_id: notification().alert_id });
  assert.deepEqual(item.pairedItem, { item: 0 });
});

test('Email result discards ambiguous, rejected and error-shaped responses without leaking addresses or raw errors', () => {
  for (const result of [
    { accepted: [], rejected: [] }, { accepted: ['owner@example.test', 'other@example.test'], rejected: [] },
    { accepted: ['owner@example.test'], rejected: ['other@example.test'] },
    { accepted: ['owner@example.test'], rejected: [], envelope: { to: ['other@example.test'] } },
    { accepted: ['owner@example.test'], rejected: [], error: 'owner@example.test secret' }, {},
    { accepted: [undefined], rejected: [] },
  ]) {
    const [item] = run('record-email-result', [result]);
    assert.equal(item.json.code, 'email_outcome_unconfirmed_discarded');
    assert.doesNotMatch(JSON.stringify(item.json), /owner@example|other@example|secret/);
  }
  assert.equal(run('record-email-result', [{ accepted: [undefined], rejected: [] }], notification({ destination: undefined }))[0].json.code, 'email_outcome_unconfirmed_discarded');
});
