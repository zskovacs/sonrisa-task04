const row = $input.first().json;
const sender = $('Operator delivery ID').first().json.sender_email;
if (!row || typeof row.id !== 'string') throw new Error('invalid_claimed_delivery');
const invalid = [{ json: { id: row.id, valid: false } }];
const uuid = value => typeof value === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) && !/^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(value);
const mailbox = value => {
  if (typeof value !== 'string' || value.length > 254 || value.includes('..')) return false;
  const parts = value.split('@');
  if (parts.length !== 2 || parts[0].length > 64 || !/^[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]+$/.test(parts[0]) || parts[0].startsWith('.') || parts[0].endsWith('.')) return false;
  const labels = parts[1].split('.');
  return labels.length >= 2 && labels.every(label => /^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/.test(label));
};
if (row.channel !== 'email' || !uuid(row.id) || !uuid(row.source_event_id) || !uuid(row.alert_id) ||
    !mailbox(row.destination) || !mailbox(sender) ||
    !['usgs', 'demo.usgs'].includes(row.source) || typeof row.title !== 'string' ||
    row.title.length < 1 || row.title.length > 300 || /[\x00-\x1f\x7f-\x9f]/.test(row.title) ||
    typeof row.occurred_at !== 'string' || row.occurred_at.length > 40 ||
    !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?(?:Z|[+-]\d{2}:\d{2})$/.test(row.occurred_at) ||
    typeof row.data?.magnitude !== 'number' || !Number.isFinite(row.data.magnitude)) return invalid;
const synthetic = row.source === 'demo.usgs' ? '[SYNTHETIC] ' : '';
const subject = `${synthetic}Earthquake M ${row.data.magnitude} | Delivery ${row.id}`;
const text = `${synthetic}Earthquake M ${row.data.magnitude}: ${row.title}\nOccurred: ${row.occurred_at}\nSource: ${row.source}\nEvent: ${row.source_event_id}\nAlert: ${row.alert_id}\nDelivery: ${row.id}`;
return [{ json: { id: row.id, valid: true, sender, destination: row.destination, subject, text } }];
