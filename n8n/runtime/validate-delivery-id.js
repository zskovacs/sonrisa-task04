const value = $input.first().json.delivery_id;
if (typeof value !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(value)) {
  throw new Error('invalid_delivery_id');
}
return [{ json: { delivery_id: value.toLowerCase() } }];
