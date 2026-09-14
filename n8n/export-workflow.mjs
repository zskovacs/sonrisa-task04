import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = new URL('./', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8');
const codeNames = {
  'Select trusted ingest path': 'select-ingest-input',
  'Normalize earthquake records': 'normalize-earthquakes',
  'Evaluate typed magnitude alerts': 'evaluate-alerts',
  'Require explicit delivery UUID': 'validate-delivery-id',
  'Prepare safe Slack text': 'prepare-slack-message',
  'Prepare safe email message': 'prepare-email-message',
  'Validate SMTP recipient acceptance': 'validate-email-result',
};
const sqlNames = {
  'Insert new source events': 'insert-source-events',
  'Read oldest pending event': 'select-pending-event',
  'Load enabled alert snapshot': 'select-event-candidates',
  'Insert prepared delivery intents': 'insert-delivery-intents',
  'Complete evaluated event': 'complete-event',
  'Claim one pending delivery': 'claim-delivery',
  'Read immutable event message': 'select-claimed-delivery',
  'Record acknowledged Slack send': 'record-slack-sent',
  'Record ambiguous Slack outcome': 'record-slack-unknown',
  'Record known Slack rejection': 'record-slack-failed',
  'Record accepted email send': 'record-email-sent',
  'Record ambiguous email outcome': 'record-email-unknown',
  'Record invalid email message': 'record-email-invalid',
};
const queryBindings = {
  'Insert new source events': '={{ JSON.stringify($json.events) }}',
  'Read oldest pending event': null,
  'Load enabled alert snapshot': '={{ $json.id }}',
  'Insert prepared delivery intents': '={{ JSON.stringify($("Evaluate typed magnitude alerts").first().json) }}',
  'Complete evaluated event': '={{ $("Evaluate typed magnitude alerts").first().json.event_id }}',
  'Claim one pending delivery': '={{ $json.delivery_id }}',
  'Read immutable event message': '={{ $json.id }}',
  'Record acknowledged Slack send': '={{ $("Prepare safe Slack text").first().json.id }}',
  'Record ambiguous Slack outcome': '={{ $("Prepare safe Slack text").first().json.id }}',
  'Record known Slack rejection': '={{ $("Prepare safe Slack text").first().json.id }}',
  'Record accepted email send': '={{ $("Prepare safe email message").first().json.id }}',
  'Record ambiguous email outcome': '={{ $("Prepare safe email message").first().json.id }}',
  'Record invalid email message': '={{ $("Prepare safe email message").first().json.id }}',
};
const specs = {
  ingest: {
    name: 'Sonrisa - Ingest Earthquakes - DEV',
    names: ['Manual ingest', 'Operator ingest input', 'Select trusted ingest path', 'Live USGS input?', 'Fetch USGS all-hour feed', 'Mark live USGS source', 'Normalize earthquake records', 'Insert new source events'],
    types: ['manualTrigger', 'set', 'code', 'if', 'httpRequest', 'set', 'code', 'postgres'],
    edges: ['Manual ingest:0>Operator ingest input:0', 'Operator ingest input:0>Select trusted ingest path:0', 'Select trusted ingest path:0>Live USGS input?:0', 'Live USGS input?:0>Fetch USGS all-hour feed:0', 'Live USGS input?:1>Normalize earthquake records:0', 'Fetch USGS all-hour feed:0>Mark live USGS source:0', 'Mark live USGS source:0>Normalize earthquake records:0', 'Normalize earthquake records:0>Insert new source events:0'],
  },
  evaluate: {
    name: 'Sonrisa - Evaluate Pending Events - DEV',
    names: ['Manual evaluation', 'Read oldest pending event', 'Load enabled alert snapshot', 'Evaluate typed magnitude alerts', 'Insert prepared delivery intents', 'Complete evaluated event'],
    types: ['manualTrigger', 'postgres', 'postgres', 'code', 'postgres', 'postgres'],
    edges: ['Manual evaluation:0>Read oldest pending event:0', 'Read oldest pending event:0>Load enabled alert snapshot:0', 'Load enabled alert snapshot:0>Evaluate typed magnitude alerts:0', 'Evaluate typed magnitude alerts:0>Insert prepared delivery intents:0', 'Insert prepared delivery intents:0>Complete evaluated event:0'],
  },
  deliver: {
    name: 'Sonrisa - Deliver Notification - DEV',
    names: ['Manual selected delivery', 'Operator delivery ID', 'Require explicit delivery UUID', 'Claim one pending delivery', 'Read immutable event message', 'Prepare safe Slack text', 'Send selected Slack notification', 'Slack acknowledged send?', 'Record acknowledged Slack send', 'Record ambiguous Slack outcome', 'Known Slack rejection?', 'Record known Slack rejection', 'Slack channel?', 'Email channel?', 'Prepare safe email message', 'Valid email message?', 'Send selected email notification', 'Validate SMTP recipient acceptance', 'SMTP accepted recipient?', 'Record accepted email send', 'Record ambiguous email outcome', 'Record invalid email message'],
    types: ['manualTrigger', 'set', 'code', 'postgres', 'postgres', 'code', 'slack', 'if', 'postgres', 'postgres', 'if', 'postgres', 'if', 'if', 'code', 'if', 'emailSend', 'code', 'if', 'postgres', 'postgres', 'postgres'],
    edges: ['Manual selected delivery:0>Operator delivery ID:0', 'Operator delivery ID:0>Require explicit delivery UUID:0', 'Require explicit delivery UUID:0>Claim one pending delivery:0', 'Claim one pending delivery:0>Read immutable event message:0', 'Read immutable event message:0>Slack channel?:0', 'Slack channel?:0>Prepare safe Slack text:0', 'Slack channel?:1>Email channel?:0', 'Email channel?:0>Prepare safe email message:0', 'Prepare safe Slack text:0>Send selected Slack notification:0', 'Send selected Slack notification:0>Slack acknowledged send?:0', 'Send selected Slack notification:1>Known Slack rejection?:0', 'Slack acknowledged send?:0>Record acknowledged Slack send:0', 'Slack acknowledged send?:1>Record ambiguous Slack outcome:0', 'Known Slack rejection?:0>Record known Slack rejection:0', 'Known Slack rejection?:1>Record ambiguous Slack outcome:0', 'Prepare safe email message:0>Valid email message?:0', 'Valid email message?:0>Send selected email notification:0', 'Valid email message?:1>Record invalid email message:0', 'Send selected email notification:0>Validate SMTP recipient acceptance:0', 'Send selected email notification:1>Record ambiguous email outcome:0', 'Validate SMTP recipient acceptance:0>SMTP accepted recipient?:0', 'SMTP accepted recipient?:0>Record accepted email send:0', 'SMTP accepted recipient?:1>Record ambiguous email outcome:0'],
  },
};
const nodeFields = ['id', 'name', 'type', 'typeVersion', 'position', 'parameters', 'disabled', 'notes', 'notesInFlow', 'onError', 'retryOnFail', 'maxTries', 'waitBetweenTries', 'alwaysOutputData', 'executeOnce'];
const requireValue = (condition, message) => { if (!condition) throw new Error(message); };
const assignment = (workflow, name, field) => workflow.nodes.find(n => n.name === name)?.parameters?.assignments?.assignments?.find(a => a.name === field)?.value;
const edgesOf = workflow => Object.entries(workflow.connections ?? {}).flatMap(([source, outputs]) =>
  (outputs.main ?? []).flatMap((destinations, output) => (destinations ?? []).map(edge => `${source}:${output}>${edge.node}:${edge.index}`))).sort();
const same = (left, right) => JSON.stringify(left) === JSON.stringify(right);
const rejectionSource = read('sdk/deliver.sdk.js').split('const knownRejection =')[1]?.split('const sent =')[0];
const knownRejectionExpression = rejectionSource?.match(/leftValue: expr\('(\{\{[^']+\}\})'\)/)?.[1];

