import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { runInNewContext } from 'node:vm';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
const run = (name, json, prepared, sender = 'sonrisa@example.test') => {
  const nodes = {
    'Operator delivery ID': { sender_email: sender },
    'Prepare safe email message': prepared,
  };
  const result = runInNewContext(`(() => { ${read(`../runtime/${name}.js`)} })()`, {
    $input: { first: () => ({ json }) },
    $: nodeName => ({ first: () => ({ json: nodes[nodeName] }) }),
  });
  assert.equal(result.length, 1);
  return JSON.parse(JSON.stringify(result[0].json));
};
const id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const row = overrides => ({
  id, source_event_id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
  alert_id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', channel: 'email',
  destination: 'sonrisa.runtime@example.test', source: 'demo.usgs',
  title: 'M 5.1 - <coast> & near town', occurred_at: '2026-09-14T16:00:00Z',
  data: { magnitude: 5.1 }, ...overrides,
});

test('email preparation emits one safe text message with traceable IDs and synthetic marker', () => {
  const message = run('prepare-email-message', row());
  assert.equal(message.valid, true);
  assert.equal(message.id, id);
  assert.equal(message.sender, 'sonrisa@example.test');
  assert.equal(message.destination, 'sonrisa.runtime@example.test');
  assert.match(message.subject, /^\[SYNTHETIC\] Earthquake M 5\.1/);
  assert.match(message.subject, new RegExp(id));
  assert.match(message.text, /M 5\.1 - <coast> & near town/);
  assert.match(message.text, new RegExp(row().source_event_id));
  assert.match(message.text, new RegExp(row().alert_id));
  assert.match(message.text, new RegExp(id));
  assert.doesNotMatch(message.text, /<html|<body/i);
});

test('email preparation rejects address lists, display names, header breaks, and invalid sender without exposing values', () => {
  for (const destination of [
    'first@example.test,second@example.test', 'first@example.test;second@example.test',
    'Person <first@example.test>', 'first@example.test\r\nBcc: other@example.test',
    'group:first@example.test;', '"first"@example.test', 'first..last@example.test',
  ]) {
    const result = run('prepare-email-message', row({ destination }));
    assert.deepEqual(result, { id, valid: false });
  }
  assert.deepEqual(run('prepare-email-message', row(), undefined, 'sender@example.test\nBcc: bad@example.test'), { id, valid: false });
  assert.deepEqual(run('prepare-email-message', row({ title: 'unsafe\nSubject' })), { id, valid: false });
  assert.deepEqual(run('prepare-email-message', row({ occurred_at: '2026-09-14T16:00:00Z\nInjected' })), { id, valid: false });
});

test('SMTP acceptance requires the sole requested recipient and an empty rejection set', () => {
  const prepared = run('prepare-email-message', row());
  const good = { accepted: [prepared.destination], rejected: [], envelope: { from: prepared.sender, to: [prepared.destination] } };
  assert.deepEqual(run('validate-email-result', good, prepared), { id, accepted: true });
  for (const candidate of [
    { accepted: [], rejected: [], envelope: good.envelope },
    { accepted: [prepared.destination, 'other@example.test'], rejected: [], envelope: good.envelope },
    { accepted: [prepared.destination], rejected: ['other@example.test'], envelope: good.envelope },
    { accepted: [prepared.destination], rejected: [], envelope: { from: 'other@example.test', to: [prepared.destination] } },
    { accepted: [prepared.destination], rejected: [], envelope: { from: prepared.sender, to: ['other@example.test'] } },
    { accepted: [prepared.destination], rejected: [], envelope: good.envelope, error: 'SMTP timeout' },
    { accepted: 'sonrisa.runtime@example.test', rejected: [] },
    { error: { message: 'SMTP timeout' } },
  ]) assert.deepEqual(run('validate-email-result', candidate, prepared), { id, accepted: false });
  assert.deepEqual(run('validate-email-result', good, { id, valid: false, sender: prepared.sender, destination: prepared.destination }), { id, accepted: false });
});

test('deliver builder has shared claim, explicit channel guards and single SMTP transport', () => {
  const build = execFileSync(process.execPath, [new URL('../build-workflow.mjs', import.meta.url).pathname, 'deliver'], { encoding: 'utf8' });
  assert.doesNotMatch(build, /__[A-Z0-9_]+__/);
  assert.match(build, /Claim one pending delivery/);
  assert.match(build, /Slack channel\?/);
  assert.match(build, /Email channel\?/);
  assert.match(build, /Valid email message\?/);
  assert.match(build, /SMTP accepted recipient\?/);
  assert.match(build, /n8n-nodes-base\.emailSend/);
  assert.match(build, /emailFormat: 'text'/);
  assert.match(build, /appendAttribution: false/);
  assert.match(build, /retryOnFail: false/);
  assert.match(build, /onError: 'continueErrorOutput'/);
  assert.match(build, /Record invalid email message/);
  assert.match(build, /Record ambiguous email outcome/);
});
