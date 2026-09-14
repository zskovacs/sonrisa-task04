const output = [];
const mailbox = value => typeof value === 'string' && value.length <= 254 && /^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@[A-Za-z0-9]+(?:[A-Za-z0-9-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9]+(?:[A-Za-z0-9-]*[A-Za-z0-9])?)+$/.test(value);
const clean = (value, limit) => typeof value === 'string' ? value.replace(/[\u0000-\u001f\u007f-\u009f]/g, ' ').replace(/\s+/g, ' ').trim().slice(0, limit) : '';
for (const [index, item] of $input.all().entries()) {
  const { event, alert_name: alertName, channel, destination } = item.json ?? {};
  const magnitude = event?.data?.magnitude;
  const title = clean(event?.title, 160);
  const occurred = typeof event?.occurred_at === 'string' && Number.isFinite(Date.parse(event.occurred_at)) ? new Date(event.occurred_at).toISOString() : '';
  if (channel !== 'email' || !mailbox(destination) || event?.contract_version !== 1 || !/^(?:usgs|demo\.usgs)$/.test(event?.source) || event?.event_type !== 'earthquake' || !title || !occurred || typeof magnitude !== 'number' || !Number.isFinite(magnitude)) {
    throw new Error('invalid_email_notification');
  }
  const name = typeof alertName === 'string' && !/[\u0000-\u001f\u007f-\u009f]/.test(alertName) ? clean(alertName, 120) : '';
  let sourceUrl = '';
  if (typeof event.source_url === 'string' && event.source_url.length <= 2048 && /^https?:\/\/[^\s\u0000-\u001f\u007f-\u009f]+$/.test(event.source_url)) {
    const authority = /^https?:\/\/([^\/?#]+)/.exec(event.source_url)?.[1];
    const parts = /^([^:]+)(?::([0-9]{1,5}))?$/.exec(authority ?? '');
    const host = parts?.[1];
    const port = parts?.[2];
    if (host && host.length <= 253 && host.split('.').every(label => /^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/.test(label)) && (!port || (Number(port) >= 1 && Number(port) <= 65535))) sourceUrl = event.source_url;
  }
  const synthetic = event.source === 'demo.usgs' ? '[SYNTHETIC] ' : '';
  const lines = [`${synthetic}Sonrisa earthquake alert`, ...(name ? [`Alert: ${name}`] : []), `Earthquake: M ${magnitude} — ${title}`, `Occurred: ${occurred}`, `Source: ${event.source === 'usgs' ? 'USGS' : 'USGS (synthetic test)'}`, ...(sourceUrl ? [`Event URL: ${sourceUrl}`] : [])];
  output.push({ json: { destination, subject: `${synthetic}Sonrisa alert: ${title}`, text: lines.join('\n') }, pairedItem: { item: index } });
}
return output;
