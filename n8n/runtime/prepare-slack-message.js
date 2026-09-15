const output = [];
const escape = value => String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
for (const item of $input.all()) {
  const { event, alert_name: alertName, channel, destination } = item.json ?? {};
  const occurred = typeof event?.occurred_at === 'string' && Number.isFinite(Date.parse(event.occurred_at)) ? new Date(event.occurred_at).toISOString() : '';
  if (channel !== 'slack' || typeof destination !== 'string' || !/^[A-Z0-9]{2,80}$/.test(destination) || typeof event?.external_id !== 'string' || !/^[!-~]{1,128}$/.test(event.external_id) || typeof event?.title !== 'string' || !occurred || typeof event?.data?.magnitude !== 'number' || !Number.isFinite(event.data.magnitude)) {
    throw new Error('invalid_slack_notification');
  }
  const name = typeof alertName === 'string' && !/[\u0000-\u001f\u007f-\u009f]/.test(alertName) ? alertName.replace(/\s+/g, ' ').trim().slice(0, 120) : '';
  const synthetic = event.source === 'demo.usgs' ? '[SYNTHETIC] ' : '';
  const lines = [`${synthetic}Earthquake M ${event.data.magnitude}: ${escape(event.title)}`, `Occurred: ${occurred}`, `Source: ${escape(event.source)}`, ...(name ? [`Alert: ${escape(name)}`] : [])];
  const text = lines.join('\n');
  output.push({ json: { destination, text } });
}
return output;
