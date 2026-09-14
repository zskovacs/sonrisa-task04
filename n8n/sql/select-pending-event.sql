SELECT id
FROM public.source_events
WHERE processing_status = 'pending'
ORDER BY received_at, id
LIMIT 1;
