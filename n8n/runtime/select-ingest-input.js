const input = $input.first().json;
if (input.mode === 'live' && input.fixture_json === '') {
  return [{ json: { mode: 'live' } }];
}
if (input.mode !== 'fixture') {
  throw new Error('invalid_ingest_mode');
}
if (typeof input.fixture_json !== 'string' || input.fixture_json.length === 0 || input.fixture_json.length > 2097152) throw new Error('invalid_fixture_feed');
let feed;
try {
  feed = JSON.parse(input.fixture_json);
} catch {
  throw new Error('invalid_fixture_feed');
}
return [{ json: { mode: 'fixture', source: 'demo.usgs', feed } }];
