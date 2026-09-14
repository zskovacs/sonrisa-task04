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
};
const sqlNames = {
  'Insert new source events': 'insert-source-events',
  'Read oldest pending event': 'select-pending-event',
  'Load enabled alert snapshot': 'select-event-candidates',
  'Insert prepared delivery intents': 'insert-delivery-intents',
  'Complete evaluated event': 'complete-event',
  'Claim one pending Slack delivery': 'claim-slack-delivery',
  'Read immutable event message': 'select-claimed-delivery',
  'Record acknowledged Slack send': 'record-slack-sent',
  'Record ambiguous Slack outcome': 'record-slack-unknown',
  'Record known Slack rejection': 'record-slack-failed',
};
const queryBindings = {
  'Insert new source events': '={{ JSON.stringify($json.events) }}',
  'Read oldest pending event': null,
  'Load enabled alert snapshot': '={{ $json.id }}',
  'Insert prepared delivery intents': '={{ JSON.stringify($("Evaluate typed magnitude alerts").first().json) }}',
  'Complete evaluated event': '={{ $("Evaluate typed magnitude alerts").first().json.event_id }}',
  'Claim one pending Slack delivery': '={{ $json.delivery_id }}',
  'Read immutable event message': '={{ $json.id }}',
  'Record acknowledged Slack send': '={{ $("Prepare safe Slack text").first().json.id }}',
  'Record ambiguous Slack outcome': '={{ $("Prepare safe Slack text").first().json.id }}',
  'Record known Slack rejection': '={{ $("Prepare safe Slack text").first().json.id }}',
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
    name: 'Sonrisa - Deliver Slack Notification - DEV',
    names: ['Manual selected delivery', 'Operator delivery ID', 'Require explicit delivery UUID', 'Claim one pending Slack delivery', 'Read immutable event message', 'Prepare safe Slack text', 'Send selected Slack notification', 'Slack acknowledged send?', 'Record acknowledged Slack send', 'Record ambiguous Slack outcome', 'Known Slack rejection?', 'Record known Slack rejection'],
    types: ['manualTrigger', 'set', 'code', 'postgres', 'postgres', 'code', 'slack', 'if', 'postgres', 'postgres', 'if', 'postgres'],
    edges: ['Manual selected delivery:0>Operator delivery ID:0', 'Operator delivery ID:0>Require explicit delivery UUID:0', 'Require explicit delivery UUID:0>Claim one pending Slack delivery:0', 'Claim one pending Slack delivery:0>Read immutable event message:0', 'Read immutable event message:0>Prepare safe Slack text:0', 'Prepare safe Slack text:0>Send selected Slack notification:0', 'Send selected Slack notification:0>Slack acknowledged send?:0', 'Send selected Slack notification:1>Known Slack rejection?:0', 'Slack acknowledged send?:0>Record acknowledged Slack send:0', 'Slack acknowledged send?:1>Record ambiguous Slack outcome:0', 'Known Slack rejection?:0>Record known Slack rejection:0', 'Known Slack rejection?:1>Record ambiguous Slack outcome:0'],
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
    const send = snapshot.nodes.find(n => n.name === 'Send selected Slack notification');
    requireValue(send.type === 'n8n-nodes-base.slack' && send.onError === 'continueErrorOutput' && send.retryOnFail !== true && send.parameters.resource === 'message' && send.parameters.operation === 'post' && send.parameters.channelId?.value === '={{ $json.destination }}' && send.parameters.text === '={{ $json.text }}', 'unexpected Slack send');
    requireValue(same(send.parameters.otherOptions, { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false }), 'unsafe Slack options');
    const rejection = snapshot.nodes.find(n => n.name === 'Known Slack rejection?');
    requireValue(knownRejectionExpression && rejection.parameters.conditions?.conditions?.[0]?.leftValue === `=${knownRejectionExpression}`, 'Slack rejection expression drift');
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
