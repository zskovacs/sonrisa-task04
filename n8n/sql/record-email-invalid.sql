UPDATE public.notification_deliveries
SET status = 'failed', last_error = 'invalid_email_message'
WHERE id = $1::uuid AND channel = 'email' AND status = 'processing'
RETURNING id;
