const input = $input.first().json;
const event = input.event;
if (!event || typeof event.id !== 'string' || !Array.isArray(input.alerts)) throw new Error('invalid_evaluation_envelope');
const intents = [];
const diagnostics = [];
const magnitude = event.data && event.data.magnitude;
if (event.event_type !== 'earthquake' || typeof magnitude !== 'number' || !Number.isFinite(magnitude)) {
  diagnostics.push({ code: 'invalid_event_magnitude' });
  return [{ json: { event_id: event.id, intents, diagnostics } }];
}
for (const alert of input.alerts) {
  if (!alert || alert.enabled !== true) continue;
  const id = typeof alert.id === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(alert.id) && !/^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(alert.id) ? alert.id : null;
  if (!id) { diagnostics.push({ code: 'invalid_alert_id' }); continue; }
  if (alert.event_type !== 'earthquake' || alert.condition_field !== 'magnitude' || alert.condition_operator !== 'gte' || alert.condition_value_type !== 'number') {
    diagnostics.push({ code: 'unsupported_condition', alert_id: id });
    continue;
  }
  if (typeof alert.condition_value !== 'number' || !Number.isFinite(alert.condition_value)) {
    diagnostics.push({ code: 'invalid_threshold', alert_id: id });
    continue;
  }
  if (magnitude < alert.condition_value) continue;
  const slack = alert.slack_destination;
  const email = alert.email_destination;
  if (slack !== null && slack !== undefined && (typeof slack !== 'string' || !/^[A-Z0-9]{2,80}$/.test(slack))) {
    diagnostics.push({ code: 'invalid_slack_destination', alert_id: id });
    continue;
  }
  if (email !== null && email !== undefined && (typeof email !== 'string' || email.length > 254 || !/^[^\s\x00-\x1f\x7f@]+@[^\s\x00-\x1f\x7f@]+$/.test(email))) {
    diagnostics.push({ code: 'invalid_email_destination', alert_id: id });
    continue;
  }
  if (slack) intents.push({ alert_id: id, channel: 'slack', destination: slack, status: 'pending', last_error: null });
  if (email) intents.push({ alert_id: id, channel: 'email', destination: email, status: 'pending', last_error: null });
}
return [{ json: { event_id: event.id, intents, diagnostics } }];
