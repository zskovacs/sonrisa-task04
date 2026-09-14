UPDATE public.notification_deliveries
SET last_error = 'delivery_outcome_unknown'
WHERE id = $1::uuid AND channel = 'email' AND status = 'processing'
RETURNING id;
