import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const name = process.argv[2];
const allowed = new Set(['select-ingest-input', 'normalize-earthquakes', 'evaluate-alerts', 'prepare-slack-message']);
if (!allowed.has(name)) throw new Error('Unknown Code node body');
const chunks = [];
for await (const chunk of process.stdin) chunks.push(chunk);
const input = JSON.parse(Buffer.concat(chunks).toString('utf8'));
const items = Array.isArray(input) ? input : [input];
const code = readFileSync(new URL(`runtime/${name}.js`, import.meta.url), 'utf8');
const output = runInNewContext(`(() => { ${code} })()`, { $input: {
  first: () => ({ json: items[0] }),
  all: () => items.map(json => ({ json })),
} });
if (!Array.isArray(output) || output.some(item => !item || typeof item.json !== 'object')) throw new Error('Invalid Code node result');
process.stdout.write(JSON.stringify(output.map(item => item.json)));
