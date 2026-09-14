WITH inserted AS (
    INSERT INTO public.source_events
        (contract_version, source, external_id, event_type, occurred_at, title, source_url, data)
    SELECT x.contract_version, x.source, x.external_id, x.event_type,
           x.occurred_at, x.title, x.source_url, x.data
    FROM jsonb_to_recordset($1::jsonb) AS x(
        contract_version integer, source text, external_id text, event_type text,
        occurred_at timestamptz, title text, source_url text, data jsonb)
    ON CONFLICT (source, external_id) DO NOTHING
    RETURNING id
)
SELECT count(*)::integer AS inserted_count FROM inserted;
