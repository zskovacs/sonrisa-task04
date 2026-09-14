import { workflow, node, trigger, ifElse, expr } from '@n8n/workflow-sdk';

const start = trigger({ type: 'n8n-nodes-base.manualTrigger', version: 1, config: { name: 'Manual ingest' } });
const input = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Operator ingest input', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'mode', name: 'mode', value: __INGEST_MODE__, type: 'string' },
  { id: 'fixture-json', name: 'fixture_json', value: __INGEST_FIXTURE_JSON__, type: 'string' }
] } } } });
const selector = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Select trusted ingest path', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_SELECT_INGEST_INPUT__ } } });
const live = ifElse({ version: 2.3, config: { name: 'Live USGS input?', parameters: { conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: expr('{{ $json.mode }}'), rightValue: 'live', operator: { type: 'string', operation: 'equals' } }
], combinator: 'and' } } } });
const fetch = node({ type: 'n8n-nodes-base.httpRequest', version: 4.5, config: { name: 'Fetch USGS all-hour feed', retryOnFail: false, parameters: { method: 'GET', url: 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson', authentication: 'none', options: { timeout: 20000, response: { response: { responseFormat: 'json' } } } } } });
const liveEnvelope = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Mark live USGS source', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'live-source', name: 'source', value: 'usgs', type: 'string' },
  { id: 'live-feed', name: 'feed', value: expr('{{ $json }}'), type: 'object' }
] } } } });
const normalize = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Normalize earthquake records', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_NORMALIZE_EARTHQUAKES__ } } });
const insert = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Insert new source events', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_INSERT_SOURCE_EVENTS__, options: { queryReplacement: expr('{{ JSON.stringify($json.events) }}') } } } });

export default workflow('sonrisa-ingest-earthquakes-dev', 'Sonrisa - Ingest Earthquakes - DEV')
  .add(start).to(input).to(selector)
  .to(live.onTrue(fetch.to(liveEnvelope.to(normalize))).onFalse(normalize))
  .add(normalize).to(insert);
