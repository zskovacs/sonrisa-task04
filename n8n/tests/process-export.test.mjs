import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { sanitizeWorkflow } from '../export-workflow.mjs';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
const specs = [
  ['Manual alert processing', 'manualTrigger'], ['Operator feed input', 'set'], ['Select trusted feed path', 'code'],
  ['Use live USGS feed?', 'if'], ['Fetch USGS all-hour feed', 'httpRequest'], ['Mark live USGS source', 'set'],
  ['Normalize earthquake records', 'code'], ['Split canonical events', 'splitOut'], ['Deduplicate current feed', 'removeDuplicates'],
  ['Deduplicate previous executions', 'removeDuplicates'], ['Load owner alert configuration', 'postgres'],
  ['Evaluate typed magnitude alerts', 'code'], ['Process each notification', 'splitInBatches'],
  ['Route notification channel', 'switch'], ['Prepare safe Slack text', 'code'], ['Send Slack notification', 'slack'],
  ['Record accepted Slack diagnostic', 'set'], ['Record exhausted Slack diagnostic', 'set'],
  ['Record invalid Slack diagnostic', 'set'],
  ['Prepare safe Email text', 'code'], ['Send Email notification', 'emailSend'], ['Record Email result', 'code'],
  ['Record invalid Email diagnostic', 'set'], ['Record exhausted Email diagnostic', 'set'],
  ['Record unsupported channel diagnostic', 'set'], ['Complete manual processing', 'set'],
];
const assignment = (name, value) => ({ id: name, name, value, type: 'string' });
const set = (...values) => ({ mode: 'manual', includeOtherFields: false, assignments: { assignments: values.map(([name, value]) => assignment(name, value)) } });
const paired = [
  ['source', "={{ $('Process each notification').item.json.event.source }}"],
  ['external_id', "={{ $('Process each notification').item.json.event.external_id }}"],
  ['alert_id', "={{ $('Process each notification').item.json.alert_id }}"],
];
const fallback = [
  ['source', "={{ $json.event?.source ?? $json.source ?? '' }}"],
  ['external_id', "={{ $json.event?.external_id ?? $json.external_id ?? '' }}"],
  ['alert_id', "={{ $json.alert_id ?? '' }}"],
];
const code = name => ({ mode: 'runOnceForAllItems', language: 'javaScript', jsCode: read(`../runtime/${name}.js`) });
const routeCondition = value => ({ options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: '={{ $json.channel }}', rightValue: value, operator: { type: 'string', operation: 'equals' } }
], combinator: 'and' });
const params = {
  'Operator feed input': set(['mode', 'live'], ['fixture_json', '']),
  'Select trusted feed path': code('select-ingest-input'),
  'Use live USGS feed?': { conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
    { leftValue: '={{ $json.mode }}', rightValue: 'live', operator: { type: 'string', operation: 'equals' } }
  ], combinator: 'and' } },
  'Fetch USGS all-hour feed': { method: 'GET', authentication: 'none', url: 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson', options: { timeout: 20000, response: { response: { responseFormat: 'json' } } } },
  'Mark live USGS source': { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
    { id: 'live-source', name: 'source', value: 'usgs', type: 'string' },
    { id: 'live-feed', name: 'feed', value: '={{ $json }}', type: 'object' }
  ] } },
  'Normalize earthquake records': code('normalize-earthquakes'),
  'Split canonical events': { fieldToSplitOut: 'events', include: 'noOtherFields' },
  'Deduplicate current feed': { operation: 'removeDuplicateInputItems', compare: 'selectedFields', fieldsToCompare: 'source,external_id', options: { removeOtherFields: false } },
  'Deduplicate previous executions': { operation: 'removeItemsSeenInPreviousExecutions', logic: 'removeItemsWithAlreadySeenKeyValues', dedupeValue: '={{ $json.source + ":" + $json.external_id }}', options: { scope: 'node', historySize: 10000 } },
  'Load owner alert configuration': { resource: 'database', operation: 'executeQuery', query: read('../sql/select-enabled-alerts.sql'), options: { queryBatching: 'independently', queryReplacement: '={{ [JSON.stringify($json)] }}' } },
  'Evaluate typed magnitude alerts': code('evaluate-alerts'),
  'Process each notification': { batchSize: 1, options: { reset: false } },
  'Route notification channel': { mode: 'rules', rules: { values: [{ outputKey: 'Slack', conditions: routeCondition('slack') }, { outputKey: 'Email', conditions: routeCondition('email') }] }, options: { fallbackOutput: 'extra', renameFallbackOutput: 'Diagnostic or unsupported' } },
  'Prepare safe Slack text': code('prepare-slack-message'),
  'Prepare safe Email text': code('prepare-email-message'),
  'Send Email notification': { resource: 'email', operation: 'send', fromEmail: 'sonrisa@example.test', toEmail: '={{ $json.destination }}', subject: '={{ $json.subject }}', emailFormat: 'text', text: '={{ $json.text }}', options: { appendAttribution: false } },
  'Record Email result': code('record-email-result'),
  'Record invalid Email diagnostic': set(['channel', 'diagnostic'], ['code', 'invalid_email_notification_discarded'], ...paired),
  'Record exhausted Email diagnostic': set(['channel', 'diagnostic'], ['code', 'email_retries_exhausted_discarded'], ...paired),
  'Send Slack notification': { resource: 'message', operation: 'post', authentication: 'accessToken', select: 'channel', channelId: { __rl: true, mode: 'id', value: '={{ $json.destination }}' }, messageType: 'text', text: '={{ $json.text }}', otherOptions: { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false } },
  'Record accepted Slack diagnostic': set(['channel', 'diagnostic'], ['code', 'slack_accepted'], ...paired),
  'Record exhausted Slack diagnostic': set(['channel', 'diagnostic'], ['code', 'slack_retries_exhausted_discarded'], ...paired),
  'Record invalid Slack diagnostic': set(['channel', 'diagnostic'], ['code', 'invalid_slack_notification_discarded'], ...paired),
  'Record unsupported channel diagnostic': set(['channel', 'diagnostic'], ['code', "={{ $json.code ?? 'unsupported_channel' }}"], ...fallback),
  'Complete manual processing': set(['code', 'processing_complete']),
};
const edgeList = [
  ['Manual alert processing',0,'Operator feed input'], ['Operator feed input',0,'Select trusted feed path'], ['Select trusted feed path',0,'Use live USGS feed?'],
  ['Use live USGS feed?',0,'Fetch USGS all-hour feed'], ['Use live USGS feed?',1,'Normalize earthquake records'],
  ['Fetch USGS all-hour feed',0,'Mark live USGS source'], ['Mark live USGS source',0,'Normalize earthquake records'],
  ['Normalize earthquake records',0,'Split canonical events'], ['Split canonical events',0,'Deduplicate current feed'],
  ['Deduplicate current feed',0,'Deduplicate previous executions'], ['Deduplicate previous executions',0,'Load owner alert configuration'],
  ['Load owner alert configuration',0,'Evaluate typed magnitude alerts'], ['Evaluate typed magnitude alerts',0,'Process each notification'],
  ['Process each notification',0,'Complete manual processing'], ['Process each notification',1,'Route notification channel'],
  ['Route notification channel',0,'Prepare safe Slack text'], ['Route notification channel',1,'Prepare safe Email text'],
  ['Route notification channel',2,'Record unsupported channel diagnostic'], ['Prepare safe Slack text',0,'Send Slack notification'],
  ['Send Slack notification',0,'Record accepted Slack diagnostic'], ['Send Slack notification',1,'Record exhausted Slack diagnostic'],
  ['Prepare safe Slack text',1,'Record invalid Slack diagnostic'],
  ['Prepare safe Email text',0,'Send Email notification'], ['Prepare safe Email text',1,'Record invalid Email diagnostic'],
  ['Send Email notification',0,'Record Email result'], ['Send Email notification',1,'Record exhausted Email diagnostic'],
  ['Record accepted Slack diagnostic',0,'Process each notification'], ['Record exhausted Slack diagnostic',0,'Process each notification'],
  ['Record invalid Slack diagnostic',0,'Process each notification'],
  ['Record Email result',0,'Process each notification'], ['Record invalid Email diagnostic',0,'Process each notification'],
  ['Record exhausted Email diagnostic',0,'Process each notification'], ['Record unsupported channel diagnostic',0,'Process each notification'],
];
const snapshot = () => {
  const nodes = specs.map(([name, type], i) => ({ id: `node-${i}`, name, type: `n8n-nodes-base.${type}`, typeVersion: type === 'emailSend' ? 2.1 : 1, position: [i * 200, 0], parameters: structuredClone(params[name] ?? {}) }));
  Object.assign(nodes.find(n => n.name === 'Send Slack notification'), { retryOnFail: true, maxTries: 5, waitBetweenTries: 5000, onError: 'continueErrorOutput' });
  nodes.find(n => n.name === 'Prepare safe Slack text').onError = 'continueErrorOutput';
  Object.assign(nodes.find(n => n.name === 'Send Email notification'), { retryOnFail: true, maxTries: 5, waitBetweenTries: 5000, onError: 'continueErrorOutput' });
  nodes.find(n => n.name === 'Prepare safe Email text').onError = 'continueErrorOutput';
  const connections = {};
  for (const [source, output, target] of edgeList) {
    connections[source] ??= { main: [] };
    connections[source].main[output] ??= [];
    connections[source].main[output].push({ node: target, type: 'main', index: 0 });
  }
  return { name: 'Sonrisa - Process Alerts - DEV', description: 'Synthetic export test', active: false, nodes, connections, settings: { executionOrder: 'v1' } };
};