export function sanitizeWorkflow(snapshot, kind) {
  const spec = specs[kind];
  requireValue(spec && snapshot && typeof snapshot === 'object', 'unknown workflow kind');
  requireValue(snapshot.name === spec.name && snapshot.active === false, 'expected named inactive workflow');
  requireValue(Array.isArray(snapshot.nodes) && same(snapshot.nodes.map(n => n.name), spec.names), 'unexpected nodes');
  requireValue(same(snapshot.nodes.map(n => n.type), spec.types.map(type => `n8n-nodes-base.${type}`)), 'unexpected node types');
  requireValue(same(edgesOf(snapshot), [...spec.edges].sort()), 'unexpected connection graph');
  requireValue(snapshot.settings?.executionOrder === 'v1', 'unexpected execution settings');
  for (const node of snapshot.nodes) {
    requireValue(Array.isArray(node.position) && node.position.length === 2, 'missing node position');
    if (node.type === 'n8n-nodes-base.code') {
      const source = codeNames[node.name];
      requireValue(source && node.parameters?.jsCode === read(`runtime/${source}.js`), `Code source drift: ${node.name}`);
    }
    if (node.type === 'n8n-nodes-base.postgres') {
      const source = sqlNames[node.name];
      requireValue(source && node.parameters?.query === read(`sql/${source}.sql`), `SQL source drift: ${node.name}`);
      requireValue(node.parameters.operation === 'executeQuery', 'unexpected Postgres operation');
      requireValue((node.parameters.options?.queryReplacement ?? null) === queryBindings[node.name], `unsafe query binding: ${node.name}`);
    }
  }
  if (kind === 'ingest') {
    requireValue(assignment(snapshot, 'Operator ingest input', 'mode') === 'live' && assignment(snapshot, 'Operator ingest input', 'fixture_json') === '', 'operator defaults must be live/empty');
    const http = snapshot.nodes.find(n => n.name === 'Fetch USGS all-hour feed');
    requireValue(http.type === 'n8n-nodes-base.httpRequest' && http.parameters.method === 'GET' && http.parameters.authentication === 'none' && http.parameters.url === 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson' && !http.parameters.sendHeaders, 'unexpected live fetch');
  }
  if (kind === 'deliver') {
    requireValue(assignment(snapshot, 'Operator delivery ID', 'delivery_id') === '', 'operator defaults must have empty delivery ID');
    requireValue(assignment(snapshot, 'Operator delivery ID', 'sender_email') === 'sonrisa@example.test', 'unexpected email sender default');
    requireValue(snapshot.nodes.every(node => node.disabled !== true), 'disabled delivery safety or transport node');
    requireValue(snapshot.nodes.find(n => n.name === 'Claim one pending delivery')?.alwaysOutputData !== true, 'claim must not synthesize an empty row');
    const send = snapshot.nodes.find(n => n.name === 'Send selected Slack notification');
    requireValue(send.type === 'n8n-nodes-base.slack' && send.onError === 'continueErrorOutput' && send.retryOnFail !== true && send.parameters.resource === 'message' && send.parameters.operation === 'post' && send.parameters.channelId?.value === '={{ $json.destination }}' && send.parameters.text === '={{ $json.text }}', 'unexpected Slack send');
    requireValue(same(send.parameters.otherOptions, { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false }), 'unsafe Slack options');
    const rejection = snapshot.nodes.find(n => n.name === 'Known Slack rejection?');
    requireValue(knownRejectionExpression && rejection.parameters.conditions?.conditions?.[0]?.leftValue === `=${knownRejectionExpression}`, 'Slack rejection expression drift');
    const exactCondition = (name, leftValue, rightValue, type, operation) => {
      const actual = snapshot.nodes.find(n => n.name === name)?.parameters?.conditions;
      const expected = { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' },
        conditions: [{ leftValue, rightValue, operator: { type, operation } }], combinator: 'and' };
      return same(actual, expected);
    };
    requireValue(exactCondition('Slack channel?', '={{ $json.channel }}', 'slack', 'string', 'equals'), 'unsafe channel routing: Slack channel?');
    requireValue(exactCondition('Email channel?', '={{ $json.channel }}', 'email', 'string', 'equals'), 'unsafe channel routing: Email channel?');
    requireValue(exactCondition('Valid email message?', '={{ $json.valid === true }}', true, 'boolean', 'true'), 'unsafe email validity routing');
    requireValue(exactCondition('SMTP accepted recipient?', '={{ $json.accepted === true }}', true, 'boolean', 'true'), 'unsafe SMTP acceptance routing');
    const smtp = snapshot.nodes.find(n => n.name === 'Send selected email notification');
    requireValue(smtp.type === 'n8n-nodes-base.emailSend' && smtp.typeVersion === 2.1 && smtp.onError === 'continueErrorOutput' && smtp.retryOnFail === false, 'unexpected SMTP settings');
    requireValue(same(Object.keys(smtp.parameters).sort(), ['resource', 'operation', 'fromEmail', 'toEmail', 'subject', 'emailFormat', 'text', 'options'].sort()), 'unexpected SMTP fields');
    requireValue(smtp.parameters.resource === 'email' && smtp.parameters.operation === 'send' && smtp.parameters.emailFormat === 'text' && same(smtp.parameters.options, { appendAttribution: false }) && smtp.parameters.fromEmail === '={{ $json.sender }}' && smtp.parameters.toEmail === '={{ $json.destination }}' && smtp.parameters.subject === '={{ $json.subject }}' && smtp.parameters.text === '={{ $json.text }}', 'unsafe SMTP configuration');
  }
  const nodes = snapshot.nodes.map(node => Object.fromEntries(nodeFields.filter(field => Object.hasOwn(node, field)).map(field => [field, node[field]])));
  return { name: snapshot.name, description: snapshot.description, active: false, nodes, connections: snapshot.connections, settings: { executionOrder: snapshot.settings.executionOrder } };
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const kind = process.argv[2];
  const input = process.argv[3];
  requireValue(specs[kind] && input && process.argv.length === 4, 'usage: node n8n/export-workflow.mjs ingest|evaluate|deliver <snapshot.json|->');
  const snapshot = JSON.parse(readFileSync(input === '-' ? 0 : input, 'utf8'));
  process.stdout.write(`${JSON.stringify(sanitizeWorkflow(snapshot, kind), null, 2)}\n`);
}
