WITH inserted AS (
    INSERT INTO public.notification_deliveries
        (source_event_id, alert_id, channel, destination, status, last_error)
    SELECT ($1::jsonb->>'event_id')::uuid, x.alert_id, x.channel, x.destination, x.status, x.last_error
    FROM jsonb_to_recordset($1::jsonb->'intents') AS x(
        alert_id uuid, channel text, destination text, status text, last_error text)
    ON CONFLICT (source_event_id, alert_id, channel) DO NOTHING
    RETURNING id
)
SELECT count(*)::integer AS inserted_count FROM inserted;
