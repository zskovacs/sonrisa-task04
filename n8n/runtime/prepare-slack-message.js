const row = $input.first().json;
if (!row || typeof row.id !== 'string' || typeof row.title !== 'string' || typeof row.destination !== 'string' || typeof row.data?.magnitude !== 'number' || !Number.isFinite(row.data.magnitude)) {
  throw new Error('invalid_claimed_delivery');
}
const escape = value => String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const synthetic = row.source === 'demo.usgs' ? '[SYNTHETIC] ' : '';
const text = `${synthetic}Earthquake M ${row.data.magnitude}: ${escape(row.title)}\nOccurred: ${escape(row.occurred_at)}\nSource: ${escape(row.source)}\nEvent: ${escape(row.source_event_id)}\nAlert: ${escape(row.alert_id)}\nDelivery: ${escape(row.id)}`;
return [{ json: { id: row.id, destination: row.destination, text } }];
