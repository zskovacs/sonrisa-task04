import { workflow, node, trigger, ifElse, expr } from '@n8n/workflow-sdk';

const start = trigger({ type: 'n8n-nodes-base.manualTrigger', version: 1, config: { name: 'Manual selected delivery' } });
const input = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Operator delivery ID', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'delivery-id', name: 'delivery_id', value: __DELIVERY_ID__, type: 'string' }
] } } } });
const validate = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Require explicit delivery UUID', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_VALIDATE_DELIVERY_ID__ } } });
const claim = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Claim one pending Slack delivery', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_CLAIM_SLACK_DELIVERY__, options: { queryReplacement: expr('{{ $json.delivery_id }}') } } } });
const details = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Read immutable event message', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_SELECT_CLAIMED_DELIVERY__, options: { queryReplacement: expr('{{ $json.id }}') } } } });
const message = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Prepare safe Slack text', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_PREPARE_SLACK_MESSAGE__ } } });
const send = node({ type: 'n8n-nodes-base.slack', version: 2.7, config: { name: 'Send selected Slack notification', retryOnFail: false, maxTries: 1, onError: 'continueErrorOutput', parameters: { resource: 'message', operation: 'post', authentication: 'accessToken', select: 'channel', channelId: { __rl: true, mode: 'id', value: expr('{{ $json.destination }}') }, messageType: 'text', text: expr('{{ $json.text }}'), otherOptions: { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false } } } });
const acknowledged = ifElse({ version: 2.3, config: { name: 'Slack acknowledged send?', parameters: { conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: expr('{{ $json.ok === true }}'), rightValue: true, operator: { type: 'boolean', operation: 'true' } }
], combinator: 'and' } } } });
const knownRejection = ifElse({ version: 2.3, config: { name: 'Known Slack rejection?', parameters: { conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: expr('{{ (() => { const error = $json.error; const codes = ["channel_not_found", "not_in_channel", "invalid_auth", "is_archived", "account_inactive", "missing_scope"]; return typeof error === "string" ? error === "Your Slack credential is missing required Oauth Scopes" || codes.some(code => error === "Slack error response: " + JSON.stringify(code)) : codes.includes(error?.code); })() }}'), rightValue: true, operator: { type: 'boolean', operation: 'true' } }
], combinator: 'and' } } } });
const sent = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Record acknowledged Slack send', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_RECORD_SLACK_SENT__, options: { queryReplacement: expr('{{ $("Prepare safe Slack text").first().json.id }}') } } } });
const failed = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Record known Slack rejection', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_RECORD_SLACK_FAILED__, options: { queryReplacement: expr('{{ $("Prepare safe Slack text").first().json.id }}') } } } });
const unknown = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Record ambiguous Slack outcome', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_RECORD_SLACK_UNKNOWN__, options: { queryReplacement: expr('{{ $("Prepare safe Slack text").first().json.id }}') } } } });

export default workflow('sonrisa-deliver-slack-notification-dev', 'Sonrisa - Deliver Slack Notification - DEV')
  .add(start).to(input).to(validate).to(claim).to(details).to(message).to(send)
  .to(acknowledged.onTrue(sent).onFalse(unknown))
  .add(send.onError(knownRejection.onTrue(failed).onFalse(unknown)));
