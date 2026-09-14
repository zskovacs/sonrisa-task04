import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';
import { sanitizeWorkflow } from '../export-workflow.mjs';

const source = readFileSync(new URL('../sdk/deliver.sdk.js', import.meta.url), 'utf8');
const rejectionNode = source.split('const knownRejection =')[1]?.split('const sent =')[0];
const expression = rejectionNode?.match(/leftValue: expr\('(\{\{[^']+\}\})'\)/)?.[1];
assert.ok(expression, 'SDK known-rejection IF expression must be present');
const classify = error => runInNewContext(expression.slice(2, -2), { $json: { error } });

test('known native Slack rejections are classified as failed', () => {
  for (const error of [
    'Your Slack credential is missing required Oauth Scopes',
    'Slack error response: "channel_not_found"',
    'Slack error response: "invalid_auth"',
    { code: 'missing_scope' },
    { code: 'not_in_channel' },
  ]) assert.equal(classify(error), true, JSON.stringify(error));
});

test('ambiguous and misleading Slack errors remain unknown', () => {
  for (const error of [
    'ETIMEDOUT',
    { code: 'ETIMEDOUT' },
    'Your Slack credential is missing required Oauth Scopes; request timed out',
    'Slack error response: "channel_not_found"; request timed out',
    'channel_not_found',
    'Slack error response: "ratelimited"',
    {},
    null,
    42,
  ]) assert.equal(classify(error), false, JSON.stringify(error));
});

test('exporter accepts the SDK rejection condition and rejects expression drift', () => {
  const workflow = JSON.parse(readFileSync(new URL('../workflows/deliver.json', import.meta.url), 'utf8'));
  const condition = workflow.nodes.find(node => node.name === 'Known Slack rejection?').parameters.conditions.conditions[0];
  condition.leftValue = `=${expression}`;
  assert.doesNotThrow(() => sanitizeWorkflow(workflow, 'deliver'));
  condition.leftValue = '={{ $json.error?.code === "channel_not_found" }}';
  assert.throws(() => sanitizeWorkflow(workflow, 'deliver'), /Slack rejection expression drift/);
});
