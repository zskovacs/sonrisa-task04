const output = [];
const safeEvent = event => ({
  source: typeof event?.source === 'string' && /^(?:usgs|demo\.usgs)$/.test(event.source) ? event.source : undefined,
  external_id: typeof event?.external_id === 'string' && /^[!-~]{1,128}$/.test(event.external_id) ? event.external_id : undefined,
});
const diagnostic = (code, event, alertId) => ({ json: { channel: 'diagnostic', code, ...safeEvent(event), ...(alertId ? { alert_id: alertId } : {}) } });
const validAlertId = id => typeof id === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id) && !/^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(id);
const validEmail = value => typeof value === 'string' && value.length <= 254 && /^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@[A-Za-z0-9]+(?:[A-Za-z0-9-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9]+(?:[A-Za-z0-9-]*[A-Za-z0-9])?)+$/.test(value);
for (const item of $input.all()) {
  const { event, alerts } = item.json ?? {};
  const magnitude = event?.data?.magnitude;
  if (event?.contract_version !== 1 || typeof event?.source !== 'string' || !/^(?:usgs|demo\.usgs)$/.test(event.source) || typeof event?.external_id !== 'string' || !/^[!-~]{1,128}$/.test(event.external_id) || event?.event_type !== 'earthquake' || typeof magnitude !== 'number' || !Number.isFinite(magnitude) || !Array.isArray(alerts)) {
    output.push(diagnostic('invalid_event_or_envelope', event));
    continue;
  }
  for (const alert of alerts) {
    if (!alert || alert.enabled !== true) continue;
    if (!validAlertId(alert.id)) { output.push(diagnostic('invalid_alert_id', event)); continue; }
    const id = alert.id;
    if (alert.event_type !== 'earthquake' || alert.condition_field !== 'magnitude' || alert.condition_operator !== 'gte' || alert.condition_value_type !== 'number') {
      output.push(diagnostic('unsupported_condition', event, id));
      continue;
    }
    if (typeof alert.condition_value !== 'number' || !Number.isFinite(alert.condition_value)) {
      output.push(diagnostic('invalid_threshold', event, id));
      continue;
    }
    if (magnitude < alert.condition_value) continue;
    const notification = (channel, destination) => ({ json: { event, alert_id: id, alert_name: alert.name, channel, destination } });
    const slack = alert.slack_destination;
    if (slack !== null && slack !== undefined) {
      if (typeof slack !== 'string' || !/^[A-Z0-9]{2,80}$/.test(slack)) output.push(diagnostic('invalid_slack_destination', event, id));
      else output.push(notification('slack', slack));
    }
    const email = alert.email_destination;
    if (email !== null && email !== undefined) {
      if (!validEmail(email)) output.push(diagnostic('invalid_email_destination', event, id));
      else output.push(notification('email', email));
    }
  }
}
return output;
