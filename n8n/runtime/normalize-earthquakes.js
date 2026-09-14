const input = $input.first().json;
if ((input.source !== 'usgs' && input.source !== 'demo.usgs') || !input.feed || input.feed.type !== 'FeatureCollection' || !Array.isArray(input.feed.features)) {
  throw new Error('invalid_feed');
}
const features = input.feed.features;
if (features.length > 1000) throw new Error('feed_too_many_features');
let size;
try {
  size = encodeURIComponent(JSON.stringify(input.feed)).replace(/%[0-9A-F]{2}/gi, 'x').length;
} catch {
  throw new Error('invalid_feed');
}
if (size > 2097152) throw new Error('feed_too_large');
const events = [];
const diagnostics = [];
for (const feature of features) {
  const id = feature && feature.id;
  const safeId = typeof id === 'string' && /^[!-~]{1,128}$/.test(id) ? id : null;
  if (!safeId) {
    diagnostics.push({ code: 'invalid_external_id' });
    continue;
  }
  const properties = feature && feature.properties;
  if (feature.type !== 'Feature' || !properties || properties.type !== 'earthquake') {
    diagnostics.push({ code: 'unsupported_feature', external_id: safeId });
    continue;
  }
  const mag = properties && properties.mag;
  if (typeof mag !== 'number' || !Number.isFinite(mag)) {
    diagnostics.push({ code: 'invalid_magnitude', external_id: safeId });
    continue;
  }
  const ms = properties.time;
  if (typeof ms !== 'number' || !Number.isFinite(ms) || !Number.isInteger(ms) || ms < -62135596800000 || ms > 253402300799999) {
    diagnostics.push({ code: 'invalid_occurrence', external_id: safeId });
    continue;
  }
  const occurred = new Date(ms).toISOString();
  const place = typeof properties.place === 'string' ? properties.place.replace(/[\x00-\x1f\x7f-\x9f]/g, ' ').trim().slice(0, 200) : '';
  const fallback = place ? `M ${mag} - ${place}` : `M ${mag} - Earthquake`;
  const rawTitle = typeof properties.title === 'string' && properties.title.trim() ? properties.title : fallback;
  const title = rawTitle.replace(/[\x00-\x1f\x7f-\x9f]/g, ' ').trim().slice(0, 300).trim() || 'Earthquake';
  let url = null;
  if (typeof properties.url === 'string' && properties.url.length <= 2048 && /^https?:\/\/[^\s\x00-\x1f\x7f-\x9f]+$/.test(properties.url)) {
    const authority = /^https?:\/\/([^\/?#]+)/.exec(properties.url)?.[1];
    if (authority && /^[A-Za-z0-9.-]+(?::[0-9]{1,5})?$/.test(authority)) url = properties.url;
  }
  events.push({ contract_version: 1, source: input.source, external_id: safeId, event_type: 'earthquake', occurred_at: occurred, title, source_url: url, data: { magnitude: mag } });
}
return [{ json: { events, diagnostics } }];
