import { readFileSync } from 'node:fs';
import { basename } from 'node:path';

const [kind, option, value] = process.argv.slice(2);
if (kind !== 'process') throw new Error('Expected process');
if (process.argv.length > 5) throw new Error('Too many arguments');
const root = new URL('./', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8');
let mode = 'live';
let fixtureJson = '';
if (option !== undefined) {
  if (option === '--fixture' && value && basename(value) === value && /^[a-z0-9-]+\.json$/.test(value)) {
    fixtureJson = JSON.stringify(JSON.parse(read(`fixtures/${value}`)));
    mode = 'fixture';
  } else throw new Error('Invalid operator input');
}
const replacements = {
  __INGEST_MODE__: JSON.stringify(mode),
  __INGEST_FIXTURE_JSON__: JSON.stringify(fixtureJson),
};
for (const name of ['select-ingest-input', 'normalize-earthquakes', 'evaluate-alerts', 'prepare-slack-message'])
  replacements[`__CODE_${name.replaceAll('-', '_').toUpperCase()}__`] = JSON.stringify(read(`runtime/${name}.js`));
replacements.__SQL_SELECT_ENABLED_ALERTS__ = JSON.stringify(read('sql/select-enabled-alerts.sql'));
const code = read('sdk/process.sdk.js').replace(/__[A-Z0-9_]+__/g, token => {
  if (!(token in replacements)) throw new Error(`Unknown placeholder ${token}`);
  return replacements[token];
});
process.stdout.write(code);
