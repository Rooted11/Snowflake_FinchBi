-- =============================================================
--  Sypher BI — Snowflake schema (DDL only)
--  Run this in a Snowflake worksheet, then load data with
--  tools/neon_to_snowflake.py (copies rows from Neon → Snowflake).
--
--  Notes vs the Postgres schema:
--    SERIAL        -> INT AUTOINCREMENT
--    TIMESTAMP     -> TIMESTAMP_NTZ
--    TEXT          -> VARCHAR
--  Snowflake accepts PRIMARY KEY / FOREIGN KEY syntax but does not
--  enforce it; kept here purely as documentation of the model.
-- =============================================================

-- Optional: provision a database/warehouse to match appsettings defaults.
-- CREATE DATABASE IF NOT EXISTS SYPHER_BI;
-- CREATE WAREHOUSE IF NOT EXISTS COMPUTE_WH WITH WAREHOUSE_SIZE = 'XSMALL' AUTO_SUSPEND = 60;
-- USE DATABASE SYPHER_BI;
-- USE SCHEMA PUBLIC;

CREATE TABLE IF NOT EXISTS segments (
  id          VARCHAR(20)  PRIMARY KEY,
  label       VARCHAR(60)  NOT NULL,
  avg_gift    INT          NOT NULL,
  donor_count INT          NOT NULL,
  color       VARCHAR(10)  NOT NULL,
  icon        VARCHAR(40)  NOT NULL
);

CREATE TABLE IF NOT EXISTS campaigns (
  id    VARCHAR(20) PRIMARY KEY,
  label VARCHAR(60) NOT NULL,
  color VARCHAR(10) NOT NULL,
  goal  INT         NOT NULL
);

CREATE TABLE IF NOT EXISTS channels (
  id       VARCHAR(20) PRIMARY KEY,
  label    VARCHAR(40) NOT NULL,
  color    VARCHAR(10) NOT NULL,
  base_pct INT         NOT NULL
);

CREATE TABLE IF NOT EXISTS donors (
  name       VARCHAR(80) PRIMARY KEY,
  segment_id VARCHAR(20) NOT NULL REFERENCES segments(id),
  max_gift   INT         NOT NULL,
  first_gift VARCHAR(10) NOT NULL,
  lifecycle  VARCHAR(20) NOT NULL
);

CREATE TABLE IF NOT EXISTS callers (
  name       VARCHAR(80)  PRIMARY KEY,
  role       VARCHAR(30)  NOT NULL,
  tenure     VARCHAR(10)  NOT NULL,
  conv_boost NUMBER(4,2)  NOT NULL
);

CREATE TABLE IF NOT EXISTS donations (
  id          INT          AUTOINCREMENT PRIMARY KEY,
  gift_date   DATE         NOT NULL,
  donor_name  VARCHAR(80)  NOT NULL REFERENCES donors(name),
  campaign_id VARCHAR(20)  NOT NULL REFERENCES campaigns(id),
  channel_id  VARCHAR(20)  NOT NULL REFERENCES channels(id),
  amount      INT          NOT NULL,
  status      VARCHAR(10)  NOT NULL
);

CREATE TABLE IF NOT EXISTS calls (
  id           INT           AUTOINCREMENT PRIMARY KEY,
  call_time    TIMESTAMP_NTZ NOT NULL,
  caller_name  VARCHAR(80)   NOT NULL REFERENCES callers(name),
  contact      VARCHAR(80)   NOT NULL REFERENCES donors(name),
  duration_sec INT           NOT NULL,
  outcome      VARCHAR(12)   NOT NULL,
  pledge       INT           NOT NULL DEFAULT 0,
  note_text    VARCHAR,
  note_context VARCHAR(80)
);
