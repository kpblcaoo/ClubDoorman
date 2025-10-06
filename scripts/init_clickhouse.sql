-- ClickHouse bootstrap for Spampyre analytics
CREATE DATABASE IF NOT EXISTS tg;

CREATE TABLE IF NOT EXISTS tg.messages_raw
(
    event_ts      DateTime           CODEC(Delta, ZSTD),
    ingest_ts     DateTime           CODEC(Delta, ZSTD),
    chat_id       Int64,
    chat_type     LowCardinality(String),
    msg_id        Int64,
    from_id       Int64,
    from_is_bot   UInt8,
    text_len      UInt16,
    has_url       UInt8,
    has_media     UInt8,
    reply_to_id   Int64             DEFAULT 0,
    ingest_source LowCardinality(String)
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(event_ts)
ORDER BY (chat_id, event_ts, msg_id)
SETTINGS index_granularity = 8192;

CREATE TABLE IF NOT EXISTS tg.metrics_minute
(
    chat_id               Int64,
    bucket                DateTime,
    msg_cnt_state         AggregateFunction(count),
    uniq_senders_state    AggregateFunction(uniqExact, Int64),
    url_cnt_state         AggregateFunction(sum, UInt64),
    media_cnt_state       AggregateFunction(sum, UInt64),
    text_len_avg_state    AggregateFunction(avg, UInt16)
)
ENGINE = AggregatingMergeTree
PARTITION BY toYYYYMM(bucket)
ORDER BY (chat_id, bucket);

CREATE MATERIALIZED VIEW IF NOT EXISTS tg.mv_metrics_minute
TO tg.metrics_minute
AS
SELECT
    chat_id,
    toStartOfMinute(event_ts) AS bucket,
    countState()                                AS msg_cnt_state,
    uniqExactState(from_id)                     AS uniq_senders_state,
    sumState(toUInt64(has_url))                 AS url_cnt_state,
    sumState(toUInt64(has_media))               AS media_cnt_state,
    avgState(text_len)                          AS text_len_avg_state
FROM tg.messages_raw
GROUP BY chat_id, bucket;

CREATE VIEW IF NOT EXISTS tg.metrics_minute_v
AS
SELECT
    chat_id,
    bucket,
    countMerge(msg_cnt_state)              AS msg_cnt,
    uniqExactMerge(uniq_senders_state)     AS uniq_senders,
    sumMerge(url_cnt_state)                AS url_cnt,
    sumMerge(media_cnt_state)              AS media_cnt,
    avgMerge(text_len_avg_state)           AS avg_text_len
FROM tg.metrics_minute
GROUP BY chat_id, bucket
ORDER BY chat_id, bucket;
