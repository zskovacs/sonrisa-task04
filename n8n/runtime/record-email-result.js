const output = [];
for (const [index, item] of $input.all().entries()) {
  const origin = $('Process each notification').item.json;
  const result = item.json ?? {};
  const destination = origin?.destination;
  const accepted = result.accepted;
  const rejected = result.rejected;
  const envelope = result.envelope;
  const confirmed = result && typeof result === 'object' && typeof destination === 'string' && destination.length > 0 && !('error' in result) && !('errors' in result) &&
    Array.isArray(accepted) && accepted.length === 1 && accepted[0] === destination &&
    Array.isArray(rejected) && rejected.length === 0 &&
    (envelope === undefined || (envelope && Array.isArray(envelope.to) && envelope.to.length === 1 && envelope.to[0] === destination));
  const source = origin?.event?.source;
  const externalId = origin?.event?.external_id;
  const alertId = origin?.alert_id;
  output.push({ json: { channel: 'diagnostic', code: confirmed ? 'email_accepted' : 'email_outcome_unconfirmed_discarded',
    ...(typeof source === 'string' && /^(?:usgs|demo\.usgs)$/.test(source) ? { source } : {}),
    ...(typeof externalId === 'string' && /^[!-~]{1,128}$/.test(externalId) ? { external_id: externalId } : {}),
    ...(typeof alertId === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(alertId) ? { alert_id: alertId } : {})
  }, pairedItem: { item: index } });
}
return output;
