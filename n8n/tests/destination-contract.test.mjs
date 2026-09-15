import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { runInNewContext } from 'node:vm';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
const vectors = JSON.parse(read('../../tests/fixtures/destination-contract.json'));
const run = (name, values) => JSON.parse(JSON.stringify(runInNewContext(
  `(() => { ${read(`../runtime/${name}.js`)} })()`,
  { $input: { all: () => values.map(json => ({ json })) } },
)));
const event = {
  contract_version: 1,
  source: 'demo.usgs',
  external_id: 'event-1',
  event_type: 'earthquake',
  occurred_at: '2026-09-14T16:00:00.000Z',
  title: 'Offshore quake',
  source_url: null,
  data: { magnitude: 5.1 },
};
const alert = vector => ({
  id: '22222222-2222-4222-8222-222222222222',
  name: 'Coastal watch',
  enabled: true,
  event_type: 'earthquake',
  condition_field: 'magnitude',
  condition_operator: 'gte',
  condition_value_type: 'number',
  condition_value: 5,
  email_destination: vector.normalizedEmail,
  slack_destination: vector.normalizedSlack,
});
const emailNotification = destination => ({
  event,
  alert_id: '22222222-2222-4222-8222-222222222222',
  alert_name: 'Coastal watch',
  channel: 'email',
  destination,
});

test('evaluator and Email preparer share the persisted destination contract', () => {
  for (const vector of vectors) {
    const output = run('evaluate-alerts', [{ event, alerts: [alert(vector)] }]);
    const notifications = output.filter(item => item.json.channel !== 'diagnostic').map(item => item.json);
    const diagnostics = output.filter(item => item.json.channel === 'diagnostic').map(item => item.json.code);

    assert.deepEqual(notifications.map(item => item.channel), vector.runtimeChannels, vector.name);
    assert.deepEqual(diagnostics, vector.runtimeDiagnostics, vector.name);
    for (const notification of notifications) {
      const expected = notification.channel === 'email' ? vector.normalizedEmail : vector.normalizedSlack;
      assert.equal(notification.destination, expected, `${vector.name}: runtime destination must be normalized`);
    }

    if (vector.normalizedEmail !== null) {
      const prepare = () => run('prepare-email-message', [emailNotification(vector.normalizedEmail)]);
      if (vector.runtimeChannels.includes('email')) {
        assert.equal(prepare()[0].json.destination, vector.normalizedEmail, vector.name);
      } else {
        assert.throws(prepare, { message: 'invalid_email_notification' }, vector.name);
      }
    }
  }
});

test('runtime rejects raw persisted Email controls before transport preparation', () => {
  const destination = 'owner@example.test\n';
  const output = run('evaluate-alerts', [{ event, alerts: [alert({
    normalizedEmail: destination,
    normalizedSlack: null,
  })] }]);

  assert.deepEqual(output.map(item => item.json.code), ['invalid_email_destination']);
  assert.throws(
    () => run('prepare-email-message', [emailNotification(destination)]),
    { message: 'invalid_email_notification' },
  );
});
