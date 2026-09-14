import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = new URL('./', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8');
const expected = [
  ['Manual alert processing', 'manualTrigger'], ['Operator feed input', 'set'],
  ['Select trusted feed path', 'code'], ['Use live USGS feed?', 'if'],
  ['Fetch USGS all-hour feed', 'httpRequest'], ['Mark live USGS source', 'set'],
  ['Normalize earthquake records', 'code'], ['Split canonical events', 'splitOut'],
  ['Deduplicate current feed', 'removeDuplicates'], ['Deduplicate previous executions', 'removeDuplicates'],
  ['Load owner alert configuration', 'postgres'], ['Evaluate typed magnitude alerts', 'code'],
  ['Process each notification', 'splitInBatches'], ['Route notification channel', 'switch'],
  ['Prepare safe Slack text', 'code'], ['Send Slack notification', 'slack'],
  ['Record accepted Slack diagnostic', 'set'], ['Record exhausted Slack diagnostic', 'set'],
  ['Record invalid Slack diagnostic', 'set'],
  ['Prepare safe Email text', 'code'], ['Send Email notification', 'emailSend'],
  ['Record Email result', 'code'], ['Record invalid Email diagnostic', 'set'],
  ['Record exhausted Email diagnostic', 'set'], ['Record unsupported channel diagnostic', 'set'],
  ['Complete manual processing', 'set'],
];
const edges = [
  'Manual alert processing:0>Operator feed input:0', 'Operator feed input:0>Select trusted feed path:0',
  'Select trusted feed path:0>Use live USGS feed?:0', 'Use live USGS feed?:0>Fetch USGS all-hour feed:0',
  'Use live USGS feed?:1>Normalize earthquake records:0', 'Fetch USGS all-hour feed:0>Mark live USGS source:0',
  'Mark live USGS source:0>Normalize earthquake records:0', 'Normalize earthquake records:0>Split canonical events:0',
  'Split canonical events:0>Deduplicate current feed:0', 'Deduplicate current feed:0>Deduplicate previous executions:0',
  'Deduplicate previous executions:0>Load owner alert configuration:0', 'Load owner alert configuration:0>Evaluate typed magnitude alerts:0',
  'Evaluate typed magnitude alerts:0>Process each notification:0', 'Process each notification:0>Complete manual processing:0',
  'Process each notification:1>Route notification channel:0', 'Route notification channel:0>Prepare safe Slack text:0',
  'Route notification channel:1>Prepare safe Email text:0', 'Route notification channel:2>Record unsupported channel diagnostic:0',
  'Prepare safe Slack text:0>Send Slack notification:0', 'Send Slack notification:0>Record accepted Slack diagnostic:0',
  'Prepare safe Slack text:1>Record invalid Slack diagnostic:0',
  'Send Slack notification:1>Record exhausted Slack diagnostic:0',
  'Prepare safe Email text:0>Send Email notification:0',
  'Prepare safe Email text:1>Record invalid Email diagnostic:0',
  'Send Email notification:0>Record Email result:0',
  'Send Email notification:1>Record exhausted Email diagnostic:0',
  'Record accepted Slack diagnostic:0>Process each notification:0',
  'Record exhausted Slack diagnostic:0>Process each notification:0',
  'Record invalid Slack diagnostic:0>Process each notification:0',
  'Record Email result:0>Process each notification:0',
  'Record invalid Email diagnostic:0>Process each notification:0',
  'Record exhausted Email diagnostic:0>Process each notification:0',
  'Record unsupported channel diagnostic:0>Process each notification:0',
].sort();
const codeNames = { 'Select trusted feed path': 'select-ingest-input', 'Normalize earthquake records': 'normalize-earthquakes', 'Evaluate typed magnitude alerts': 'evaluate-alerts', 'Prepare safe Slack text': 'prepare-slack-message', 'Prepare safe Email text': 'prepare-email-message', 'Record Email result': 'record-email-result' };
const nodeFields = ['id', 'name', 'type', 'typeVersion', 'position', 'parameters', 'disabled', 'notes', 'notesInFlow', 'onError', 'retryOnFail', 'maxTries', 'waitBetweenTries', 'alwaysOutputData', 'executeOnce'];
const requireValue = (condition, message) => { if (!condition) throw new Error(message); };
const canonical = value => Array.isArray(value) ? value.map(canonical) : value && typeof value === 'object' ? Object.fromEntries(Object.keys(value).sort().map(key => [key, canonical(value[key])])) : value;
const same = (a, b) => JSON.stringify(canonical(a)) === JSON.stringify(canonical(b));
const expectedNodes = expected.map(([name, type]) => [name, `n8n-nodes-base.${type}`]).sort((left, right) => left[0].localeCompare(right[0]));
const keys = (value, expected) => same(Object.keys(value ?? {}).sort(), [...expected].sort());
const node = (workflow, name) => workflow.nodes.find(n => n.name === name);
const actualEdges = workflow => Object.entries(workflow.connections ?? {}).flatMap(([source, outputs]) =>
  (outputs.main ?? []).flatMap((destinations, output) => (destinations ?? []).map(edge => `${source}:${output}>${edge.node}:${edge.index}`))).sort();
const assignment = (workflow, name, field) => node(workflow, name)?.parameters?.assignments?.assignments?.find(a => a.name === field)?.value;
const condition = (channel) => ({ options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: '={{ $json.channel }}', rightValue: channel, operator: { type: 'string', operation: 'equals' } }
], combinator: 'and' });
const assertSet = (workflow, name, code) => {
  const parameters = node(workflow, name)?.parameters;
  requireValue(keys(parameters, ['mode', 'includeOtherFields', 'assignments']) && parameters?.mode === 'manual' && parameters.includeOtherFields === false, `unsafe diagnostic: ${name}`);
  const values = Object.fromEntries((parameters.assignments?.assignments ?? []).map(a => [a.name, a.value]));
  const paired = {
    source: "={{ $('Process each notification').item.json.event.source }}",
    external_id: "={{ $('Process each notification').item.json.event.external_id }}",
    alert_id: "={{ $('Process each notification').item.json.alert_id }}",
  };
  const fallback = {
    source: "={{ $json.event?.source ?? $json.source ?? '' }}",
    external_id: "={{ $json.event?.external_id ?? $json.external_id ?? '' }}",
    alert_id: "={{ $json.alert_id ?? '' }}",
  };
  const expected = code === 'processing_complete' ? { code } :
    code === 'unsupported_channel' ? { channel: 'diagnostic', code: "={{ $json.code ?? 'unsupported_channel' }}", ...fallback } :
    { channel: 'diagnostic', code, ...paired };
  requireValue(same(values, expected), `unsafe diagnostic: ${name}`);
};

