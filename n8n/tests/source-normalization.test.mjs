import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const root = new URL('..', import.meta.url);
const file = (path) => readFileSync(new URL(path, root), 'utf8');
const fixture = JSON.parse(file('fixtures/earthquakes.json'));
const run = (name, json) => {
  const code = file(`runtime/${name}.js`);
  const result = runInNewContext(`(() => { ${code} })()`, { $input: { first: () => ({ json }) } });
  assert.equal(result.length, 1);
  return JSON.parse(JSON.stringify(result[0].json));
};

test('manual selector defaults to live and reserves fixture namespace', () => {
  assert.deepEqual(run('select-ingest-input', { mode: 'live', fixture_json: '' }), { mode: 'live' });
  const synthetic = run('select-ingest-input', { mode: 'fixture', fixture_json: JSON.stringify(fixture) });
  assert.equal(synthetic.source, 'demo.usgs');
  assert.equal(synthetic.feed.features.length, 3);
  assert.throws(() => run('select-ingest-input', { mode: 'other' }), /invalid_ingest_mode/);
  assert.throws(() => run('select-ingest-input', { mode: 'fixture', fixture_json: '' }), /invalid_fixture_feed/);
});

test('normalizer accepts finite numbers, bounds identity and isolates bad records', () => {
  const feed = structuredClone(fixture);
  feed.features.push({ type: 'Feature', id: 'bad-mag', properties: { type: 'earthquake', mag: '5', time: 1789392000000 } });
  feed.features.push({ id: 'bad-id with space', properties: { mag: 5, time: 1789392000000 } });
  feed.features.push({ type: 'Feature', id: 'bad-time', properties: { type: 'earthquake', mag: 5, time: null } });
  const out = run('normalize-earthquakes', { source: 'demo.usgs', feed });
  assert.equal(out.events.length, 3);
  assert.deepEqual(out.events.map(x => x.data.magnitude), [4.9, 5, 5.1]);
  assert.equal(out.events[0].contract_version, 1);
  assert.equal(out.events[0].title, 'M 4.9 - <@U123> & coast');
  assert.deepEqual(out.diagnostics.map(x => x.code), ['invalid_magnitude', 'invalid_external_id', 'invalid_occurrence']);
  assert.throws(() => run('normalize-earthquakes', { source: 'usgs', feed: { features: [] } }), /invalid_feed/);
});

test('normalizer rejects unsupported feature and out-of-range UTC without discarding valid records', () => {
  const feed = structuredClone(fixture);
  feed.features[0].properties.type = 'quarry blast';
  feed.features[1].properties.time = 253402300800000;
  feed.features[2].properties.title = 'M 5.1 - \u0085Safe';
  feed.features[2].properties.url = 'HTTPS://example.test/quake';
  const out = run('normalize-earthquakes', { source: 'usgs', feed });
  assert.equal(out.events.length, 1);
  assert.equal(out.events[0].title, 'M 5.1 -  Safe');
  assert.equal(out.events[0].source_url, null);
  assert.deepEqual(out.diagnostics.map(x => x.code), ['unsupported_feature', 'invalid_occurrence']);
});

test('normalizer omits invalid optional URL and rejects oversized feed', () => {
  const feed = structuredClone(fixture);
  feed.features[0].properties.url = 'javascript:alert(1)';
  assert.equal(run('normalize-earthquakes', { source: 'usgs', feed }).events[0].source_url, null);
  assert.throws(() => run('normalize-earthquakes', { source: 'usgs', feed: { type: 'FeatureCollection', features: Array(1001).fill(fixture.features[0]) } }), /feed_too_many_features/);
});
