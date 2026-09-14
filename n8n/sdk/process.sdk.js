import { workflow, node, trigger, ifElse, switchCase, splitInBatches, nextBatch, expr } from '@n8n/workflow-sdk';

const start = trigger({ type: 'n8n-nodes-base.manualTrigger', version: 1, config: { name: 'Manual alert processing' }, output: [{}] });
const input = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Operator feed input', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'mode', name: 'mode', value: __INGEST_MODE__, type: 'string' },
  { id: 'fixture-json', name: 'fixture_json', value: __INGEST_FIXTURE_JSON__, type: 'string' }
] } } }, output: [{ mode: 'live', fixture_json: '' }] });
const selector = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Select trusted feed path', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_SELECT_INGEST_INPUT__ } }, output: [{ mode: 'live' }] });
const live = ifElse({ version: 2.3, config: { name: 'Use live USGS feed?', parameters: { conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
  { leftValue: expr('{{ $json.mode }}'), rightValue: 'live', operator: { type: 'string', operation: 'equals' } }
], combinator: 'and' } } }, output: [{ mode: 'live' }] });
const fetch = node({ type: 'n8n-nodes-base.httpRequest', version: 4.5, config: { name: 'Fetch USGS all-hour feed', parameters: { method: 'GET', url: 'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson', authentication: 'none', options: { timeout: 20000, response: { response: { responseFormat: 'json' } } } } }, output: [{ type: 'FeatureCollection', features: [] }] });
const liveEnvelope = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Mark live USGS source', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'live-source', name: 'source', value: 'usgs', type: 'string' },
  { id: 'live-feed', name: 'feed', value: expr('{{ $json }}'), type: 'object' }
] } } }, output: [{ source: 'usgs', feed: { type: 'FeatureCollection', features: [] } }] });
const normalize = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Normalize earthquake records', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_NORMALIZE_EARTHQUAKES__ } }, output: [{ events: [], diagnostics: [] }] });
const splitEvents = node({ type: 'n8n-nodes-base.splitOut', version: 1, config: { name: 'Split canonical events', parameters: { fieldToSplitOut: 'events', include: 'noOtherFields' } }, output: [{ contract_version: 1, source: 'demo.usgs', external_id: 'example', event_type: 'earthquake', data: { magnitude: 5 } }] });
const withinInput = node({ type: 'n8n-nodes-base.removeDuplicates', version: 2, config: { name: 'Deduplicate current feed', parameters: { operation: 'removeDuplicateInputItems', compare: 'selectedFields', fieldsToCompare: 'source,external_id', options: { removeOtherFields: false } } }, output: [{ source: 'demo.usgs', external_id: 'example' }] });
const previousRuns = node({ type: 'n8n-nodes-base.removeDuplicates', version: 2, config: { name: 'Deduplicate previous executions', parameters: { operation: 'removeItemsSeenInPreviousExecutions', logic: 'removeItemsWithAlreadySeenKeyValues', dedupeValue: expr('{{ $json.source + ":" + $json.external_id }}'), options: { scope: 'node', historySize: 10000 } } }, output: [{ source: 'demo.usgs', external_id: 'example' }] });
const configs = node({ type: 'n8n-nodes-base.postgres', version: 2.7, config: { name: 'Load owner alert configuration', parameters: { resource: 'database', operation: 'executeQuery', query: __SQL_SELECT_ENABLED_ALERTS__, options: { queryBatching: 'independently', queryReplacement: expr('{{ [JSON.stringify($json)] }}') } } }, output: [{ event: { source: 'demo.usgs', external_id: 'example', contract_version: 1, event_type: 'earthquake', data: { magnitude: 5 } }, alerts: [] }] });
const evaluate = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Evaluate typed magnitude alerts', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_EVALUATE_ALERTS__ } }, output: [{ channel: 'slack', destination: 'C123ABC', alert_id: '22222222-2222-4222-8222-222222222222', event: { source: 'demo.usgs', external_id: 'example', data: { magnitude: 5 } } }] });
const loop = splitInBatches({ version: 3, config: { name: 'Process each notification', parameters: { batchSize: 1, options: { reset: false } } }, output: [{ channel: 'slack' }] });
const route = switchCase({ version: 3.4, config: { name: 'Route notification channel', parameters: { mode: 'rules', rules: { values: [
  { outputKey: 'Slack', conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
    { leftValue: expr('{{ $json.channel }}'), rightValue: 'slack', operator: { type: 'string', operation: 'equals' } }
  ], combinator: 'and' } },
  { outputKey: 'Email', conditions: { options: { caseSensitive: true, leftValue: '', typeValidation: 'strict' }, conditions: [
    { leftValue: expr('{{ $json.channel }}'), rightValue: 'email', operator: { type: 'string', operation: 'equals' } }
  ], combinator: 'and' } }
] }, options: { fallbackOutput: 'extra', renameFallbackOutput: 'Diagnostic or unsupported' } } }, output: [{ channel: 'slack' }] });
const message = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Prepare safe Slack text', onError: 'continueErrorOutput', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_PREPARE_SLACK_MESSAGE__ } }, output: [{ destination: 'C123ABC', text: 'Earthquake M 5' }] });
const send = node({ type: 'n8n-nodes-base.slack', version: 2.7, config: { name: 'Send Slack notification', retryOnFail: true, maxTries: 5, waitBetweenTries: 5000, onError: 'continueErrorOutput', parameters: { resource: 'message', operation: 'post', authentication: 'accessToken', select: 'channel', channelId: { __rl: true, mode: 'id', value: expr('{{ $json.destination }}') }, messageType: 'text', text: expr('{{ $json.text }}'), otherOptions: { includeLinkToWorkflow: false, mrkdwn: false, link_names: false, unfurl_links: false, unfurl_media: false } } }, output: [{ ok: true }] });
const accepted = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record accepted Slack diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'accepted-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'accepted-code', name: 'code', value: 'slack_accepted', type: 'string' },
  { id: 'accepted-source', name: 'source', value: expr("{{ $('Process each notification').item.json.event.source }}"), type: 'string' },
  { id: 'accepted-external-id', name: 'external_id', value: expr("{{ $('Process each notification').item.json.event.external_id }}"), type: 'string' },
  { id: 'accepted-alert-id', name: 'alert_id', value: expr("{{ $('Process each notification').item.json.alert_id }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'slack_accepted' }] });
const discarded = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record exhausted Slack diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'discarded-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'discarded-code', name: 'code', value: 'slack_retries_exhausted_discarded', type: 'string' },
  { id: 'discarded-source', name: 'source', value: expr("{{ $('Process each notification').item.json.event.source }}"), type: 'string' },
  { id: 'discarded-external-id', name: 'external_id', value: expr("{{ $('Process each notification').item.json.event.external_id }}"), type: 'string' },
  { id: 'discarded-alert-id', name: 'alert_id', value: expr("{{ $('Process each notification').item.json.alert_id }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'slack_retries_exhausted_discarded' }] });
const invalid = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record invalid Slack diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'invalid-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'invalid-code', name: 'code', value: 'invalid_slack_notification_discarded', type: 'string' },
  { id: 'invalid-source', name: 'source', value: expr("{{ $('Process each notification').item.json.event.source }}"), type: 'string' },
  { id: 'invalid-external-id', name: 'external_id', value: expr("{{ $('Process each notification').item.json.event.external_id }}"), type: 'string' },
  { id: 'invalid-alert-id', name: 'alert_id', value: expr("{{ $('Process each notification').item.json.alert_id }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'invalid_slack_notification_discarded' }] });
const emailMessage = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Prepare safe Email text', onError: 'continueErrorOutput', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_PREPARE_EMAIL_MESSAGE__ } }, output: [{ destination: 'owner@example.test', subject: 'Sonrisa alert: Earthquake', text: 'Earthquake' }] });
const emailSend = node({ type: 'n8n-nodes-base.emailSend', version: 2.1, config: { name: 'Send Email notification', retryOnFail: true, maxTries: 5, waitBetweenTries: 5000, onError: 'continueErrorOutput', parameters: { resource: 'email', operation: 'send', fromEmail: 'sonrisa@example.test', toEmail: expr('{{ $json.destination }}'), subject: expr('{{ $json.subject }}'), emailFormat: 'text', text: expr('{{ $json.text }}'), options: { appendAttribution: false } } }, output: [{ accepted: ['owner@example.test'], rejected: [] }] });
const emailResult = node({ type: 'n8n-nodes-base.code', version: 2, config: { name: 'Record Email result', parameters: { mode: 'runOnceForAllItems', language: 'javaScript', jsCode: __CODE_RECORD_EMAIL_RESULT__ } }, output: [{ channel: 'diagnostic', code: 'email_accepted' }] });
const invalidEmail = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record invalid Email diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'invalid-email-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'invalid-email-code', name: 'code', value: 'invalid_email_notification_discarded', type: 'string' },
  { id: 'invalid-email-source', name: 'source', value: expr("{{ $('Process each notification').item.json.event.source }}"), type: 'string' },
  { id: 'invalid-email-external-id', name: 'external_id', value: expr("{{ $('Process each notification').item.json.event.external_id }}"), type: 'string' },
  { id: 'invalid-email-alert-id', name: 'alert_id', value: expr("{{ $('Process each notification').item.json.alert_id }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'invalid_email_notification_discarded' }] });
const exhaustedEmail = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record exhausted Email diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'exhausted-email-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'exhausted-email-code', name: 'code', value: 'email_retries_exhausted_discarded', type: 'string' },
  { id: 'exhausted-email-source', name: 'source', value: expr("{{ $('Process each notification').item.json.event.source }}"), type: 'string' },
  { id: 'exhausted-email-external-id', name: 'external_id', value: expr("{{ $('Process each notification').item.json.event.external_id }}"), type: 'string' },
  { id: 'exhausted-email-alert-id', name: 'alert_id', value: expr("{{ $('Process each notification').item.json.alert_id }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'email_retries_exhausted_discarded' }] });
const unsupported = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Record unsupported channel diagnostic', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'unsupported-channel', name: 'channel', value: 'diagnostic', type: 'string' },
  { id: 'unsupported-code', name: 'code', value: expr("{{ $json.code ?? 'unsupported_channel' }}"), type: 'string' },
  { id: 'unsupported-source', name: 'source', value: expr("{{ $json.event?.source ?? $json.source ?? '' }}"), type: 'string' },
  { id: 'unsupported-external-id', name: 'external_id', value: expr("{{ $json.event?.external_id ?? $json.external_id ?? '' }}"), type: 'string' },
  { id: 'unsupported-alert-id', name: 'alert_id', value: expr("{{ $json.alert_id ?? '' }}"), type: 'string' }
] } } }, output: [{ channel: 'diagnostic', code: 'unsupported_channel' }] });
const complete = node({ type: 'n8n-nodes-base.set', version: 3.5, config: { name: 'Complete manual processing', parameters: { mode: 'manual', includeOtherFields: false, assignments: { assignments: [
  { id: 'complete-code', name: 'code', value: 'processing_complete', type: 'string' }
] } } }, output: [{ code: 'processing_complete' }] });

export default workflow('sonrisa-process-alerts-dev', 'Sonrisa - Process Alerts - DEV')
  .add(start).to(input).to(selector)
  .to(live.onTrue(fetch.to(liveEnvelope.to(normalize))).onFalse(normalize))
  .add(normalize).to(splitEvents).to(withinInput).to(previousRuns).to(configs).to(evaluate)
  .to(loop.onDone(complete).onEachBatch(route
    .onCase(0, message.to(send.to(accepted.to(nextBatch(loop)))))
    .onCase(1, emailMessage.to(emailSend.to(emailResult.to(nextBatch(loop)))))
    .onCase(2, unsupported.to(nextBatch(loop)))))
  .add(message.onError(invalid.to(nextBatch(loop))))
  .add(send.onError(discarded.to(nextBatch(loop))))
  .add(emailMessage.onError(invalidEmail.to(nextBatch(loop))))
  .add(emailSend.onError(exhaustedEmail.to(nextBatch(loop))));
