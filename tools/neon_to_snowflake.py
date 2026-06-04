#!/usr/bin/env python3
"""
Sypher BI — Neon → Snowflake loader.

Copies the Sypher schema + data from the live Neon (Postgres) database into
Snowflake so the API can run with Database:Provider = "Snowflake". Idempotent:
each table is TRUNCATEd and reloaded.

Usage:
    pip install -r tools/requirements.txt
    # Neon (Postgres) source:
    export NEON_HOST=...        NEON_DB=neondb  NEON_USER=neondb_owner  NEON_PASSWORD=...
    # Snowflake target:
    export SNOWFLAKE_ACCOUNT=...  SNOWFLAKE_USER=...   SNOWFLAKE_PASSWORD=...
    export SNOWFLAKE_ROLE=SYSADMIN SNOWFLAKE_WAREHOUSE=COMPUTE_WH
    export SNOWFLAKE_DATABASE=SYPHER_BI SNOWFLAKE_SCHEMA=PUBLIC
    python tools/neon_to_snowflake.py

Nothing runs automatically — this is a manual/cron ETL utility.
"""

import os
import sys
import pathlib

import psycopg2
import snowflake.connector

# Load order respects foreign keys (parents before children).
TABLE_ORDER = ["segments", "campaigns", "channels", "callers", "donors", "donations", "calls"]
SCHEMA_FILE = pathlib.Path(__file__).resolve().parent.parent / "snowflake" / "schema.sql"


def neon_connect():
    return psycopg2.connect(
        host=os.environ["NEON_HOST"],
        dbname=os.environ.get("NEON_DB", "neondb"),
        user=os.environ["NEON_USER"],
        password=os.environ["NEON_PASSWORD"],
        sslmode="require",
    )


def snowflake_connect():
    return snowflake.connector.connect(
        account=os.environ["SNOWFLAKE_ACCOUNT"],
        user=os.environ["SNOWFLAKE_USER"],
        password=os.environ["SNOWFLAKE_PASSWORD"],
        role=os.environ.get("SNOWFLAKE_ROLE", "SYSADMIN"),
        warehouse=os.environ.get("SNOWFLAKE_WAREHOUSE", "COMPUTE_WH"),
        database=os.environ.get("SNOWFLAKE_DATABASE", "SYPHER_BI"),
        schema=os.environ.get("SNOWFLAKE_SCHEMA", "PUBLIC"),
    )


def create_schema(sf_cur):
    """Run snowflake/schema.sql, statement by statement."""
    sql = SCHEMA_FILE.read_text()
    statements = [s.strip() for s in sql.split(";") if s.strip() and not s.strip().startswith("--")]
    for stmt in statements:
        sf_cur.execute(stmt)
    print(f"  schema: ran {len(statements)} DDL statement(s)")


def copy_table(pg_cur, sf_cur, table):
    pg_cur.execute(f"SELECT * FROM {table}")
    rows = pg_cur.fetchall()
    cols = [d[0] for d in pg_cur.description]

    # Let Snowflake AUTOINCREMENT generate surrogate keys.
    if "id" in cols:
        idx = cols.index("id")
        cols = [c for c in cols if c != "id"]
        rows = [tuple(v for j, v in enumerate(r) if j != idx) for r in rows]

    sf_cur.execute(f"TRUNCATE TABLE IF EXISTS {table}")
    if rows:
        placeholders = ", ".join(["%s"] * len(cols))
        collist = ", ".join(cols)
        sf_cur.executemany(
            f"INSERT INTO {table} ({collist}) VALUES ({placeholders})", rows
        )
    print(f"  {table:<10} copied {len(rows)} row(s)")


def main():
    if not SCHEMA_FILE.exists():
        sys.exit(f"schema file not found: {SCHEMA_FILE}")

    print("Connecting to Neon…")
    pg = neon_connect()
    print("Connecting to Snowflake…")
    sf = snowflake_connect()
    try:
        pg_cur, sf_cur = pg.cursor(), sf.cursor()
        print("Creating Snowflake schema…")
        create_schema(sf_cur)
        print("Copying tables…")
        for table in TABLE_ORDER:
            copy_table(pg_cur, sf_cur, table)
        sf.commit()
        print("Done. Set Database:Provider=Snowflake to serve from Snowflake.")
    finally:
        pg.close()
        sf.close()


if __name__ == "__main__":
    main()