test('exporter accepts local candidate node ordering and strips transport credential references', () => {
  const candidate = snapshot();
  candidate.id = 'remote-id';
  candidate.nodes.reverse();
  candidate.nodes.find(n => n.name === 'Send Slack notification').credentials = { slackApi: { id: 'synthetic-secret-reference' } };
  candidate.nodes.find(n => n.name === 'Send Slack notification').webhookId = 'server-generated-slack-webhook';
  candidate.nodes.find(n => n.name === 'Send Email notification').credentials = { smtp: { id: 'synthetic-smtp-reference' } };
  candidate.nodes.find(n => n.name === 'Send Email notification').webhookId = 'server-generated-email-webhook';
  const output = sanitizeWorkflow(candidate, 'process');
  assert.equal(output.nodes.length, 26);
  assert.equal(JSON.stringify(output).includes('synthetic-secret-reference'), false);
  assert.equal(JSON.stringify(output).includes('server-generated-slack-webhook'), false);
  assert.equal(JSON.stringify(output).includes('synthetic-smtp-reference'), false);
  assert.equal(JSON.stringify(output).includes('server-generated-email-webhook'), false);
});

test('A exhausted and B accepted both return to loop; done port has no transport edge', () => {
  const candidate = sanitizeWorkflow(snapshot(), 'process');
  const children = (source, output = 0) => candidate.connections[source]?.main?.[output]?.map(x => x.node) ?? [];
  assert.deepEqual(children('Send Slack notification', 1), ['Record exhausted Slack diagnostic']);
  assert.deepEqual(children('Record exhausted Slack diagnostic'), ['Process each notification']);
  assert.deepEqual(children('Record accepted Slack diagnostic'), ['Process each notification']);
  assert.deepEqual(children('Prepare safe Slack text', 1), ['Record invalid Slack diagnostic']);
  assert.deepEqual(children('Record invalid Slack diagnostic'), ['Process each notification']);
  assert.deepEqual(children('Process each notification', 0), ['Complete manual processing']);
  assert.deepEqual(children('Process each notification', 1), ['Route notification channel']);
  assert.deepEqual(children('Send Email notification', 1), ['Record exhausted Email diagnostic']);
  assert.deepEqual(children('Record exhausted Email diagnostic'), ['Process each notification']);
  assert.deepEqual(children('Record Email result'), ['Process each notification']);
  assert.deepEqual(children('Prepare safe Email text', 1), ['Record invalid Email diagnostic']);
});

