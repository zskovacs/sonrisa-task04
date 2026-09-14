const output = [];
const escape = value => String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
for (const item of $input.all()) {
  const { event, alert_id: alertId, channel, destination } = item.json ?? {};
  if (channel !== 'slack' || typeof destination !== 'string' || !/^[A-Z0-9]{2,80}$/.test(destination) || typeof event?.external_id !== 'string' || !/^[!-~]{1,128}$/.test(event.external_id) || typeof event?.title !== 'string' || typeof event?.data?.magnitude !== 'number' || !Number.isFinite(event.data.magnitude)) {
    throw new Error('invalid_slack_notification');
  }
  const synthetic = event.source === 'demo.usgs' ? '[SYNTHETIC] ' : '';
  const text = `${synthetic}Earthquake M ${event.data.magnitude}: ${escape(event.title)}\nOccurred: ${escape(event.occurred_at)}\nSource: ${escape(event.source)}\nEvent: ${escape(event.external_id)}\nAlert: ${escape(alertId)}`;
  output.push({ json: { destination, text } });
}
return output;
