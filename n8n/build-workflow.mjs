import { readFileSync } from 'node:fs';
import { basename } from 'node:path';

const [kind, option, value] = process.argv.slice(2);
if (!['ingest', 'evaluate', 'deliver'].includes(kind)) throw new Error('Expected ingest, evaluate, or deliver');
if (process.argv.length > 5) throw new Error('Too many arguments');
const root = new URL('./', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8');
let mode = 'live';
let fixtureJson = '';
let deliveryId = '';
if (option !== undefined) {
  if (kind === 'ingest' && option === '--fixture' && value && basename(value) === value && /^[a-z0-9-]+\.json$/.test(value)) {
    fixtureJson = JSON.stringify(JSON.parse(read(`fixtures/${value}`)));
    mode = 'fixture';
  } else if (kind === 'deliver' && option === '--delivery-id' && value && /^[0-9a-fA-F-]{36}$/.test(value)) {
    deliveryId = value;
  } else {
    throw new Error('Invalid operator input');
  }
}
const replacements = {
  __INGEST_MODE__: JSON.stringify(mode),
  __INGEST_FIXTURE_JSON__: JSON.stringify(fixtureJson),
  __DELIVERY_ID__: JSON.stringify(deliveryId),
  __EMAIL_SENDER__: JSON.stringify('sonrisa@example.test'),
};
const codes = ['select-ingest-input', 'normalize-earthquakes', 'evaluate-alerts', 'validate-delivery-id', 'prepare-slack-message', 'prepare-email-message', 'validate-email-result'];
for (const name of codes) replacements[`__CODE_${name.replaceAll('-', '_').toUpperCase()}__`] = JSON.stringify(read(`runtime/${name}.js`));
const queries = ['insert-source-events', 'select-pending-event', 'select-event-candidates', 'insert-delivery-intents', 'complete-event', 'claim-delivery', 'select-claimed-delivery', 'record-slack-sent', 'record-slack-failed', 'record-slack-unknown', 'record-email-sent', 'record-email-unknown', 'record-email-invalid'];
for (const name of queries) replacements[`__SQL_${name.replaceAll('-', '_').toUpperCase()}__`] = JSON.stringify(read(`sql/${name}.sql`));
const code = read(`sdk/${kind}.sdk.js`).replace(/__[A-Z0-9_]+__/g, token => {
  if (!(token in replacements)) throw new Error(`Unknown placeholder ${token}`);
  return replacements[token];
});
process.stdout.write(code);