test('exporter rejects bypass, unsafe retry, temporary input, SQL drift and pins', () => {
  for (const change of [
    s => { s.connections['Send Slack notification'].main[1][0].node = 'Complete manual processing'; },
    s => { s.nodes.find(n => n.name === 'Send Slack notification').maxTries = 1; },
    s => { s.nodes.find(n => n.name === 'Operator feed input').parameters.assignments.assignments[0].value = 'fixture'; },
    s => { s.nodes.find(n => n.name === 'Load owner alert configuration').parameters.query += ' SELECT 1'; },
    s => { s.pinData = { value: true }; },
    s => { s.nodes.find(n => n.name === 'Evaluate typed magnitude alerts').executeOnce = true; },
    s => { s.nodes.find(n => n.name === 'Fetch USGS all-hour feed').webhookId = 'unexpected-webhook'; },
    s => { s.nodes.find(n => n.name === 'Use live USGS feed?').parameters.options = { unsafe: true }; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').parameters.toEmail = 'victim@example.test'; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').parameters.resource = 'other'; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').parameters.operation = 'sendAndWait'; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').parameters.options.ccEmail = 'other@example.test'; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').maxTries = 1; },
    s => { s.nodes.find(n => n.name === 'Send Email notification').onError = 'continueRegularOutput'; },
    s => { s.nodes.find(n => n.name === 'Prepare safe Email text').parameters.jsCode = 'return $input.all()'; },
  ]) {
    const s = snapshot(); change(s);
    assert.throws(() => sanitizeWorkflow(s, 'process'));
  }
});

