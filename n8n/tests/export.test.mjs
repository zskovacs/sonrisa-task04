import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { sanitizeWorkflow } from '../export-workflow.mjs';

const tracked = kind => JSON.parse(readFileSync(new URL(`../workflows/${kind}.json`, import.meta.url), 'utf8'));
const snapshot = kind => {
  const result = tracked(kind);
  result.id = 'synthetic-workflow-id';
  result.activeVersion = { id: 'synthetic-history-id' };
  result.createdAt = '2000-01-01T00:00:00Z';
  result.parentFolderId = 'synthetic-project-id';
  result.pinData = { synthetic: [{ json: { value: 'discard' } }] };
  result.meta = { synthetic: true };
  result.settings.availableInMCP = true;
  const credentialed = result.nodes.find(n => n.type === 'n8n-nodes-base.postgres' || n.type === 'n8n-nodes-base.slack');
  credentialed.credentials = { synthetic: { id: 'synthetic-credential-id', name: 'discard' } };
  const slack = result.nodes.find(n => n.type === 'n8n-nodes-base.slack');
  if (slack) slack.webhookId = 'synthetic-webhook-id';
  return result;
};
const assignment = (workflow, nodeName, field) => workflow.nodes.find(n => n.name === nodeName).parameters.assignments.assignments.find(a => a.name === field).value;
const children = (workflow, name, output = 0) => (workflow.connections[name]?.main?.[output] ?? []).map(edge => edge.node);

for (const kind of ['ingest', 'evaluate', 'deliver']) {
  test(`${kind} export preserves checked graph and exact editable source`, () => {
    const original = snapshot(kind);
    const exported = sanitizeWorkflow(original, kind);
    assert.equal(exported.active, false);
    assert.deepEqual(Object.keys(exported).sort(), ['active', 'connections', 'description', 'name', 'nodes', 'settings']);
    assert.deepEqual(exported.connections, original.connections);
    assert.equal(exported.nodes.length, original.nodes.length);
    assert.deepEqual(exported.nodes.map(n => n.position), original.nodes.map(n => n.position));
    assert.deepEqual(exported, tracked(kind));
    assert.equal(exported.settings.executionOrder, original.settings.executionOrder);
    assert.equal(JSON.stringify(exported).includes('credentials'), false);
    assert.equal(JSON.stringify(exported).includes('pinData'), false);
    assert.equal(JSON.stringify(exported).includes('activeVersion'), false);
    assert.equal(JSON.stringify(exported).includes('createdAt'), false);
    for (const node of exported.nodes) {
      assert.equal('webhookId' in node, false);
      assert.equal('credentials' in node, false);
    }
  });
}

test('operator inputs are inert and future live fetch stays separate from fixture branch', () => {
  const ingest = sanitizeWorkflow(snapshot('ingest'), 'ingest');
  const deliver = sanitizeWorkflow(snapshot('deliver'), 'deliver');
  assert.equal(assignment(ingest, 'Operator ingest input', 'mode'), 'live');
  assert.equal(assignment(ingest, 'Operator ingest input', 'fixture_json'), '');
  assert.equal(assignment(deliver, 'Operator delivery ID', 'delivery_id'), '');
  assert.deepEqual(children(ingest, 'Live USGS input?', 0), ['Fetch USGS all-hour feed']);
  assert.deepEqual(children(ingest, 'Live USGS input?', 1), ['Normalize earthquake records']);
  assert.deepEqual(children(ingest, 'Fetch USGS all-hour feed'), ['Mark live USGS source']);
  assert.equal(ingest.nodes.some(n => n.type === 'n8n-nodes-base.scheduleTrigger' || n.type === 'n8n-nodes-base.webhook' || n.type === 'n8n-nodes-base.formTrigger'), false);
});

test('delivery graph has explicit claim gate before the only Slack send', () => {
  const deliver = sanitizeWorkflow(snapshot('deliver'), 'deliver');
  const chain = ['Manual selected delivery', 'Operator delivery ID', 'Require explicit delivery UUID', 'Claim one pending Slack delivery', 'Read immutable event message', 'Prepare safe Slack text', 'Send selected Slack notification'];
  for (let i = 0; i < chain.length - 1; i++) assert.deepEqual(children(deliver, chain[i]), [chain[i + 1]]);
  assert.deepEqual(children(deliver, 'Send selected Slack notification', 0), ['Slack acknowledged send?']);
  assert.deepEqual(children(deliver, 'Send selected Slack notification', 1), ['Known Slack rejection?']);
  const send = deliver.nodes.find(n => n.name === 'Send selected Slack notification');
  assert.equal(send.retryOnFail ?? false, false);
  assert.equal(send.onError, 'continueErrorOutput');
  assert.deepEqual(send.parameters.otherOptions, { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false });
});

test('sanitizer fails closed on active, temporary input, source drift and graph bypass', () => {
  const active = snapshot('ingest'); active.active = true;
  assert.throws(() => sanitizeWorkflow(active, 'ingest'), /inactive/);
  const fixture = snapshot('ingest'); fixture.nodes.find(n => n.name === 'Operator ingest input').parameters.assignments.assignments[0].value = 'fixture';
  assert.throws(() => sanitizeWorkflow(fixture, 'ingest'), /operator defaults/);
  const code = snapshot('evaluate'); code.nodes.find(n => n.name === 'Evaluate typed magnitude alerts').parameters.jsCode += '\n// changed';
  assert.throws(() => sanitizeWorkflow(code, 'evaluate'), /source drift/);
  const bypass = snapshot('deliver'); bypass.connections['Operator delivery ID'].main[0][0].node = 'Send selected Slack notification';
  assert.throws(() => sanitizeWorkflow(bypass, 'deliver'), /connection/);
  const binding = snapshot('evaluate'); binding.nodes.find(n => n.name === 'Insert prepared delivery intents').parameters.options.queryReplacement = '={{ $json.unsafe }}';
  assert.throws(() => sanitizeWorkflow(binding, 'evaluate'), /query binding/);
});

test('CLI output matches checked snapshot and tracked export', () => {
  const temp = mkdtempSync(join(tmpdir(), 'sonrisa-export-'));
  try {
    for (const kind of ['ingest', 'evaluate', 'deliver']) {
      const path = join(temp, `${kind}.json`);
      writeFileSync(path, JSON.stringify(snapshot(kind)));
      const output = execFileSync(process.execPath, [new URL('../export-workflow.mjs', import.meta.url).pathname, kind, path], { encoding: 'utf8' });
      assert.deepEqual(JSON.parse(output), tracked(kind));
    }
    const stdinOutput = execFileSync(process.execPath, [new URL('../export-workflow.mjs', import.meta.url).pathname, 'ingest', '-'], { input: JSON.stringify(snapshot('ingest')), encoding: 'utf8' });
    assert.deepEqual(JSON.parse(stdinOutput), tracked('ingest'));
  } finally {
    rmSync(temp, { recursive: true, force: true });
  }
});