// n8n editor saves omit these verified defaults. Fill them only for checking; export keeps the saved representation.
const normalizeParameters = current => {
  const p = structuredClone(current.parameters ?? {});
  const fill = (key, value) => { if (!Object.hasOwn(p, key)) p[key] = value; };
  if (current.type === 'n8n-nodes-base.code') { fill('mode', 'runOnceForAllItems'); fill('language', 'javaScript'); }
  if (current.type === 'n8n-nodes-base.set') { fill('mode', 'manual'); fill('includeOtherFields', false); if (same(p.options, {})) delete p.options; }
  if (current.name === 'Fetch USGS all-hour feed') { fill('method', 'GET'); fill('authentication', 'none'); }
  if (current.name === 'Use live USGS feed?') { if (same(p.options, {})) delete p.options; }
  if (current.name === 'Use live USGS feed?' || current.name === 'Route notification channel') {
    const conditions = current.name === 'Use live USGS feed?' ? [p.conditions] : (p.rules?.values ?? []).map(rule => rule.conditions);
    for (const c of conditions) if (c?.options?.version === 1) delete c.options.version;
  }
  if (current.name === 'Route notification channel') {
    fill('mode', 'rules');
    for (const [index, rule] of (p.rules?.values ?? []).entries()) if (!Object.hasOwn(rule, 'outputKey') && index < 2) rule.outputKey = ['Slack', 'Email'][index];
  }
  if (current.name === 'Split canonical events') { fill('include', 'noOtherFields'); if (same(p.options, {})) delete p.options; }
  if (current.name === 'Deduplicate current feed') fill('operation', 'removeDuplicateInputItems');
  if (current.name === 'Deduplicate previous executions') fill('logic', 'removeItemsWithAlreadySeenKeyValues');
  if (current.name === 'Load owner alert configuration') fill('resource', 'database');
  if (current.name === 'Process each notification') fill('batchSize', 1);
  if (current.name === 'Send Slack notification') { fill('resource', 'message'); fill('operation', 'post'); fill('authentication', 'accessToken'); fill('messageType', 'text'); }
  if (current.name === 'Send Email notification' && current.type === 'n8n-nodes-base.emailSend') { fill('resource', 'email'); fill('operation', 'send'); }
  return p;
};

