UPDATE public.notification_deliveries
SET status = 'sent', sent_at = now(), last_error = NULL
WHERE id = $1::uuid AND channel = 'slack' AND status = 'processing'
RETURNING id;
