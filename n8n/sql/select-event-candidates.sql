WITH selected AS (
    SELECT id, contract_version, source, external_id, event_type, occurred_at,
           title, source_url, data, received_at, processing_status
    FROM public.source_events
    WHERE id = $1::uuid AND processing_status = 'pending'
)
SELECT to_jsonb(e) AS event,
       COALESCE(c.alerts, '[]'::jsonb) AS alerts
FROM selected AS e
LEFT JOIN LATERAL (
    SELECT jsonb_agg(jsonb_build_object(
        'id', a.id,
        'enabled', a.enabled,
        'event_type', a.event_type,
        'condition_field', a.condition_field,
        'condition_operator', a.condition_operator,
        'condition_value_type', a.condition_value_type,
        'condition_value', a.condition_value,
        'email_destination', u.email_destination,
        'slack_destination', u.slack_destination
    ) ORDER BY a.id) AS alerts
    FROM public.alerts AS a
    JOIN public.users AS u ON u.id = a.owner_id
    WHERE a.enabled = true
) AS c ON true;
