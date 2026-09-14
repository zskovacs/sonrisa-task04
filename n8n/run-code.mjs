import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const name = process.argv[2];
const allowed = new Set(['select-ingest-input', 'normalize-earthquakes', 'evaluate-alerts', 'validate-delivery-id', 'prepare-slack-message']);
if (!allowed.has(name)) throw new Error('Unknown Code node body');
const chunks = [];
for await (const chunk of process.stdin) chunks.push(chunk);
const input = JSON.parse(Buffer.concat(chunks).toString('utf8'));
const code = readFileSync(new URL(`runtime/${name}.js`, import.meta.url), 'utf8');
const output = runInNewContext(`(() => { ${code} })()`, { $input: { first: () => ({ json: input }) } });
if (!Array.isArray(output) || output.length !== 1 || !output[0] || typeof output[0].json !== 'object') throw new Error('Invalid Code node result');
process.stdout.write(JSON.stringify(output[0].json));
