UPDATE public.notification_deliveries
SET status = 'failed', last_error = 'slack_rejected'
WHERE id = $1::uuid AND channel = 'slack' AND status = 'processing'
RETURNING id;
