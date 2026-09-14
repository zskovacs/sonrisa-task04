import { workflow, node, trigger, expr } from '@n8n/workflow-sdk';

const start = trigger({ type: 'n8n-nodes-base.manualTrigger', version: 1, config: { name: 'Manual evaluation' } });
const pending = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Read oldest pending event', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_SELECT_PENDING_EVENT__ } } });
const candidates = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Load enabled alert snapshot', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_SELECT_EVENT_CANDIDATES__, options: { queryReplacement: expr('{{ $json.id }}') } } } });
const evaluate = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Evaluate typed magnitude alerts', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_EVALUATE_ALERTS__ } } });
const insert = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Insert prepared delivery intents', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_INSERT_DELIVERY_INTENTS__, options: { queryReplacement: expr('{{ JSON.stringify($("Evaluate typed magnitude alerts").first().json) }}') } } } });
const complete = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Complete evaluated event', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_COMPLETE_EVENT__, options: { queryReplacement: expr('{{ $("Evaluate typed magnitude alerts").first().json.event_id }}') } } } });

export default workflow('sonrisa-evaluate-pending-events-dev', 'Sonrisa - Evaluate Pending Events - DEV')
  .add(start).to(pending).to(candidates).to(evaluate).to(insert).to(complete);
