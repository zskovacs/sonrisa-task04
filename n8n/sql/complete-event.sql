UPDATE public.source_events
SET processing_status = 'evaluated'
WHERE id = $1::uuid AND processing_status = 'pending'
RETURNING id;