test('exporter accepts only verified omitted editor defaults and cosmetic Switch key omission', () => {
  const candidate = snapshot();
  for (const current of candidate.nodes) {
    if (current.type === 'n8n-nodes-base.code') { delete current.parameters.mode; delete current.parameters.language; }
    if (current.type === 'n8n-nodes-base.set') { delete current.parameters.mode; delete current.parameters.includeOtherFields; current.parameters.options = {}; }
    if (current.name === 'Fetch USGS all-hour feed') { delete current.parameters.method; delete current.parameters.authentication; }
    if (current.name === 'Use live USGS feed?') { current.parameters.conditions.options.version = 1; current.parameters.options = {}; }
    if (current.name === 'Route notification channel') {
      delete current.parameters.mode;
      for (const rule of current.parameters.rules.values) { delete rule.outputKey; rule.conditions.options.version = 1; }
    }
    if (current.name === 'Split canonical events') { delete current.parameters.include; current.parameters.options = {}; }
    if (current.name === 'Deduplicate current feed') delete current.parameters.operation;
    if (current.name === 'Deduplicate previous executions') delete current.parameters.logic;
    if (current.name === 'Load owner alert configuration') delete current.parameters.resource;
    if (current.name === 'Process each notification') delete current.parameters.batchSize;
    if (current.name === 'Send Slack notification') {
      for (const key of ['resource', 'operation', 'authentication', 'messageType']) delete current.parameters[key];
    }
    if (current.name === 'Send Email notification') {
      delete current.parameters.resource;
      delete current.parameters.operation;
    }
  }
  assert.equal(sanitizeWorkflow(candidate, 'process').nodes.length, 26);
  candidate.nodes.find(n => n.name === 'Send Email notification').parameters.emailFormat = 'html';
  assert.throws(() => sanitizeWorkflow(candidate, 'process'));
});

test('checked-in process export is sanitized and matches the current graph, source and SQL guards', () => {
  const exported = JSON.parse(read('../workflows/process.json'));
  assert.deepEqual(sanitizeWorkflow(exported, 'process'), exported);
  assert.equal(exported.nodes.length, 26);
});
