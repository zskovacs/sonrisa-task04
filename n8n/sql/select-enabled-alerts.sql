WITH input AS (SELECT $1::jsonb AS event)
SELECT input.event,
       COALESCE(jsonb_agg(jsonb_build_object(
           'id', a.id, 'enabled', a.enabled, 'event_type', a.event_type,
           'condition_field', a.condition_field,
           'condition_operator', a.condition_operator,
           'condition_value_type', a.condition_value_type,
           'condition_value', a.condition_value,
           'email_destination', u.email_destination,
           'slack_destination', u.slack_destination
       ) ORDER BY a.id) FILTER (WHERE a.id IS NOT NULL), '[]'::jsonb) AS alerts
FROM input
LEFT JOIN (public.alerts AS a JOIN public.users AS u ON u.id = a.owner_id)
       ON a.enabled = true AND a.event_type = input.event->>'event_type'
GROUP BY input.event;
