SELECT d.id, d.source_event_id, d.alert_id, d.destination,
       e.source, e.external_id, e.title, e.occurred_at, e.data
FROM public.notification_deliveries AS d
JOIN public.source_events AS e ON e.id = d.source_event_id
WHERE d.id = $1::uuid AND d.channel = 'slack' AND d.status = 'processing';
