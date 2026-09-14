WITH claimed AS (
    UPDATE public.notification_deliveries
    SET status = 'processing', last_error = NULL
    WHERE id = $1::uuid AND channel = 'slack' AND status = 'pending'
    RETURNING id, source_event_id, alert_id, destination
)
SELECT id, source_event_id, alert_id, destination
FROM claimed;