export function sanitizeWorkflow(snapshot, kind) {
  requireValue(kind === 'process' && snapshot && typeof snapshot === 'object', 'unknown workflow kind');
  requireValue(snapshot.name === 'Sonrisa - Process Alerts - DEV' && snapshot.active === false, 'expected named inactive workflow');
  requireValue(Array.isArray(snapshot.nodes) && same(snapshot.nodes.map(n => [n.name, n.type]).sort((left, right) => left[0].localeCompare(right[0])), expectedNodes), 'unexpected nodes');
  requireValue(same(actualEdges(snapshot), edges), 'unexpected connection graph');
  requireValue(snapshot.settings?.executionOrder === 'v1', 'unexpected execution settings');
  requireValue(!snapshot.staticData && !snapshot.pinData, 'workflow contains state or pins');
  const checked = { ...snapshot, nodes: snapshot.nodes.map(current => ({ ...current, parameters: normalizeParameters(current) })) };
  for (const current of checked.nodes) {
    requireValue(Array.isArray(current.position) && current.position.length === 2 && current.disabled !== true && current.executeOnce !== true && current.alwaysOutputData !== true, `unsafe node state: ${current.name}`);
    const isTransportSend = (current.name === 'Send Slack notification' && current.type === 'n8n-nodes-base.slack') ||
      (current.name === 'Send Email notification' && current.type === 'n8n-nodes-base.emailSend');
    requireValue(!current.pinData && (isTransportSend || !Object.hasOwn(current, 'webhookId')), `node contains pins or webhook: ${current.name}`);
    if (current.type === 'n8n-nodes-base.code') requireValue(keys(current.parameters, ['mode', 'language', 'jsCode']) && current.parameters?.mode === 'runOnceForAllItems' && current.parameters?.language === 'javaScript' && current.parameters?.jsCode === read(`runtime/${codeNames[current.name]}.js`), `Code source drift: ${current.name}`);
  }
  requireValue(assignment(checked, 'Operator feed input', 'mode') === 'live' && assignment(checked, 'Operator feed input', 'fixture_json') === '', 'operator defaults must be live/empty');
  requireValue(keys(node(checked, 'Operator feed input').parameters, ['mode', 'includeOtherFields', 'assignments']) && node(checked, 'Operator feed input').parameters.includeOtherFields === false, 'unsafe operator input');
  requireValue(keys(node(checked, 'Use live USGS feed?').parameters, ['conditions']) && same(node(checked, 'Use live USGS feed?').parameters?.conditions, { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
    { leftValue: '={{ $json.mode }}', rightValue: 'live', operator: { type: 'string', operation: 'equals' } }
  ], combinator: 'and' }), 'unsafe live fixture routing');
  const fetch = node(checked, 'Fetch USGS all-hour feed');
  requireValue(keys(fetch.parameters, ['method', 'url', 'authentication', 'options']) && fetch.parameters?.method === 'GET' && fetch.parameters?.authentication === 'none' && fetch.parameters?.url === 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson' && same(fetch.parameters?.options, { timeout: 20000, response: { response: { responseFormat: 'json' } } }), 'unsafe live fetch');
  requireValue(same(node(checked, 'Mark live USGS source').parameters, { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
    { id: 'live-source', name: 'source', value: 'usgs', type: 'string' },
    { id: 'live-feed', name: 'feed', value: '={{ $json }}', type: 'object' }
  ] } }), 'unsafe live feed envelope');
  requireValue(same(node(checked, 'Split canonical events').parameters, { fieldToSplitOut: 'events', include: 'noOtherFields' }), 'unsafe event split');
  requireValue(same(node(checked, 'Deduplicate current feed').parameters, { operation: 'removeDuplicateInputItems', compare: 'selectedFields', fieldsToCompare: 'source,external_id', options: { removeOtherFields: false } }), 'unsafe within-feed deduplication');
  requireValue(same(node(checked, 'Deduplicate previous executions').parameters, { operation: 'removeItemsSeenInPreviousExecutions', logic: 'removeItemsWithAlreadySeenKeyValues', dedupeValue: '={{ $json.source + ":" + $json.external_id }}', options: { scope: 'node', historySize: 10000 } }), 'unsafe previous-execution deduplication');
  const pg = node(checked, 'Load owner alert configuration');
  requireValue(keys(pg.parameters, ['resource', 'operation', 'query', 'options']) && pg.parameters?.resource === 'database' && pg.parameters?.operation === 'executeQuery' && pg.parameters?.query === read('sql/select-enabled-alerts.sql') && same(pg.parameters?.options, { queryBatching: 'independently', queryReplacement: '={{ [JSON.stringify($json)] }}' }), 'unsafe configuration query or binding');
  requireValue(same(node(checked, 'Process each notification').parameters, { batchSize: 1, options: { reset: false } }), 'unsafe notification loop');
  const route = node(checked, 'Route notification channel').parameters;
  requireValue(keys(route, ['mode', 'rules', 'options']) && route?.mode === 'rules' && same(route.rules?.values, [
    { outputKey: 'Slack', conditions: condition('slack') },
    { outputKey: 'Email', conditions: condition('email') }
  ]) && same(route.options, { fallbackOutput: 'extra', renameFallbackOutput: 'Diagnostic or unsupported' }), 'unsafe channel routing');
  const send = node(checked, 'Send Slack notification');
  requireValue(node(checked, 'Prepare safe Slack text').onError === 'continueErrorOutput', 'unsafe Slack preparation error path');
  requireValue(send.retryOnFail === true && send.maxTries === 5 && send.waitBetweenTries === 5000 && send.onError === 'continueErrorOutput', 'unsafe Slack retry settings');
  requireValue(keys(send.parameters, ['resource', 'operation', 'authentication', 'select', 'channelId', 'messageType', 'text', 'otherOptions']) && send.parameters?.resource === 'message' && send.parameters?.operation === 'post' && send.parameters?.authentication === 'accessToken' && send.parameters?.select === 'channel' && send.parameters?.messageType === 'text' && send.parameters?.channelId?.mode === 'id' && send.parameters?.channelId?.value === '={{ $json.destination }}' && send.parameters?.text === '={{ $json.text }}', 'unsafe Slack send');
  requireValue(same(send.parameters?.otherOptions, { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false }), 'unsafe Slack options');
  const email = node(checked, 'Send Email notification');
  requireValue(node(checked, 'Prepare safe Email text').onError === 'continueErrorOutput', 'unsafe Email preparation error path');
  requireValue(email.retryOnFail === true && email.maxTries === 5 && email.waitBetweenTries === 5000 && email.onError === 'continueErrorOutput', 'unsafe Email retry settings');
  requireValue(email.typeVersion === 2.1 && keys(email.parameters, ['resource', 'operation', 'fromEmail', 'toEmail', 'subject', 'emailFormat', 'text', 'options']) &&
    email.parameters?.resource === 'email' && email.parameters?.operation === 'send' &&
    email.parameters?.fromEmail === 'sonrisa@example.test' && email.parameters?.toEmail === '={{ $json.destination }}' &&
    email.parameters?.subject === '={{ $json.subject }}' && email.parameters?.emailFormat === 'text' &&
    email.parameters?.text === '={{ $json.text }}' && same(email.parameters?.options, { appendAttribution: false }), 'unsafe Email send');
  for (const [name, code] of [
    ['Record accepted Slack diagnostic', 'slack_accepted'],
    ['Record exhausted Slack diagnostic', 'slack_retries_exhausted_discarded'],
    ['Record invalid Slack diagnostic', 'invalid_slack_notification_discarded'],
    ['Record invalid Email diagnostic', 'invalid_email_notification_discarded'],
    ['Record exhausted Email diagnostic', 'email_retries_exhausted_discarded'],
    ['Record unsupported channel diagnostic', 'unsupported_channel'],
    ['Complete manual processing', 'processing_complete'],
  ]) assertSet(checked, name, code);
  const nodes = snapshot.nodes.map(current => Object.fromEntries(nodeFields.filter(field => Object.hasOwn(current, field)).map(field => [field, current[field]])));
  return { name: snapshot.name, description: snapshot.description, active: false, nodes, connections: snapshot.connections, settings: { executionOrder: snapshot.settings.executionOrder } };
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const [kind, input] = process.argv.slice(2);
  requireValue(kind === 'process' && input && process.argv.length === 4, 'usage: node n8n/export-workflow.mjs process <snapshot.json|->');
  const snapshot = JSON.parse(readFileSync(input === '-' ? 0 : input, 'utf8'));
  process.stdout.write(`${JSON.stringify(sanitizeWorkflow(snapshot, kind), null, 2)}\n`);
}
