const result = $input.first().json;
const prepared = $('Prepare safe email message').first().json;
const expected = prepared.destination;
const envelope = result?.envelope;
const envelopeMatches = envelope === undefined ||
  (envelope && envelope.from === prepared.sender && Array.isArray(envelope.to) &&
   envelope.to.length === 1 && envelope.to[0] === expected);
const accepted = prepared.valid === true && typeof expected === 'string' && expected.length > 0 &&
  typeof prepared.sender === 'string' && prepared.sender.length > 0 &&
  result && typeof result === 'object' && !Object.hasOwn(result, 'error') &&
  Array.isArray(result.accepted) && result.accepted.length === 1 &&
  result.accepted[0] === expected && Array.isArray(result.rejected) &&
  result.rejected.length === 0 && envelopeMatches;
return [{ json: { id: prepared.id, accepted } }];
