#!/usr/bin/env python3
"""Read-only SQL Server architecture metadata extractor.

Extracts catalog evidence for a configured seed set and its dependency
boundary. Does not execute modules, does not modify database objects, and
does not write credentials to output artifacts.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from collections import deque
from datetime import date, datetime
from decimal import Decimal
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Set, Tuple
from zoneinfo import ZoneInfo

try:
    import pyodbc
except ImportError:  # pragma: no cover - optional until install
    pyodbc = None  # type: ignore


IST = ZoneInfo("Asia/Kolkata")
TOOL_NAME = "db_architecture_extract"
EXIT_OK = 0
EXIT_CONFIG = 1
EXIT_CONNECTION = 2
EXIT_EXTRACT = 3
EXIT_OUTPUT = 4

ENV_USERNAME_KEYS = ("DB_EXTRACT_USERNAME", "SQLCMDSERVER_USERNAME", "SQLCMDUSER")
ENV_PASSWORD_KEYS = ("DB_EXTRACT_PASSWORD", "SQLCMDPASSWORD")
ENV_SERVER_KEYS = ("DB_EXTRACT_SERVER", "SQLCMDSERVER")
ENV_DATABASE_KEYS = ("DB_EXTRACT_DATABASE", "SQLCMDDATABASE")

FORBIDDEN_STATEMENT_RE = re.compile(
    r"(?is)(?<![.\w])(?:"
    r"INSERT|UPDATE|DELETE|MERGE|ALTER|CREATE|DROP|TRUNCATE|"
    r"EXEC|EXECUTE|sp_executesql|SELECT\s+INTO|INTO\s+#|"
    r"OPENROWSET|OPENQUERY|OPENDATASOURCE|DBCC|BACKUP|RESTORE|"
    r"GRANT|REVOKE|DENY|xp_cmdshell"
    r")\b"
)
LEADING_SELECT_RE = re.compile(r"(?is)^\s*SELECT\b")
SQL_COMMENT_BLOCK_RE = re.compile(r"/\*.*?\*/", re.DOTALL)
SQL_COMMENT_LINE_RE = re.compile(r"--[^\n]*")
UNSAFE_FILENAME_RE = re.compile(r"[^\w.-]")

SYSTEM_SCHEMAS = {"sys", "INFORMATION_SCHEMA"}
INVENTORY_TYPES = {
    "USER_TABLE",
    "VIEW",
    "SQL_STORED_PROCEDURE",
    "SQL_SCALAR_FUNCTION",
    "SQL_INLINE_TABLE_VALUED_FUNCTION",
    "SQL_TABLE_VALUED_FUNCTION",
    "CLR_SCALAR_FUNCTION",
    "CLR_TABLE_VALUED_FUNCTION",
    "SQL_TRIGGER",
    "TYPE_TABLE",
    "SYNONYM",
}
SKIP_RECURSE_TYPES = {
    "PRIMARY_KEY_CONSTRAINT",
    "FOREIGN_KEY_CONSTRAINT",
    "DEFAULT_CONSTRAINT",
    "CHECK_CONSTRAINT",
    "UNIQUE_CONSTRAINT",
    "INTERNAL_TABLE",
    "SYSTEM_TABLE",
    "SERVICE_QUEUE",
}

# Bit/flag fields that must serialize as JSON booleans.
BOOL_FIELDS = {
    "is_ms_shipped",
    "is_nullable",
    "is_identity",
    "is_computed",
    "is_disabled",
    "is_not_trusted",
    "is_unique",
    "is_primary_key",
    "is_unique_constraint",
    "is_output",
    "has_default_value",
    "is_table_type",
    "is_schema_bound_reference",
    "is_caller_dependent",
    "is_ambiguous",
    "is_descending",
    "is_included",
    "is_instead_of_trigger",
    "is_system_named",
    "read_only",
}


# ---------------------------------------------------------------------------
# Catalog queries. Every statement is a single SELECT against documented
# catalog views. Identifiers such as create_date are not SQL CREATE.
# ---------------------------------------------------------------------------

Q_CURRENT_DATABASE = "SELECT DB_NAME() AS database_name"
Q_RESOLVE_OBJECT = """
SELECT
    o.object_id,
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name,
    RTRIM(o.type) AS type_code,
    o.type_desc AS [type],
    o.create_date,
    o.modify_date,
    CAST(o.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.objects o
WHERE o.name = ?
  AND SCHEMA_NAME(o.schema_id) = ?
"""
Q_RESOLVE_OBJECT_ANY_SCHEMA = """
SELECT
    o.object_id,
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name,
    RTRIM(o.type) AS type_code,
    o.type_desc AS [type],
    o.create_date,
    o.modify_date,
    CAST(o.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.objects o
WHERE o.name = ?
"""
Q_OBJECT_BY_IDS = """
SELECT
    o.object_id,
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name,
    RTRIM(o.type) AS type_code,
    o.type_desc AS [type],
    o.create_date,
    o.modify_date,
    CAST(o.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.objects o
WHERE o.object_id IN {ids}
"""
Q_SQL_DEPENDENCIES = """
SELECT
    d.referencing_id,
    OBJECT_SCHEMA_NAME(d.referencing_id) AS referencing_schema,
    OBJECT_NAME(d.referencing_id) AS referencing_object,
    ro.type_desc AS referencing_type,
    d.referenced_id,
    d.referenced_server_name,
    d.referenced_database_name,
    d.referenced_schema_name,
    d.referenced_entity_name,
    COALESCE(OBJECT_SCHEMA_NAME(d.referenced_id), d.referenced_schema_name) AS referenced_schema,
    COALESCE(OBJECT_NAME(d.referenced_id), d.referenced_entity_name) AS referenced_object,
    xo.type_desc AS referenced_type,
    CAST(d.is_schema_bound_reference AS bit) AS is_schema_bound_reference,
    CAST(d.is_caller_dependent AS bit) AS is_caller_dependent,
    CAST(d.is_ambiguous AS bit) AS is_ambiguous,
    d.referenced_class_desc,
    d.referencing_class_desc,
    CAST(ISNULL(ro.is_ms_shipped, 0) AS bit) AS referencing_is_ms_shipped,
    CAST(ISNULL(xo.is_ms_shipped, 0) AS bit) AS referenced_is_ms_shipped
FROM sys.sql_expression_dependencies d
LEFT JOIN sys.objects ro ON ro.object_id = d.referencing_id
LEFT JOIN sys.objects xo ON xo.object_id = d.referenced_id
WHERE d.referencing_id IN {ids}
   OR d.referenced_id IN {ids}
"""
Q_FK_PARTNERS = """
SELECT
    fk.object_id AS foreign_key_object_id,
    fk.name AS foreign_key,
    fk.parent_object_id,
    fk.referenced_object_id,
    SCHEMA_NAME(pt.schema_id) AS parent_schema,
    pt.name AS parent_table,
    SCHEMA_NAME(rt.schema_id) AS referenced_schema,
    rt.name AS referenced_table
FROM sys.foreign_keys fk
INNER JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
WHERE fk.parent_object_id IN {ids}
   OR fk.referenced_object_id IN {ids}
"""
Q_TRIGGERS_FOR_PARENTS = """
SELECT
    tr.object_id,
    SCHEMA_NAME(o.schema_id) AS [schema],
    tr.name,
    tr.parent_id,
    RTRIM(tr.type) AS type_code,
    tr.type_desc AS [type],
    CAST(tr.is_disabled AS bit) AS is_disabled,
    CAST(tr.is_instead_of_trigger AS bit) AS is_instead_of_trigger,
    tr.create_date,
    tr.modify_date
FROM sys.triggers tr
INNER JOIN sys.objects o ON o.object_id = tr.object_id
WHERE tr.parent_class = 1
  AND tr.parent_id IN {ids}
"""
Q_TVP_FOR_MODULES = """
SELECT
    tt.type_table_object_id AS object_id,
    SCHEMA_NAME(tt.schema_id) AS [schema],
    tt.name,
    N'TYPE_TABLE' AS [type],
    tt.user_type_id,
    CAST(tt.is_table_type AS bit) AS is_table_type,
    p.object_id AS from_module_id
FROM sys.parameters p
INNER JOIN sys.types t ON t.user_type_id = p.user_type_id
INNER JOIN sys.table_types tt ON tt.user_type_id = t.user_type_id
WHERE t.is_table_type = 1
  AND p.object_id IN {ids}
"""
Q_TABLES = """
SELECT
    SCHEMA_NAME(t.schema_id) AS [schema],
    t.name AS [table],
    t.object_id,
    t.create_date,
    t.modify_date,
    CAST(t.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.tables t
WHERE t.object_id IN {ids}
"""
Q_COLUMNS = """
SELECT
    SCHEMA_NAME(t.schema_id) AS [schema],
    t.name AS [table],
    c.column_id,
    c.name AS [column],
    ty.name AS data_type,
    c.max_length,
    c.[precision],
    c.scale,
    CAST(c.is_nullable AS bit) AS is_nullable,
    CAST(c.is_identity AS bit) AS is_identity,
    CAST(c.is_computed AS bit) AS is_computed,
    dc.definition AS default_definition,
    cc.definition AS computed_definition
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
LEFT JOIN sys.computed_columns cc
    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
WHERE t.object_id IN {ids}
"""
Q_FOREIGN_KEYS = """
SELECT
    fk.name AS foreign_key,
    SCHEMA_NAME(pt.schema_id) AS parent_schema,
    pt.name AS parent_table,
    pc.name AS parent_column,
    SCHEMA_NAME(rt.schema_id) AS referenced_schema,
    rt.name AS referenced_table,
    rc.name AS referenced_column,
    fk.delete_referential_action_desc AS delete_action,
    fk.update_referential_action_desc AS update_action,
    CAST(fk.is_disabled AS bit) AS is_disabled,
    CAST(fk.is_not_trusted AS bit) AS is_not_trusted,
    fkc.constraint_column_id AS ordinal
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
INNER JOIN sys.columns pc
    ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
INNER JOIN sys.columns rc
    ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id IN {ids}
   OR fk.referenced_object_id IN {ids}
"""
Q_INDEXES = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [table],
    i.name AS index_name,
    i.index_id,
    i.type_desc AS index_type,
    CAST(i.is_unique AS bit) AS is_unique,
    CAST(i.is_primary_key AS bit) AS is_primary_key,
    CAST(i.is_unique_constraint AS bit) AS is_unique_constraint,
    CAST(i.is_disabled AS bit) AS is_disabled,
    i.filter_definition,
    c.name AS [column],
    ic.key_ordinal,
    ic.index_column_id,
    CAST(ic.is_included_column AS bit) AS is_included_column,
    CAST(ic.is_descending_key AS bit) AS is_descending_key
FROM sys.indexes i
INNER JOIN sys.objects o ON o.object_id = i.object_id
INNER JOIN sys.index_columns ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id IN {ids}
"""
Q_INDEXES_HEAP = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [table],
    i.name AS index_name,
    i.index_id,
    i.type_desc AS index_type,
    CAST(i.is_unique AS bit) AS is_unique,
    CAST(i.is_primary_key AS bit) AS is_primary_key,
    CAST(i.is_unique_constraint AS bit) AS is_unique_constraint,
    CAST(i.is_disabled AS bit) AS is_disabled,
    i.filter_definition
FROM sys.indexes i
INNER JOIN sys.objects o ON o.object_id = i.object_id
WHERE i.object_id IN {ids}
  AND i.type = 0
"""
Q_KEY_CONSTRAINTS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [table],
    kc.name AS constraint_name,
    kc.type_desc AS constraint_type,
    c.name AS [column],
    ic.key_ordinal,
    CAST(kc.is_system_named AS bit) AS is_system_named
FROM sys.key_constraints kc
INNER JOIN sys.objects o ON o.object_id = kc.parent_object_id
INNER JOIN sys.indexes i
    ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
INNER JOIN sys.index_columns ic
    ON ic.object_id = i.object_id
   AND ic.index_id = i.index_id
   AND ic.is_included_column = 0
INNER JOIN sys.columns c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.parent_object_id IN {ids}
"""
Q_DEFAULT_CONSTRAINTS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [table],
    dc.name AS constraint_name,
    c.name AS [column],
    dc.definition,
    CAST(dc.is_system_named AS bit) AS is_system_named
FROM sys.default_constraints dc
INNER JOIN sys.objects o ON o.object_id = dc.parent_object_id
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id IN {ids}
"""
Q_CHECK_CONSTRAINTS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [table],
    cc.name AS constraint_name,
    cc.definition,
    CAST(cc.is_disabled AS bit) AS is_disabled,
    CAST(cc.is_not_trusted AS bit) AS is_not_trusted,
    CAST(cc.is_system_named AS bit) AS is_system_named
FROM sys.check_constraints cc
INNER JOIN sys.objects o ON o.object_id = cc.parent_object_id
WHERE cc.parent_object_id IN {ids}
"""
Q_PROCEDURES = """
SELECT
    SCHEMA_NAME(p.schema_id) AS [schema],
    p.name AS [procedure],
    p.object_id,
    p.create_date,
    p.modify_date,
    CAST(p.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.procedures p
WHERE p.object_id IN {ids}
"""
Q_PARAMETERS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [procedure],
    par.parameter_id,
    par.name AS parameter_name,
    ty.name AS data_type,
    par.max_length,
    par.[precision],
    par.scale,
    CAST(par.is_output AS bit) AS is_output,
    CAST(par.has_default_value AS bit) AS has_default_value,
    CAST(ty.is_table_type AS bit) AS is_table_type,
    SCHEMA_NAME(tt.schema_id) AS table_type_schema,
    tt.name AS table_type_name
FROM sys.parameters par
INNER JOIN sys.objects o ON o.object_id = par.object_id
INNER JOIN sys.types ty ON ty.user_type_id = par.user_type_id
LEFT JOIN sys.table_types tt ON tt.user_type_id = par.user_type_id
WHERE par.object_id IN {ids}
  AND par.parameter_id > 0
"""
Q_SQL_MODULES = """
SELECT
    m.object_id,
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name,
    o.type_desc AS [type],
    m.definition,
    CAST(m.uses_ansi_nulls AS bit) AS uses_ansi_nulls,
    CAST(m.uses_quoted_identifier AS bit) AS uses_quoted_identifier
FROM sys.sql_modules m
INNER JOIN sys.objects o ON o.object_id = m.object_id
WHERE m.object_id IN {ids}
"""
Q_TABLE_TYPES = """
SELECT
    SCHEMA_NAME(tt.schema_id) AS [schema],
    tt.name AS type_name,
    tt.user_type_id,
    tt.type_table_object_id AS object_id,
    CAST(tt.is_table_type AS bit) AS is_table_type
FROM sys.table_types tt
WHERE tt.type_table_object_id IN {ids}
"""
Q_TABLE_TYPE_COLUMNS = """
SELECT
    SCHEMA_NAME(tt.schema_id) AS [schema],
    tt.name AS type_name,
    c.column_id,
    c.name AS [column],
    ty.name AS data_type,
    c.max_length,
    c.[precision],
    c.scale,
    CAST(c.is_nullable AS bit) AS is_nullable,
    CAST(c.is_identity AS bit) AS is_identity,
    CAST(c.is_computed AS bit) AS is_computed
FROM sys.table_types tt
INNER JOIN sys.columns c ON c.object_id = tt.type_table_object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE tt.type_table_object_id IN {ids}
"""
Q_VIEWS = """
SELECT
    SCHEMA_NAME(v.schema_id) AS [schema],
    v.name AS [view],
    v.object_id,
    v.create_date,
    v.modify_date,
    CAST(v.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.views v
WHERE v.object_id IN {ids}
"""
Q_FUNCTIONS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    o.name AS [function],
    o.object_id,
    RTRIM(o.type) AS type_code,
    o.type_desc AS [type],
    o.create_date,
    o.modify_date,
    CAST(o.is_ms_shipped AS bit) AS is_ms_shipped
FROM sys.objects o
WHERE o.type IN ('FN', 'IF', 'TF', 'FS', 'FT')
  AND o.object_id IN {ids}
"""
Q_TRIGGERS = """
SELECT
    SCHEMA_NAME(o.schema_id) AS [schema],
    tr.name AS [trigger],
    tr.object_id,
    SCHEMA_NAME(po.schema_id) AS parent_schema,
    po.name AS parent_object,
    po.type_desc AS parent_type,
    CASE WHEN tr.is_instead_of_trigger = 1 THEN N'INSTEAD_OF' ELSE N'AFTER' END AS trigger_type,
    CAST(tr.is_disabled AS bit) AS is_disabled,
    CAST(tr.is_instead_of_trigger AS bit) AS is_instead_of_trigger,
    tr.create_date,
    tr.modify_date
FROM sys.triggers tr
INNER JOIN sys.objects o ON o.object_id = tr.object_id
LEFT JOIN sys.objects po ON po.object_id = tr.parent_id
WHERE tr.object_id IN {ids}
"""
Q_TRIGGER_EVENTS = """
SELECT
    te.object_id,
    te.type_desc AS event_type
FROM sys.trigger_events te
WHERE te.object_id IN {ids}
"""

NAMED_QUERIES: Dict[str, str] = {
    "current_database": Q_CURRENT_DATABASE,
    "resolve_object": Q_RESOLVE_OBJECT,
    "resolve_object_any_schema": Q_RESOLVE_OBJECT_ANY_SCHEMA,
    "object_by_ids": Q_OBJECT_BY_IDS,
    "sql_dependencies": Q_SQL_DEPENDENCIES,
    "fk_partners": Q_FK_PARTNERS,
    "triggers_for_parents": Q_TRIGGERS_FOR_PARENTS,
    "tvp_for_modules": Q_TVP_FOR_MODULES,
    "tables": Q_TABLES,
    "columns": Q_COLUMNS,
    "foreign_keys": Q_FOREIGN_KEYS,
    "indexes": Q_INDEXES,
    "indexes_heap": Q_INDEXES_HEAP,
    "key_constraints": Q_KEY_CONSTRAINTS,
    "default_constraints": Q_DEFAULT_CONSTRAINTS,
    "check_constraints": Q_CHECK_CONSTRAINTS,
    "procedures": Q_PROCEDURES,
    "parameters": Q_PARAMETERS,
    "sql_modules": Q_SQL_MODULES,
    "table_types": Q_TABLE_TYPES,
    "table_type_columns": Q_TABLE_TYPE_COLUMNS,
    "views": Q_VIEWS,
    "functions": Q_FUNCTIONS,
    "triggers": Q_TRIGGERS,
    "trigger_events": Q_TRIGGER_EVENTS,
}


# ---------------------------------------------------------------------------
# SQL safety
# ---------------------------------------------------------------------------

def strip_sql_comments(sql: str) -> str:
    sql = SQL_COMMENT_BLOCK_RE.sub(" ", sql)
    sql = SQL_COMMENT_LINE_RE.sub(" ", sql)
    return sql


def assert_readonly_sql(sql: str) -> str:
    """Reject any statement that is not a single SELECT against catalogs."""
    stripped = strip_sql_comments(sql).strip()
    if not stripped:
        raise RuntimeError("Refusing to execute an empty SQL statement.")
    if ";" in stripped.rstrip(";"):
        raise RuntimeError("Refusing to execute a multi-statement SQL batch.")
    body = stripped.rstrip(";").strip()
    if not LEADING_SELECT_RE.search(body):
        raise RuntimeError("Refusing to execute a non-SELECT SQL statement.")
    # Remove the leading SELECT so SELECT itself is not flagged.
    remainder = LEADING_SELECT_RE.sub(" ", body, count=1)
    if FORBIDDEN_STATEMENT_RE.search(remainder):
        raise RuntimeError("Refusing to execute SQL containing a forbidden keyword.")
    return sql


def sql_int_list(ids: Iterable[Any]) -> str:
    cleaned: List[str] = []
    seen: Set[int] = set()
    for raw in ids:
        value = int(raw)
        if value in seen:
            continue
        seen.add(value)
        cleaned.append(str(value))
    if not cleaned:
        raise ValueError("Cannot build IN list from empty id set.")
    return "(" + ",".join(cleaned) + ")"


def render_query(sql: str, ids: Optional[Iterable[Any]] = None) -> str:
    if ids is None:
        return sql
    return sql.format(ids=sql_int_list(ids))


def validate_named_queries() -> List[str]:
    errors: List[str] = []
    for name, sql in NAMED_QUERIES.items():
        probe = sql.replace("{ids}", "(1)")
        try:
            assert_readonly_sql(probe)
        except RuntimeError as exc:
            errors.append(f"{name}: {exc}")
    return errors


# ---------------------------------------------------------------------------
# Value / JSON helpers
# ---------------------------------------------------------------------------

def now_ist() -> datetime:
    return datetime.now(IST)


def iso_ist(value: Optional[datetime] = None) -> str:
    stamp = value or now_ist()
    if stamp.tzinfo is None:
        stamp = stamp.replace(tzinfo=IST)
    else:
        stamp = stamp.astimezone(IST)
    return stamp.isoformat(timespec="seconds")


def normalize_value(key: str, value: Any) -> Any:
    if value is None:
        return None
    if isinstance(value, bool):
        return value
    if key in BOOL_FIELDS:
        if isinstance(value, (bytes, bytearray)):
            return bool(value[0]) if value else False
        return bool(value)
    if isinstance(value, datetime):
        return value.isoformat(timespec="seconds")
    if isinstance(value, date):
        return value.isoformat()
    if isinstance(value, Decimal):
        if value == value.to_integral_value():
            return int(value)
        return str(value)
    if isinstance(value, bytes):
        try:
            return value.decode("utf-8")
        except UnicodeDecodeError:
            return value.hex()
    return value


def row_to_dict(columns: Sequence[str], row: Sequence[Any]) -> Dict[str, Any]:
    return {columns[i]: normalize_value(columns[i], row[i]) for i in range(len(columns))}


def fetch_dicts(cursor: Any) -> List[Dict[str, Any]]:
    if cursor.description is None:
        return []
    columns = [item[0] for item in cursor.description]
    return [row_to_dict(columns, row) for row in cursor.fetchall()]


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(payload, indent=2, ensure_ascii=False) + "\n"
    path.write_text(text, encoding="utf-8", newline="\n")


def definition_filename(schema: str, name: str) -> str:
    schema_part = UNSAFE_FILENAME_RE.sub("_", schema or "unknown")
    name_part = UNSAFE_FILENAME_RE.sub("_", name or "unknown")
    return f"{schema_part}.{name_part}.sql"


def write_definition(path: Path, definition: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        handle.write(definition)


def qualified_name(schema: Optional[str], name: Optional[str]) -> str:
    schema = schema or ""
    name = name or ""
    if schema and name:
        return f"{schema}.{name}"
    return name or schema or ""


def parse_object_name(raw: str, default_schema: str) -> Tuple[str, str]:
    text = (raw or "").strip()
    if not text:
        raise ValueError("Object name is empty.")
    if "." in text:
        schema, name = text.split(".", 1)
        return schema.strip("[]"), name.strip("[]")
    return default_schema, text.strip("[]")


def env_first(keys: Sequence[str]) -> str:
    for key in keys:
        value = os.environ.get(key)
        if value:
            return value
    return ""


def redact_settings(settings: Dict[str, Any]) -> Dict[str, Any]:
    secret_keys = {"password", "pwd", "connection_string", "conn_str"}
    redacted: Dict[str, Any] = {}
    for key, value in settings.items():
        if key.lower() in secret_keys:
            redacted[key] = "***"
        else:
            redacted[key] = value
    return redacted


def sort_records(rows: List[Dict[str, Any]], keys: Sequence[str]) -> List[Dict[str, Any]]:
    def sort_key(row: Dict[str, Any]) -> Tuple[Any, ...]:
        items = []
        for key in keys:
            value = row.get(key)
            if value is None:
                items.append("")
            else:
                items.append(value)
        return tuple(items)

    return sorted(rows, key=sort_key)


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

def default_config() -> Dict[str, Any]:
    return {
        "server": "",
        "database": "AnmolDristi",
        "schema": "dbo",
        "driver": "ODBC Driver 17 for SQL Server",
        "trusted_connection": True,
        "username": "",
        "password": "",
        "seed_objects": [],
        "seed_procedures": [],
        "include_procedure_definitions": True,
        "include_views": True,
        "include_functions": True,
        "include_triggers": True,
        "include_dependencies": True,
        "dependency_depth": 5,
        "output_directory": "output",
        "read_only": True,
    }


def load_config(path: Path) -> Dict[str, Any]:
    if not path.is_file():
        raise FileNotFoundError(f"Configuration file not found: {path}")
    try:
        loaded = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"Configuration file is not valid JSON: {path}: {exc}") from exc
    if not isinstance(loaded, dict):
        raise ValueError("Configuration root must be a JSON object.")
    config = default_config()
    config.update(loaded)
    return config


def apply_cli_overrides(config: Dict[str, Any], args: argparse.Namespace) -> Dict[str, Any]:
    if args.output:
        config["output_directory"] = args.output
    if args.dependency_depth is not None:
        config["dependency_depth"] = args.dependency_depth
    if args.no_definitions:
        config["include_procedure_definitions"] = False
        config["write_view_definitions"] = False
        config["write_function_definitions"] = False
        config["write_trigger_definitions"] = False
    config["tables_only"] = bool(args.tables_only)
    config["procedures_only"] = bool(args.procedures_only)
    if config["tables_only"] and config["procedures_only"]:
        raise ValueError("--tables-only and --procedures-only cannot be combined.")
    return config


def validate_config(config: Dict[str, Any]) -> None:
    if config.get("read_only") is not True:
        raise ValueError("Refusing to run: config.read_only must be true.")
    if not config.get("server") and not env_first(ENV_SERVER_KEYS):
        raise ValueError("SQL Server host is required (config.server or DB_EXTRACT_SERVER).")
    if not config.get("database"):
        raise ValueError("config.database is required.")
    if not config.get("schema"):
        raise ValueError("config.schema is required.")
    try:
        depth = int(config.get("dependency_depth", 5))
    except (TypeError, ValueError) as exc:
        raise ValueError("config.dependency_depth must be an integer.") from exc
    if depth < 0:
        raise ValueError("config.dependency_depth must be >= 0.")
    config["dependency_depth"] = depth
    if not isinstance(config.get("seed_objects"), list):
        raise ValueError("config.seed_objects must be a list.")
    if not isinstance(config.get("seed_procedures"), list):
        raise ValueError("config.seed_procedures must be a list.")
    trusted = bool(config.get("trusted_connection"))
    username = env_first(ENV_USERNAME_KEYS) or config.get("username") or ""
    password = env_first(ENV_PASSWORD_KEYS) or config.get("password") or ""
    if not trusted and not username:
        raise ValueError(
            "SQL authentication requires username "
            "(config.username or DB_EXTRACT_USERNAME)."
        )
    if not trusted and not password:
        raise ValueError(
            "SQL authentication requires password "
            "(config.password or DB_EXTRACT_PASSWORD). "
            "Do not commit the password."
        )
    config["_username"] = username
    config["_password"] = password
    config["_server"] = env_first(ENV_SERVER_KEYS) or config["server"]
    config["_database"] = env_first(ENV_DATABASE_KEYS) or config["database"]


def build_connection_string(config: Dict[str, Any]) -> str:
    driver = config.get("driver") or "ODBC Driver 17 for SQL Server"
    server = config["_server"]
    database = config["_database"]
    parts = [
        f"DRIVER={{{driver}}}",
        f"SERVER={server}",
        f"DATABASE={database}",
        "ApplicationIntent=ReadOnly",
    ]
    if config.get("trusted_connection"):
        parts.append("Trusted_Connection=yes")
    else:
        parts.append(f"UID={config['_username']}")
        parts.append(f"PWD={config['_password']}")
    return ";".join(parts) + ";"


# ---------------------------------------------------------------------------
# Extraction log
# ---------------------------------------------------------------------------

class ExtractionLog:
    def __init__(self, quiet: bool = False) -> None:
        self.started_at = now_ist()
        self.ended_at: Optional[datetime] = None
        self.entries: List[Dict[str, Any]] = []
        self.query_categories: List[str] = []
        self.skipped: List[Dict[str, Any]] = []
        self.unresolved: List[Dict[str, Any]] = []
        self.object_counts: Dict[str, int] = {}
        self.settings: Dict[str, Any] = {}
        self.database = ""
        self._seen_categories: Set[str] = set()
        self.quiet = quiet

    def record(
        self,
        level: str,
        category: str,
        message: str,
        object_name: Optional[str] = None,
    ) -> None:
        entry = {
            "level": level,
            "category": category,
            "message": message,
        }
        if object_name:
            entry["object"] = object_name
        self.entries.append(entry)
        if self.quiet:
            return
        stream = sys.stderr if level in {"warning", "error"} else sys.stdout
        prefix = level.upper()
        target = f" [{object_name}]" if object_name else ""
        print(f"{prefix}{target}: {message}", file=stream)

    def info(self, category: str, message: str, object_name: Optional[str] = None) -> None:
        self.record("info", category, message, object_name)

    def warning(self, category: str, message: str, object_name: Optional[str] = None) -> None:
        self.record("warning", category, message, object_name)

    def error(self, category: str, message: str, object_name: Optional[str] = None) -> None:
        self.record("error", category, message, object_name)

    def add_category(self, category: str) -> None:
        if category not in self._seen_categories:
            self._seen_categories.add(category)
            self.query_categories.append(category)

    def warnings(self) -> List[Dict[str, Any]]:
        return [entry for entry in self.entries if entry["level"] == "warning"]

    def errors(self) -> List[Dict[str, Any]]:
        return [entry for entry in self.entries if entry["level"] == "error"]

    def to_dict(self) -> Dict[str, Any]:
        ended = self.ended_at or now_ist()
        return {
            "tool": TOOL_NAME,
            "database": self.database,
            "started_at_ist": iso_ist(self.started_at),
            "ended_at_ist": iso_ist(ended),
            "settings": redact_settings(self.settings),
            "query_categories": list(self.query_categories),
            "object_counts": dict(self.object_counts),
            "warnings": self.warnings(),
            "errors": self.errors(),
            "objects_skipped": list(self.skipped),
            "unresolved_dependencies": list(self.unresolved),
            "entries": list(self.entries),
        }


# ---------------------------------------------------------------------------
# Database catalog access
# ---------------------------------------------------------------------------

class SqlCatalog:
    def __init__(self, connection: Any, log: ExtractionLog) -> None:
        self.connection = connection
        self.log = log

    def execute(self, category: str, sql: str, params: Optional[Sequence[Any]] = None) -> List[Dict[str, Any]]:
        rendered = sql
        assert_readonly_sql(rendered)
        self.log.add_category(category)
        cursor = self.connection.cursor()
        try:
            if params:
                cursor.execute(rendered, params)
            else:
                cursor.execute(rendered)
            return fetch_dicts(cursor)
        finally:
            cursor.close()

    def execute_ids(self, category: str, sql: str, ids: Sequence[int]) -> List[Dict[str, Any]]:
        if not ids:
            return []
        rows: List[Dict[str, Any]] = []
        chunk_size = 400
        unique_ids = sorted({int(item) for item in ids})
        for offset in range(0, len(unique_ids), chunk_size):
            chunk = unique_ids[offset : offset + chunk_size]
            rendered = render_query(sql, chunk)
            rows.extend(self.execute(category, rendered))
        return rows

    def current_database(self) -> str:
        rows = self.execute("session", Q_CURRENT_DATABASE)
        if not rows:
            raise RuntimeError("Could not read current database name.")
        return str(rows[0]["database_name"])

    def resolve_object(self, schema: str, name: str) -> List[Dict[str, Any]]:
        rows = self.execute("resolve_object", Q_RESOLVE_OBJECT, (name, schema))
        if rows:
            return rows
        return self.execute("resolve_object", Q_RESOLVE_OBJECT_ANY_SCHEMA, (name,))

    def objects_by_ids(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return self.execute_ids("objects", Q_OBJECT_BY_IDS, ids)

    def sql_dependencies(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return self.execute_ids("sql_expression_dependencies", Q_SQL_DEPENDENCIES, ids)

    def fk_partners(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return self.execute_ids("foreign_keys", Q_FK_PARTNERS, ids)

    def triggers_for_parents(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return self.execute_ids("triggers", Q_TRIGGERS_FOR_PARENTS, ids)

    def tvps_for_modules(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return self.execute_ids("table_types", Q_TVP_FOR_MODULES, ids)


# ---------------------------------------------------------------------------
# Discovery
# ---------------------------------------------------------------------------

class DiscoveredObject:
    __slots__ = (
        "object_id",
        "schema",
        "name",
        "type",
        "discovery_source",
        "discovered_from",
        "dependency_depth",
        "discovery_reason",
        "is_ms_shipped",
        "referenced_server",
        "referenced_database",
    )

    def __init__(self, **kwargs: Any) -> None:
        self.object_id: Optional[int] = kwargs.get("object_id")
        self.schema: Optional[str] = kwargs.get("schema")
        self.name: Optional[str] = kwargs.get("name")
        self.type: Optional[str] = kwargs.get("type")
        self.discovery_source: str = kwargs["discovery_source"]
        self.discovered_from: Optional[str] = kwargs.get("discovered_from")
        self.dependency_depth: int = int(kwargs.get("dependency_depth") or 0)
        self.discovery_reason: Optional[str] = kwargs.get("discovery_reason")
        self.is_ms_shipped: bool = bool(kwargs.get("is_ms_shipped") or False)
        self.referenced_server: Optional[str] = kwargs.get("referenced_server")
        self.referenced_database: Optional[str] = kwargs.get("referenced_database")

    def key(self) -> Tuple[Any, ...]:
        if self.object_id is not None:
            return ("id", int(self.object_id))
        return (
            "name",
            (self.referenced_server or "").lower(),
            (self.referenced_database or "").lower(),
            (self.schema or "").lower(),
            (self.name or "").lower(),
        )

    def qualified(self) -> str:
        return qualified_name(self.schema, self.name)

    def to_dict(self) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "object_id": self.object_id,
            "schema": self.schema,
            "name": self.name,
            "type": self.type,
            "discovery_source": self.discovery_source,
            "dependency_depth": self.dependency_depth,
        }
        if self.discovered_from:
            payload["discovered_from"] = self.discovered_from
        if self.discovery_reason:
            payload["discovery_reason"] = self.discovery_reason
        if self.referenced_server:
            payload["referenced_server"] = self.referenced_server
        if self.referenced_database:
            payload["referenced_database"] = self.referenced_database
        return payload


def classify_reference(
    row: Dict[str, Any],
    current_database: str,
) -> str:
    server = row.get("referenced_server_name")
    database = row.get("referenced_database_name")
    referenced_id = row.get("referenced_id")
    if server:
        return "external"
    if database and str(database).lower() != str(current_database).lower():
        return "external"
    if referenced_id is None:
        return "unresolved"
    schema = (row.get("referenced_schema") or "").lower()
    if schema in {item.lower() for item in SYSTEM_SCHEMAS}:
        return "external"
    return "dependency"


def should_inventory(type_desc: Optional[str], is_ms_shipped: bool) -> bool:
    if is_ms_shipped:
        return False
    if not type_desc:
        return True
    if type_desc in SKIP_RECURSE_TYPES:
        return False
    if type_desc in INVENTORY_TYPES:
        return True
    return False


class DiscoveryEngine:
    def __init__(
        self,
        catalog: SqlCatalog,
        config: Dict[str, Any],
        log: ExtractionLog,
        current_database: str,
    ) -> None:
        self.catalog = catalog
        self.config = config
        self.log = log
        self.current_database = current_database
        self.objects: Dict[Tuple[Any, ...], DiscoveredObject] = {}
        self.dependencies: List[Dict[str, Any]] = []
        self._seen_dep_keys: Set[Tuple[Any, ...]] = set()

    def add_object(self, discovered: DiscoveredObject) -> bool:
        key = discovered.key()
        existing = self.objects.get(key)
        if existing is None:
            self.objects[key] = discovered
            return True
        if discovered.dependency_depth < existing.dependency_depth:
            self.objects[key] = discovered
            return False
        return False

    def resolve_seed(self, raw_name: str, kind: str) -> None:
        schema, name = parse_object_name(raw_name, self.config["schema"])
        rows = self.catalog.resolve_object(schema, name)
        label = qualified_name(schema, name)
        if not rows:
            unresolved = DiscoveredObject(
                object_id=None,
                schema=schema,
                name=name,
                type=None,
                discovery_source="unresolved",
                discovered_from=None,
                dependency_depth=0,
                discovery_reason=f"seed_{kind}",
            )
            self.add_object(unresolved)
            self.log.warning(
                "seed",
                f"Seed {kind} could not be resolved from metadata.",
                label,
            )
            self.log.unresolved.append(
                {
                    "schema": schema,
                    "name": name,
                    "kind": kind,
                    "reason": "not_found",
                }
            )
            return
        preferred = [row for row in rows if (row.get("schema") or "").lower() == schema.lower()]
        chosen = preferred or rows
        if len(chosen) > 1:
            self.log.warning(
                "seed",
                f"Seed {kind} matched multiple objects; using configured schema matches.",
                label,
            )
        if preferred and len(rows) > len(preferred):
            extra = [
                qualified_name(row.get("schema"), row.get("name"))
                for row in rows
                if (row.get("schema") or "").lower() != schema.lower()
            ]
            self.log.warning(
                "seed",
                "Additional same-name objects exist outside the configured schema: "
                + ", ".join(extra),
                label,
            )
        for row in chosen:
            if row.get("is_ms_shipped"):
                self.log.warning("seed", "Seed resolved to an ms-shipped object; skipped.", label)
                continue
            self.add_object(
                DiscoveredObject(
                    object_id=int(row["object_id"]),
                    schema=row.get("schema"),
                    name=row.get("name"),
                    type=row.get("type"),
                    discovery_source="seed",
                    dependency_depth=0,
                    discovery_reason=f"seed_{kind}",
                    is_ms_shipped=bool(row.get("is_ms_shipped")),
                )
            )
            self.log.info("seed", f"Resolved seed {kind}.", qualified_name(row.get("schema"), row.get("name")))

    def add_dependency_record(self, row: Dict[str, Any]) -> None:
        record = {
            "referencing_schema": row.get("referencing_schema"),
            "referencing_object": row.get("referencing_object"),
            "referencing_type": row.get("referencing_type"),
            "referenced_schema": row.get("referenced_schema"),
            "referenced_object": row.get("referenced_object"),
            "referenced_type": row.get("referenced_type"),
            "referenced_server_name": row.get("referenced_server_name"),
            "referenced_database_name": row.get("referenced_database_name"),
            "is_schema_bound_reference": bool(row.get("is_schema_bound_reference")),
            "is_caller_dependent": bool(row.get("is_caller_dependent")),
            "is_ambiguous": bool(row.get("is_ambiguous")),
        }
        key = tuple((k, record[k]) for k in sorted(record.keys()))
        if key in self._seen_dep_keys:
            return
        self._seen_dep_keys.add(key)
        self.dependencies.append(record)

    def _enqueue_from_dep(
        self,
        row: Dict[str, Any],
        origin: DiscoveredObject,
        next_depth: int,
        queue: deque,
    ) -> None:
        source = classify_reference(row, self.current_database)
        referenced_id = row.get("referenced_id")
        schema = row.get("referenced_schema")
        name = row.get("referenced_object")
        type_desc = row.get("referenced_type")
        discovered = DiscoveredObject(
            object_id=int(referenced_id) if referenced_id is not None else None,
            schema=schema,
            name=name,
            type=type_desc,
            discovery_source=source if source in {"unresolved", "external"} else "dependency",
            discovered_from=origin.qualified(),
            dependency_depth=next_depth,
            discovery_reason="sql_expression_dependency",
            referenced_server=row.get("referenced_server_name"),
            referenced_database=row.get("referenced_database_name"),
        )
        if source == "unresolved":
            self.add_object(discovered)
            self.log.warning(
                "dependency",
                "Referenced object could not be resolved from metadata.",
                qualified_name(schema, name) or origin.qualified(),
            )
            self.log.unresolved.append(
                {
                    "from": origin.qualified(),
                    "referenced_schema": schema,
                    "referenced_object": name,
                    "referenced_database": row.get("referenced_database_name"),
                    "referenced_server": row.get("referenced_server_name"),
                    "is_ambiguous": bool(row.get("is_ambiguous")),
                    "is_caller_dependent": bool(row.get("is_caller_dependent")),
                }
            )
            return
        if source == "external":
            self.add_object(discovered)
            return
        if referenced_id is None:
            self.add_object(discovered)
            return
        discovered.is_ms_shipped = bool(row.get("referenced_is_ms_shipped"))
        if not should_inventory(discovered.type, discovered.is_ms_shipped):
            self.log.skipped.append(
                {
                    "object_id": discovered.object_id,
                    "schema": discovered.schema,
                    "name": discovered.name,
                    "type": discovered.type,
                    "reason": "not_inventory_type_or_ms_shipped",
                }
            )
            return
        is_new = self.add_object(discovered)
        if is_new:
            queue.append(discovered)

    def _enqueue_referencing(
        self,
        row: Dict[str, Any],
        origin: DiscoveredObject,
        next_depth: int,
        queue: deque,
    ) -> None:
        referencing_id = row.get("referencing_id")
        if referencing_id is None:
            return
        type_desc = row.get("referencing_type")
        is_ms_shipped = bool(row.get("referencing_is_ms_shipped"))
        if not should_inventory(type_desc, is_ms_shipped):
            return
        discovered = DiscoveredObject(
            object_id=int(referencing_id),
            schema=row.get("referencing_schema"),
            name=row.get("referencing_object"),
            type=type_desc,
            discovery_source="dependency",
            discovered_from=origin.qualified(),
            dependency_depth=next_depth,
            discovery_reason="referenced_by",
            is_ms_shipped=is_ms_shipped,
        )
        if self.add_object(discovered):
            queue.append(discovered)

    def _enqueue_object_row(
        self,
        row: Dict[str, Any],
        origin: DiscoveredObject,
        next_depth: int,
        reason: str,
        queue: deque,
        id_field: str = "object_id",
    ) -> None:
        object_id = row.get(id_field)
        if object_id is None:
            return
        type_desc = row.get("type")
        schema = row.get("schema")
        name = row.get("name")
        is_ms_shipped = bool(row.get("is_ms_shipped"))
        if type_desc is None or schema is None or name is None:
            details = self.catalog.objects_by_ids([int(object_id)])
            if details:
                item = details[0]
                type_desc = item.get("type") or type_desc
                schema = item.get("schema") or schema
                name = item.get("name") or name
                is_ms_shipped = bool(item.get("is_ms_shipped"))
        if not should_inventory(type_desc, is_ms_shipped):
            return
        discovered = DiscoveredObject(
            object_id=int(object_id),
            schema=schema,
            name=name,
            type=type_desc,
            discovery_source="dependency",
            discovered_from=origin.qualified(),
            dependency_depth=next_depth,
            discovery_reason=reason,
            is_ms_shipped=is_ms_shipped,
        )
        if self.add_object(discovered):
            queue.append(discovered)

    def expand(self, origin: DiscoveredObject, queue: deque) -> None:
        if origin.object_id is None:
            return
        max_depth = int(self.config["dependency_depth"])
        if origin.dependency_depth >= max_depth:
            return
        next_depth = origin.dependency_depth + 1
        origin_id = int(origin.object_id)

        for row in self.catalog.sql_dependencies([origin_id]):
            self.add_dependency_record(row)
            referencing_id = row.get("referencing_id")
            referenced_id = row.get("referenced_id")
            if referencing_id is not None and int(referencing_id) == origin_id:
                self._enqueue_from_dep(row, origin, next_depth, queue)
            if referenced_id is not None and int(referenced_id) == origin_id:
                self._enqueue_referencing(row, origin, next_depth, queue)
            if referenced_id is None and referencing_id is not None and int(referencing_id) == origin_id:
                # Unresolved outbound reference already handled by _enqueue_from_dep.
                pass

        if origin.type == "USER_TABLE":
            for row in self.catalog.fk_partners([origin_id]):
                parent_id = int(row["parent_object_id"])
                referenced_id = int(row["referenced_object_id"])
                other_id = referenced_id if parent_id == origin_id else parent_id
                other_schema = (
                    row.get("referenced_schema") if parent_id == origin_id else row.get("parent_schema")
                )
                other_name = (
                    row.get("referenced_table") if parent_id == origin_id else row.get("parent_table")
                )
                self._enqueue_object_row(
                    {
                        "object_id": other_id,
                        "schema": other_schema,
                        "name": other_name,
                        "type": "USER_TABLE",
                    },
                    origin,
                    next_depth,
                    "foreign_key",
                    queue,
                )

        if self.config.get("include_triggers") and origin.type in {"USER_TABLE", "VIEW"}:
            for row in self.catalog.triggers_for_parents([origin_id]):
                self._enqueue_object_row(row, origin, next_depth, "trigger", queue)

        if origin.type in {
            "SQL_STORED_PROCEDURE",
            "SQL_SCALAR_FUNCTION",
            "SQL_INLINE_TABLE_VALUED_FUNCTION",
            "SQL_TABLE_VALUED_FUNCTION",
        }:
            for row in self.catalog.tvps_for_modules([origin_id]):
                self._enqueue_object_row(row, origin, origin.dependency_depth, "table_type", queue)

    def attach_structural_relations(self) -> None:
        """Always attach triggers and TVPs for objects already in the boundary."""
        table_ids = [
            item.object_id
            for item in self.objects.values()
            if item.object_id is not None and item.type in {"USER_TABLE", "VIEW"}
        ]
        module_ids = [
            item.object_id
            for item in self.objects.values()
            if item.object_id is not None
            and item.type
            in {
                "SQL_STORED_PROCEDURE",
                "SQL_SCALAR_FUNCTION",
                "SQL_INLINE_TABLE_VALUED_FUNCTION",
                "SQL_TABLE_VALUED_FUNCTION",
            }
        ]
        dummy_queue: deque = deque()
        if self.config.get("include_triggers") and table_ids:
            by_id = {item.object_id: item for item in self.objects.values() if item.object_id is not None}
            for row in self.catalog.triggers_for_parents(table_ids):
                parent = by_id.get(row.get("parent_id"))
                if parent is None:
                    continue
                self._enqueue_object_row(row, parent, parent.dependency_depth, "trigger", dummy_queue)
        if module_ids:
            by_id = {item.object_id: item for item in self.objects.values() if item.object_id is not None}
            for row in self.catalog.tvps_for_modules(module_ids):
                parent = by_id.get(row.get("from_module_id"))
                if parent is None:
                    continue
                self._enqueue_object_row(row, parent, parent.dependency_depth, "table_type", dummy_queue)
        table_ids = [
            item.object_id
            for item in self.objects.values()
            if item.object_id is not None and item.type == "USER_TABLE"
        ]
        if table_ids:
            by_id = {item.object_id: item for item in self.objects.values() if item.object_id is not None}
            for row in self.catalog.fk_partners(table_ids):
                for side_id, schema, name in (
                    (row.get("parent_object_id"), row.get("parent_schema"), row.get("parent_table")),
                    (
                        row.get("referenced_object_id"),
                        row.get("referenced_schema"),
                        row.get("referenced_table"),
                    ),
                ):
                    if side_id is None or int(side_id) in by_id:
                        continue
                    # One extra FK hop for tables already inside the boundary.
                    origin_id = (
                        row.get("referenced_object_id")
                        if int(side_id) == int(row.get("parent_object_id") or 0)
                        else row.get("parent_object_id")
                    )
                    origin = by_id.get(origin_id) if origin_id is not None else None
                    if origin is None:
                        continue
                    next_depth = origin.dependency_depth + 1
                    if next_depth > int(self.config["dependency_depth"]) + 1:
                        continue
                    self._enqueue_object_row(
                        {"object_id": side_id, "schema": schema, "name": name, "type": "USER_TABLE"},
                        origin,
                        next_depth,
                        "foreign_key",
                        dummy_queue,
                    )

    def run(self) -> None:
        for name in self.config.get("seed_objects") or []:
            self.resolve_seed(str(name), "object")
        for name in self.config.get("seed_procedures") or []:
            self.resolve_seed(str(name), "procedure")

        queue: deque = deque(
            item for item in self.objects.values() if item.discovery_source == "seed" and item.object_id is not None
        )
        visited: Set[int] = set()
        while queue:
            current = queue.popleft()
            if current.object_id is None:
                continue
            object_id = int(current.object_id)
            if object_id in visited:
                continue
            visited.add(object_id)
            self.expand(current, queue)

        self.attach_structural_relations()

        # Capture remaining dependency rows for every inventoried module so
        # dependencies.json is complete for the boundary, including unresolved.
        module_ids = [
            item.object_id
            for item in self.objects.values()
            if item.object_id is not None
        ]
        for row in self.catalog.sql_dependencies(module_ids):
            self.add_dependency_record(row)
            if row.get("referenced_id") is None and row.get("referencing_id") is not None:
                source = classify_reference(row, self.current_database)
                if source in {"unresolved", "external"}:
                    discovered = DiscoveredObject(
                        object_id=None,
                        schema=row.get("referenced_schema"),
                        name=row.get("referenced_object"),
                        type=row.get("referenced_type"),
                        discovery_source=source,
                        discovered_from=qualified_name(
                            row.get("referencing_schema"), row.get("referencing_object")
                        ),
                        dependency_depth=int(self.config["dependency_depth"]),
                        discovery_reason="sql_expression_dependency",
                        referenced_server=row.get("referenced_server_name"),
                        referenced_database=row.get("referenced_database_name"),
                    )
                    self.add_object(discovered)
                    if source == "unresolved":
                        self.log.unresolved.append(
                            {
                                "from": discovered.discovered_from,
                                "referenced_schema": discovered.schema,
                                "referenced_object": discovered.name,
                                "referenced_database": discovered.referenced_database,
                                "referenced_server": discovered.referenced_server,
                                "is_ambiguous": bool(row.get("is_ambiguous")),
                                "is_caller_dependent": bool(row.get("is_caller_dependent")),
                            }
                        )


# ---------------------------------------------------------------------------
# Extraction stages
# ---------------------------------------------------------------------------

def ids_of_type(objects: Sequence[DiscoveredObject], *types: str) -> List[int]:
    wanted = set(types)
    return sorted(
        {
            int(item.object_id)
            for item in objects
            if item.object_id is not None and item.type in wanted
        }
    )


def build_indexes(rows: List[Dict[str, Any]], heap_rows: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    grouped: Dict[Tuple[Any, ...], Dict[str, Any]] = {}
    for row in rows:
        key = (row.get("schema"), row.get("table"), row.get("index_id"), row.get("index_name"))
        item = grouped.get(key)
        if item is None:
            item = {
                "schema": row.get("schema"),
                "table": row.get("table"),
                "index_name": row.get("index_name"),
                "index_type": row.get("index_type"),
                "is_unique": bool(row.get("is_unique")),
                "is_primary_key": bool(row.get("is_primary_key")),
                "is_unique_constraint": bool(row.get("is_unique_constraint")),
                "is_disabled": bool(row.get("is_disabled")),
                "filter_definition": row.get("filter_definition"),
                "key_columns": [],
                "included_columns": [],
            }
            grouped[key] = item
        column_entry = {
            "column": row.get("column"),
            "ordinal": row.get("key_ordinal") if not row.get("is_included_column") else row.get("index_column_id"),
            "is_descending": bool(row.get("is_descending_key")),
        }
        if row.get("is_included_column"):
            item["included_columns"].append(column_entry)
        else:
            item["key_columns"].append(column_entry)
    for row in heap_rows:
        key = (row.get("schema"), row.get("table"), row.get("index_id"), row.get("index_name"))
        grouped.setdefault(
            key,
            {
                "schema": row.get("schema"),
                "table": row.get("table"),
                "index_name": row.get("index_name"),
                "index_type": row.get("index_type"),
                "is_unique": bool(row.get("is_unique")),
                "is_primary_key": bool(row.get("is_primary_key")),
                "is_unique_constraint": bool(row.get("is_unique_constraint")),
                "is_disabled": bool(row.get("is_disabled")),
                "filter_definition": row.get("filter_definition"),
                "key_columns": [],
                "included_columns": [],
            },
        )
    indexes = list(grouped.values())
    for item in indexes:
        item["key_columns"] = sorted(
            item["key_columns"], key=lambda col: (col.get("ordinal") is None, col.get("ordinal") or 0, col.get("column") or "")
        )
        item["included_columns"] = sorted(
            item["included_columns"],
            key=lambda col: (col.get("ordinal") is None, col.get("ordinal") or 0, col.get("column") or ""),
        )
    return sort_records(indexes, ("schema", "table", "index_name", "index_type"))


def build_constraints(
    key_rows: List[Dict[str, Any]],
    default_rows: List[Dict[str, Any]],
    check_rows: List[Dict[str, Any]],
) -> List[Dict[str, Any]]:
    grouped: Dict[Tuple[Any, ...], Dict[str, Any]] = {}
    for row in key_rows:
        key = (row.get("schema"), row.get("table"), row.get("constraint_name"))
        item = grouped.get(key)
        if item is None:
            constraint_type = row.get("constraint_type") or ""
            if constraint_type == "PRIMARY_KEY_CONSTRAINT":
                constraint_type = "PRIMARY_KEY"
            elif constraint_type == "UNIQUE_CONSTRAINT":
                constraint_type = "UNIQUE"
            item = {
                "schema": row.get("schema"),
                "table": row.get("table"),
                "constraint_name": row.get("constraint_name"),
                "constraint_type": constraint_type,
                "definition": None,
                "is_disabled": False,
                "is_not_trusted": False,
                "is_system_named": bool(row.get("is_system_named")),
                "columns": [],
            }
            grouped[key] = item
        item["columns"].append(
            {"column": row.get("column"), "ordinal": row.get("key_ordinal")}
        )
    constraints = list(grouped.values())
    for item in constraints:
        item["columns"] = [
            col["column"]
            for col in sorted(item["columns"], key=lambda col: col.get("ordinal") or 0)
        ]
    for row in default_rows:
        constraints.append(
            {
                "schema": row.get("schema"),
                "table": row.get("table"),
                "constraint_name": row.get("constraint_name"),
                "constraint_type": "DEFAULT",
                "column": row.get("column"),
                "definition": row.get("definition"),
                "is_disabled": False,
                "is_not_trusted": False,
                "is_system_named": bool(row.get("is_system_named")),
            }
        )
    for row in check_rows:
        constraints.append(
            {
                "schema": row.get("schema"),
                "table": row.get("table"),
                "constraint_name": row.get("constraint_name"),
                "constraint_type": "CHECK",
                "definition": row.get("definition"),
                "is_disabled": bool(row.get("is_disabled")),
                "is_not_trusted": bool(row.get("is_not_trusted")),
                "is_system_named": bool(row.get("is_system_named")),
            }
        )
    return sort_records(constraints, ("schema", "table", "constraint_type", "constraint_name"))


def write_module_definitions(
    output_dir: Path,
    modules: List[Dict[str, Any]],
    wanted_ids: Set[int],
    log: ExtractionLog,
    category: str,
) -> int:
    written = 0
    output_dir.mkdir(parents=True, exist_ok=True)
    for module in modules:
        object_id = module.get("object_id")
        if object_id is None or int(object_id) not in wanted_ids:
            continue
        schema = module.get("schema") or "unknown"
        name = module.get("name") or "unknown"
        definition = module.get("definition")
        label = qualified_name(schema, name)
        if definition is None:
            log.warning(
                category,
                "Module definition is NULL (encrypted or unavailable). File not written.",
                label,
            )
            continue
        write_definition(output_dir / definition_filename(schema, name), definition)
        written += 1
    return written


class Extractor:
    def __init__(self, catalog: SqlCatalog, config: Dict[str, Any], log: ExtractionLog, output_dir: Path) -> None:
        self.catalog = catalog
        self.config = config
        self.log = log
        self.output_dir = output_dir
        self.artifacts: Dict[str, Any] = {}
        self.critical_failure = False

    def stage(self, name: str, func: Any) -> Any:
        try:
            return func()
        except Exception as exc:
            self.log.error(name, f"{name} failed: {type(exc).__name__}: {exc}")
            self.critical_failure = True
            return None

    def emit(self, filename: str, payload: Any) -> None:
        write_json(self.output_dir / filename, payload)
        self.artifacts[filename] = payload

    def run(self, discovery: DiscoveryEngine) -> int:
        objects = list(discovery.objects.values())
        discovered_payload = sort_records(
            [item.to_dict() for item in objects],
            ("dependency_depth", "schema", "name", "discovery_source"),
        )
        self.emit("discovered_objects.json", discovered_payload)

        table_ids = ids_of_type(objects, "USER_TABLE")
        procedure_ids = ids_of_type(objects, "SQL_STORED_PROCEDURE")
        view_ids = ids_of_type(objects, "VIEW")
        function_ids = ids_of_type(
            objects,
            "SQL_SCALAR_FUNCTION",
            "SQL_INLINE_TABLE_VALUED_FUNCTION",
            "SQL_TABLE_VALUED_FUNCTION",
            "CLR_SCALAR_FUNCTION",
            "CLR_TABLE_VALUED_FUNCTION",
        )
        trigger_ids = ids_of_type(objects, "SQL_TRIGGER")
        table_type_ids = ids_of_type(objects, "TYPE_TABLE")

        tables_only = bool(self.config.get("tables_only"))
        procedures_only = bool(self.config.get("procedures_only"))
        write_tables = not procedures_only
        write_procedures = not tables_only
        write_views = bool(self.config.get("include_views")) and not tables_only and not procedures_only
        write_functions = bool(self.config.get("include_functions")) and not tables_only and not procedures_only
        write_triggers = bool(self.config.get("include_triggers")) and not procedures_only
        write_dependencies = bool(self.config.get("include_dependencies")) and not tables_only
        write_proc_defs = bool(self.config.get("include_procedure_definitions")) and write_procedures
        write_other_defs = not bool(self.config.get("no_definitions")) and self.config.get("write_view_definitions", True)

        counts = {
            "tables": 0,
            "columns": 0,
            "foreign_keys": 0,
            "indexes": 0,
            "constraints": 0,
            "procedures": 0,
            "procedure_parameters": 0,
            "user_defined_table_types": 0,
            "table_type_columns": 0,
            "views": 0,
            "functions": 0,
            "triggers": 0,
            "dependencies": 0,
            "discovered_objects": len(discovered_payload),
        }

        if write_tables:
            tables = self.stage("tables", lambda: sort_records(self.catalog.execute_ids("tables", Q_TABLES, table_ids), ("schema", "table")))
            columns = self.stage("columns", lambda: sort_records(self.catalog.execute_ids("columns", Q_COLUMNS, table_ids), ("schema", "table", "column_id")))
            foreign_keys = self.stage(
                "foreign_keys",
                lambda: sort_records(
                    self.catalog.execute_ids("foreign_keys", Q_FOREIGN_KEYS, table_ids),
                    ("foreign_key", "parent_schema", "parent_table", "ordinal", "parent_column"),
                ),
            )
            index_rows = self.stage("indexes", lambda: self.catalog.execute_ids("indexes", Q_INDEXES, table_ids + table_type_ids))
            heap_rows = self.stage("indexes", lambda: self.catalog.execute_ids("indexes", Q_INDEXES_HEAP, table_ids + table_type_ids))
            key_rows = self.stage("constraints", lambda: self.catalog.execute_ids("constraints", Q_KEY_CONSTRAINTS, table_ids + table_type_ids))
            default_rows = self.stage("constraints", lambda: self.catalog.execute_ids("constraints", Q_DEFAULT_CONSTRAINTS, table_ids + table_type_ids))
            check_rows = self.stage("constraints", lambda: self.catalog.execute_ids("constraints", Q_CHECK_CONSTRAINTS, table_ids + table_type_ids))
            if tables is not None:
                self.emit("tables.json", tables)
                counts["tables"] = len(tables)
            if columns is not None:
                cleaned_columns = []
                for row in columns:
                    cleaned_columns.append(
                        {
                            "schema": row.get("schema"),
                            "table": row.get("table"),
                            "column_id": row.get("column_id"),
                            "column": row.get("column"),
                            "data_type": row.get("data_type"),
                            "max_length": row.get("max_length"),
                            "precision": row.get("precision"),
                            "scale": row.get("scale"),
                            "is_nullable": bool(row.get("is_nullable")),
                            "is_identity": bool(row.get("is_identity")),
                            "is_computed": bool(row.get("is_computed")),
                            "default_definition": row.get("default_definition"),
                            "computed_definition": row.get("computed_definition"),
                        }
                    )
                self.emit("columns.json", cleaned_columns)
                counts["columns"] = len(cleaned_columns)
            if foreign_keys is not None:
                cleaned_fks = []
                for row in foreign_keys:
                    cleaned_fks.append(
                        {
                            "foreign_key": row.get("foreign_key"),
                            "parent_schema": row.get("parent_schema"),
                            "parent_table": row.get("parent_table"),
                            "parent_column": row.get("parent_column"),
                            "referenced_schema": row.get("referenced_schema"),
                            "referenced_table": row.get("referenced_table"),
                            "referenced_column": row.get("referenced_column"),
                            "delete_action": row.get("delete_action"),
                            "update_action": row.get("update_action"),
                            "is_disabled": bool(row.get("is_disabled")),
                            "is_not_trusted": bool(row.get("is_not_trusted")),
                        }
                    )
                self.emit("foreign_keys.json", cleaned_fks)
                counts["foreign_keys"] = len(cleaned_fks)
            if index_rows is not None and heap_rows is not None:
                indexes = build_indexes(index_rows, heap_rows)
                self.emit("indexes.json", indexes)
                counts["indexes"] = len(indexes)
            if key_rows is not None and default_rows is not None and check_rows is not None:
                constraints = build_constraints(key_rows, default_rows, check_rows)
                self.emit("constraints.json", constraints)
                counts["constraints"] = len(constraints)

        if write_procedures:
            procedures = self.stage(
                "procedures",
                lambda: sort_records(self.catalog.execute_ids("procedures", Q_PROCEDURES, procedure_ids), ("schema", "procedure")),
            )
            parameters = self.stage(
                "procedure_parameters",
                lambda: sort_records(
                    self.catalog.execute_ids("procedure_parameters", Q_PARAMETERS, procedure_ids),
                    ("schema", "procedure", "parameter_id"),
                ),
            )
            if procedures is not None:
                self.emit("procedures.json", procedures)
                counts["procedures"] = len(procedures)
            if parameters is not None:
                cleaned_params = []
                for row in parameters:
                    cleaned_params.append(
                        {
                            "schema": row.get("schema"),
                            "procedure": row.get("procedure"),
                            "parameter_id": row.get("parameter_id"),
                            "parameter_name": row.get("parameter_name"),
                            "data_type": row.get("data_type"),
                            "max_length": row.get("max_length"),
                            "precision": row.get("precision"),
                            "scale": row.get("scale"),
                            "is_output": bool(row.get("is_output")),
                            "has_default_value": bool(row.get("has_default_value")),
                            "is_table_type": bool(row.get("is_table_type")),
                            "table_type_schema": row.get("table_type_schema"),
                            "table_type_name": row.get("table_type_name"),
                        }
                    )
                self.emit("procedure_parameters.json", cleaned_params)
                counts["procedure_parameters"] = len(cleaned_params)

        table_types = self.stage(
            "user_defined_table_types",
            lambda: sort_records(self.catalog.execute_ids("table_types", Q_TABLE_TYPES, table_type_ids), ("schema", "type_name")),
        )
        table_type_columns = self.stage(
            "table_type_columns",
            lambda: sort_records(
                self.catalog.execute_ids("table_type_columns", Q_TABLE_TYPE_COLUMNS, table_type_ids),
                ("schema", "type_name", "column_id"),
            ),
        )
        if table_types is not None and not tables_only:
            self.emit("user_defined_table_types.json", table_types)
            counts["user_defined_table_types"] = len(table_types)
        if table_type_columns is not None and not tables_only:
            self.emit("table_type_columns.json", table_type_columns)
            counts["table_type_columns"] = len(table_type_columns)

        modules = None
        definition_ids = set(procedure_ids + view_ids + function_ids + trigger_ids)
        if definition_ids:
            modules = self.stage("sql_modules", lambda: self.catalog.execute_ids("sql_modules", Q_SQL_MODULES, list(definition_ids)))

        if write_proc_defs and modules is not None:
            written = write_module_definitions(
                self.output_dir / "procedure_definitions",
                modules,
                set(procedure_ids),
                self.log,
                "procedure_definitions",
            )
            self.log.info("procedure_definitions", f"Wrote {written} procedure definition file(s).")

        if write_views:
            views = self.stage(
                "views",
                lambda: sort_records(self.catalog.execute_ids("views", Q_VIEWS, view_ids), ("schema", "view")),
            )
            if views is not None:
                self.emit("views.json", views)
                counts["views"] = len(views)
            if write_other_defs and modules is not None:
                written = write_module_definitions(
                    self.output_dir / "view_definitions",
                    modules,
                    set(view_ids),
                    self.log,
                    "view_definitions",
                )
                self.log.info("view_definitions", f"Wrote {written} view definition file(s).")

        if write_functions:
            functions = self.stage(
                "functions",
                lambda: sort_records(self.catalog.execute_ids("functions", Q_FUNCTIONS, function_ids), ("schema", "function")),
            )
            if functions is not None:
                self.emit("functions.json", functions)
                counts["functions"] = len(functions)
            if write_other_defs and modules is not None:
                written = write_module_definitions(
                    self.output_dir / "function_definitions",
                    modules,
                    set(function_ids),
                    self.log,
                    "function_definitions",
                )
                self.log.info("function_definitions", f"Wrote {written} function definition file(s).")

        if write_triggers:
            triggers = self.stage(
                "triggers",
                lambda: sort_records(self.catalog.execute_ids("triggers", Q_TRIGGERS, trigger_ids), ("schema", "trigger")),
            )
            events = self.stage(
                "trigger_events",
                lambda: self.catalog.execute_ids("trigger_events", Q_TRIGGER_EVENTS, trigger_ids),
            )
            if triggers is not None:
                events_by_id: Dict[int, List[str]] = {}
                if events:
                    for row in events:
                        events_by_id.setdefault(int(row["object_id"]), []).append(row.get("event_type") or "")
                cleaned_triggers = []
                for row in triggers:
                    object_id = int(row["object_id"]) if row.get("object_id") is not None else None
                    cleaned_triggers.append(
                        {
                            "schema": row.get("schema"),
                            "trigger": row.get("trigger"),
                            "object_id": row.get("object_id"),
                            "parent_schema": row.get("parent_schema"),
                            "parent_object": row.get("parent_object"),
                            "parent_type": row.get("parent_type"),
                            "trigger_type": row.get("trigger_type"),
                            "events": sorted(events_by_id.get(object_id, [])) if object_id is not None else [],
                            "is_disabled": bool(row.get("is_disabled")),
                            "create_date": row.get("create_date"),
                            "modify_date": row.get("modify_date"),
                        }
                    )
                self.emit("triggers.json", cleaned_triggers)
                counts["triggers"] = len(cleaned_triggers)
            if write_other_defs and modules is not None:
                written = write_module_definitions(
                    self.output_dir / "trigger_definitions",
                    modules,
                    set(trigger_ids),
                    self.log,
                    "trigger_definitions",
                )
                self.log.info("trigger_definitions", f"Wrote {written} trigger definition file(s).")

        if write_dependencies:
            deps = sort_records(
                discovery.dependencies,
                (
                    "referencing_schema",
                    "referencing_object",
                    "referenced_schema",
                    "referenced_object",
                    "referenced_database_name",
                ),
            )
            self.emit("dependencies.json", deps)
            counts["dependencies"] = len(deps)

        self.log.object_counts = counts
        return counts["discovered_objects"]


# ---------------------------------------------------------------------------
# CLI and self-check
# ---------------------------------------------------------------------------

def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Extract read-only SQL Server architecture metadata from catalog views."
    )
    parser.add_argument("--config", required=False, help="Path to config.json")
    parser.add_argument("--output", help="Override output directory")
    parser.add_argument("--dependency-depth", type=int, help="Override dependency_depth")
    parser.add_argument("--no-definitions", action="store_true", help="Skip writing .sql module definitions")
    parser.add_argument("--tables-only", action="store_true", help="Write table-related artifacts only")
    parser.add_argument("--procedures-only", action="store_true", help="Write procedure-related artifacts only")
    parser.add_argument(
        "--self-check",
        action="store_true",
        help="Validate SQL safety, example config, and discovery cycle handling without connecting",
    )
    return parser


def connect(config: Dict[str, Any], log: ExtractionLog) -> Any:
    if pyodbc is None:
        raise RuntimeError("pyodbc is not installed. Run: pip install -r requirements.txt")
    conn_str = build_connection_string(config)
    log.info(
        "connection",
        "Connecting with ApplicationIntent=ReadOnly "
        f"(auth={'trusted' if config.get('trusted_connection') else 'sql'}, "
        f"server={config['_server']}, database={config['_database']}).",
    )
    try:
        connection = pyodbc.connect(conn_str, timeout=30, autocommit=True)
    except pyodbc.Error as exc:
        raise ConnectionError(f"Database connection failed: {exc}") from exc
    return connection


def build_manifest(config: Dict[str, Any], log: ExtractionLog) -> Dict[str, Any]:
    counts = log.object_counts
    return {
        "tool": TOOL_NAME,
        "database": log.database or config.get("_database") or config.get("database"),
        "schema": config.get("schema"),
        "extracted_at_ist": iso_ist(log.ended_at or now_ist()),
        "read_only": True,
        "dependency_depth": int(config.get("dependency_depth", 5)),
        "seed_objects": list(config.get("seed_objects") or []),
        "seed_procedures": list(config.get("seed_procedures") or []),
        "object_counts": {
            "tables": counts.get("tables", 0),
            "columns": counts.get("columns", 0),
            "procedures": counts.get("procedures", 0),
            "views": counts.get("views", 0),
            "functions": counts.get("functions", 0),
            "triggers": counts.get("triggers", 0),
            "dependencies": counts.get("dependencies", 0),
        },
        "warnings": log.warnings(),
    }


def resolve_output_dir(config: Dict[str, Any], config_path: Optional[Path]) -> Path:
    raw = config.get("output_directory") or "output"
    path = Path(raw)
    if not path.is_absolute():
        base = config_path.parent if config_path is not None else Path.cwd()
        path = base / path
    return path


def run_extraction(config: Dict[str, Any], config_path: Optional[Path]) -> int:
    log = ExtractionLog()
    log.settings = {
        "server": config.get("_server"),
        "database": config.get("_database"),
        "schema": config.get("schema"),
        "driver": config.get("driver"),
        "trusted_connection": bool(config.get("trusted_connection")),
        "username": config.get("_username") or "",
        "dependency_depth": int(config.get("dependency_depth", 5)),
        "include_procedure_definitions": bool(config.get("include_procedure_definitions")),
        "include_views": bool(config.get("include_views")),
        "include_functions": bool(config.get("include_functions")),
        "include_triggers": bool(config.get("include_triggers")),
        "include_dependencies": bool(config.get("include_dependencies")),
        "tables_only": bool(config.get("tables_only")),
        "procedures_only": bool(config.get("procedures_only")),
        "seed_objects": list(config.get("seed_objects") or []),
        "seed_procedures": list(config.get("seed_procedures") or []),
        "read_only": True,
    }
    output_dir = resolve_output_dir(config, config_path)
    output_dir.mkdir(parents=True, exist_ok=True)

    query_errors = validate_named_queries()
    if query_errors:
        for item in query_errors:
            log.error("sql_safety", item)
        log.ended_at = now_ist()
        write_json(output_dir / "extraction_log.json", log.to_dict())
        return EXIT_CONFIG

    connection = None
    exit_code = EXIT_OK
    try:
        try:
            connection = connect(config, log)
        except ConnectionError as exc:
            log.error("connection", str(exc))
            exit_code = EXIT_CONNECTION
        except RuntimeError as exc:
            log.error("connection", str(exc))
            exit_code = EXIT_CONNECTION
        else:
            catalog = SqlCatalog(connection, log)
            try:
                current_database = catalog.current_database()
            except Exception as exc:
                log.error("connection", f"Failed to read current database: {exc}")
                exit_code = EXIT_CONNECTION
            else:
                log.database = current_database
                expected = str(config["_database"])
                if current_database.lower() != expected.lower():
                    log.error(
                        "connection",
                        f"Connected database '{current_database}' does not match configured '{expected}'.",
                    )
                    exit_code = EXIT_CONNECTION
                else:
                    log.info("connection", f"Connected to database {current_database}.")
                    discovery = DiscoveryEngine(catalog, config, log, current_database)
                    try:
                        discovery.run()
                    except Exception as exc:
                        log.error("discovery", f"Dependency discovery failed: {type(exc).__name__}: {exc}")
                        exit_code = EXIT_EXTRACT
                    else:
                        extractor = Extractor(catalog, config, log, output_dir)
                        try:
                            extractor.run(discovery)
                        except Exception as exc:
                            log.error("output", f"Failed while writing artifacts: {type(exc).__name__}: {exc}")
                            exit_code = EXIT_OUTPUT
                        else:
                            if extractor.critical_failure:
                                exit_code = EXIT_EXTRACT
    finally:
        if connection is not None:
            try:
                connection.close()
            except Exception:
                pass
        log.ended_at = now_ist()
        try:
            write_json(output_dir / "extraction_log.json", log.to_dict())
            write_json(output_dir / "manifest.json", build_manifest(config, log))
        except Exception as exc:
            print(f"ERROR: Failed to write log/manifest: {exc}", file=sys.stderr)
            if exit_code == EXIT_OK:
                exit_code = EXIT_OUTPUT
    return exit_code


class FakeCatalog:
    """In-memory catalog used by --self-check. No database connection."""

    def __init__(self) -> None:
        self.objects = [
            {
                "object_id": 1,
                "schema": "dbo",
                "name": "SalesDailyActivity",
                "type": "USER_TABLE",
                "is_ms_shipped": False,
            },
            {
                "object_id": 2,
                "schema": "dbo",
                "name": "usp_GetApprovalRequest",
                "type": "SQL_STORED_PROCEDURE",
                "is_ms_shipped": False,
            },
            {
                "object_id": 3,
                "schema": "dbo",
                "name": "SalesApprovalRequest",
                "type": "USER_TABLE",
                "is_ms_shipped": False,
            },
            {
                "object_id": 4,
                "schema": "dbo",
                "name": "usp_Helper",
                "type": "SQL_STORED_PROCEDURE",
                "is_ms_shipped": False,
            },
            {
                "object_id": 5,
                "schema": "dbo",
                "name": "TVP_IntIds",
                "type": "TYPE_TABLE",
                "is_ms_shipped": False,
            },
            {
                "object_id": 6,
                "schema": "dbo",
                "name": "DeepTable",
                "type": "USER_TABLE",
                "is_ms_shipped": False,
            },
        ]
        self._by_id = {int(item["object_id"]): item for item in self.objects}

    def resolve_object(self, schema: str, name: str) -> List[Dict[str, Any]]:
        return [
            dict(item)
            for item in self.objects
            if item["name"] == name and item["schema"].lower() == schema.lower()
        ]

    def objects_by_ids(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return [dict(self._by_id[int(item)]) for item in ids if int(item) in self._by_id]

    def sql_dependencies(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        rows = [
            {
                "referencing_id": 2,
                "referencing_schema": "dbo",
                "referencing_object": "usp_GetApprovalRequest",
                "referencing_type": "SQL_STORED_PROCEDURE",
                "referencing_is_ms_shipped": False,
                "referenced_id": 3,
                "referenced_schema": "dbo",
                "referenced_object": "SalesApprovalRequest",
                "referenced_type": "USER_TABLE",
                "referenced_is_ms_shipped": False,
                "referenced_server_name": None,
                "referenced_database_name": None,
                "is_schema_bound_reference": False,
                "is_caller_dependent": False,
                "is_ambiguous": False,
            },
            {
                "referencing_id": 2,
                "referencing_schema": "dbo",
                "referencing_object": "usp_GetApprovalRequest",
                "referencing_type": "SQL_STORED_PROCEDURE",
                "referencing_is_ms_shipped": False,
                "referenced_id": None,
                "referenced_schema": "dbo",
                "referenced_object": "MissingObject",
                "referenced_type": None,
                "referenced_is_ms_shipped": False,
                "referenced_server_name": None,
                "referenced_database_name": None,
                "is_schema_bound_reference": False,
                "is_caller_dependent": False,
                "is_ambiguous": True,
            },
            {
                "referencing_id": 4,
                "referencing_schema": "dbo",
                "referencing_object": "usp_Helper",
                "referencing_type": "SQL_STORED_PROCEDURE",
                "referencing_is_ms_shipped": False,
                "referenced_id": 3,
                "referenced_schema": "dbo",
                "referenced_object": "SalesApprovalRequest",
                "referenced_type": "USER_TABLE",
                "referenced_is_ms_shipped": False,
                "referenced_server_name": None,
                "referenced_database_name": None,
                "is_schema_bound_reference": False,
                "is_caller_dependent": False,
                "is_ambiguous": False,
            },
            {
                "referencing_id": 2,
                "referencing_schema": "dbo",
                "referencing_object": "usp_GetApprovalRequest",
                "referencing_type": "SQL_STORED_PROCEDURE",
                "referencing_is_ms_shipped": False,
                "referenced_id": 2,
                "referenced_schema": "dbo",
                "referenced_object": "usp_GetApprovalRequest",
                "referenced_type": "SQL_STORED_PROCEDURE",
                "referenced_is_ms_shipped": False,
                "referenced_server_name": None,
                "referenced_database_name": None,
                "is_schema_bound_reference": False,
                "is_caller_dependent": False,
                "is_ambiguous": False,
            },
            {
                "referencing_id": 4,
                "referencing_schema": "dbo",
                "referencing_object": "usp_Helper",
                "referencing_type": "SQL_STORED_PROCEDURE",
                "referencing_is_ms_shipped": False,
                "referenced_id": 6,
                "referenced_schema": "dbo",
                "referenced_object": "DeepTable",
                "referenced_type": "USER_TABLE",
                "referenced_is_ms_shipped": False,
                "referenced_server_name": None,
                "referenced_database_name": None,
                "is_schema_bound_reference": False,
                "is_caller_dependent": False,
                "is_ambiguous": False,
            },
        ]
        wanted = {int(item) for item in ids}
        return [
            dict(row)
            for row in rows
            if row.get("referencing_id") in wanted or row.get("referenced_id") in wanted
        ]

    def fk_partners(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        wanted = {int(item) for item in ids}
        rows = [
            {
                "foreign_key_object_id": 99,
                "foreign_key": "FK_SalesApprovalRequest_Activity",
                "parent_object_id": 3,
                "referenced_object_id": 1,
                "parent_schema": "dbo",
                "parent_table": "SalesApprovalRequest",
                "referenced_schema": "dbo",
                "referenced_table": "SalesDailyActivity",
            }
        ]
        return [
            dict(row)
            for row in rows
            if int(row["parent_object_id"]) in wanted or int(row["referenced_object_id"]) in wanted
        ]

    def triggers_for_parents(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        return []

    def tvps_for_modules(self, ids: Sequence[int]) -> List[Dict[str, Any]]:
        wanted = {int(item) for item in ids}
        if 2 not in wanted:
            return []
        return [
            {
                "object_id": 5,
                "schema": "dbo",
                "name": "TVP_IntIds",
                "type": "TYPE_TABLE",
                "user_type_id": 50,
                "is_table_type": True,
                "from_module_id": 2,
            }
        ]


def run_discovery_self_check() -> List[str]:
    errors: List[str] = []
    log = ExtractionLog(quiet=True)
    config = default_config()
    config.update(
        {
            "schema": "dbo",
            "dependency_depth": 2,
            "include_triggers": True,
            "seed_objects": ["SalesDailyActivity"],
            "seed_procedures": ["usp_GetApprovalRequest", "DoesNotExist"],
        }
    )
    engine = DiscoveryEngine(FakeCatalog(), config, log, "AnmolDristi")  # type: ignore[arg-type]
    engine.run()
    by_name = {item.name: item for item in engine.objects.values()}
    if "SalesDailyActivity" not in by_name or by_name["SalesDailyActivity"].discovery_source != "seed":
        errors.append("Seed table was not recorded.")
    if "usp_GetApprovalRequest" not in by_name or by_name["usp_GetApprovalRequest"].discovery_source != "seed":
        errors.append("Seed procedure was not recorded.")
    if "DoesNotExist" not in by_name or by_name["DoesNotExist"].discovery_source != "unresolved":
        errors.append("Missing seed was not preserved as unresolved.")
    if "SalesApprovalRequest" not in by_name:
        errors.append("Referenced table was not discovered.")
    elif by_name["SalesApprovalRequest"].discovery_source != "dependency":
        errors.append("Referenced table discovery_source was not dependency.")
    if "usp_Helper" not in by_name:
        errors.append("Referenced-by procedure was not discovered.")
    if "MissingObject" not in by_name or by_name["MissingObject"].discovery_source != "unresolved":
        errors.append("Unresolved dependency was discarded.")
    if "TVP_IntIds" not in by_name:
        errors.append("TVP parameter type was not discovered.")
    if "DeepTable" in by_name:
        errors.append("Object beyond dependency_depth was included.")
    names = [item.name for item in engine.objects.values()]
    if names.count("usp_GetApprovalRequest") != 1:
        errors.append("Self-referencing procedure created duplicate inventory rows.")
    return errors


def run_self_check(example_path: Path) -> int:
    errors: List[str] = []

    query_errors = validate_named_queries()
    errors.extend(query_errors)

    try:
        config = load_config(example_path)
    except Exception as exc:
        errors.append(f"config.example.json could not be loaded: {exc}")
        config = default_config()
    if config.get("read_only") is not True:
        errors.append("config.example.json must set read_only to true.")
    if config.get("database") != "AnmolDristi":
        errors.append("config.example.json database must be AnmolDristi.")
    if int(config.get("dependency_depth", -1)) != 5:
        errors.append("config.example.json dependency_depth must be 5.")

    try:
        bad = default_config()
        bad["read_only"] = False
        bad["server"] = "example"
        validate_config(bad)
        errors.append("validate_config accepted read_only=false.")
    except ValueError:
        pass

    iso = iso_ist()
    if "+05:30" not in iso and iso.endswith("05:30"):
        pass
    if "+05:30" not in iso:
        errors.append(f"IST timestamp missing +05:30 offset: {iso}")

    source = Path(__file__).read_text(encoding="utf-8")
    for verb in ("INSERT", "UPDATE", "DELETE", "MERGE", "ALTER", "CREATE", "DROP", "TRUNCATE", "EXEC"):
        # Flag only executed-statement patterns, not catalog column names or comments.
        if re.search(rf'(?is)(?:cursor\.)?execute\(\s*["\']\s*{verb}\b', source):
            errors.append(f"Source contains execute() of {verb}.")

    # Cycle handling: visiting the same object_id twice must not recurse forever.
    seen: Set[int] = set()
    graph = {1: [2, 1], 2: [1, 3], 3: []}
    queue = deque([1])
    visits = 0
    while queue:
        current = queue.popleft()
        if current in seen:
            continue
        seen.add(current)
        visits += 1
        if visits > 10:
            errors.append("Cycle tracking failed: visit count exceeded bound.")
            break
        for nxt in graph[current]:
            if nxt not in seen:
                queue.append(nxt)
    if seen != {1, 2, 3}:
        errors.append(f"Cycle tracking produced unexpected visit set: {seen}")

    rows = [
        {"schema": "dbo", "table": "B"},
        {"schema": "dbo", "table": "A"},
    ]
    sorted_rows = sort_records(rows, ("schema", "table"))
    if [row["table"] for row in sorted_rows] != ["A", "B"]:
        errors.append("sort_records is not deterministic.")

    redacted = redact_settings({"username": "alice", "password": "secret", "server": "host"})
    if redacted.get("password") != "***" or redacted.get("username") != "alice":
        errors.append("Credential redaction failed.")

    try:
        assert_readonly_sql("INSERT INTO dbo.T VALUES (1)")
        errors.append("assert_readonly_sql accepted INSERT.")
    except RuntimeError:
        pass
    try:
        assert_readonly_sql("SELECT * FROM sys.tables")
    except RuntimeError as exc:
        errors.append(f"assert_readonly_sql rejected a valid SELECT: {exc}")

    errors.extend(run_discovery_self_check())

    if errors:
        print("SELF-CHECK FAILED", file=sys.stderr)
        for item in errors:
            print(f"  - {item}", file=sys.stderr)
        return EXIT_CONFIG
    print("SELF-CHECK PASSED")
    print(f"  named queries: {len(NAMED_QUERIES)}")
    print(f"  example config: {example_path}")
    print(f"  ist timestamp: {iso}")
    return EXIT_OK


def main(argv: Optional[Sequence[str]] = None) -> int:
    if sys.version_info < (3, 9):
        print("Python 3.9 or later is required (zoneinfo).", file=sys.stderr)
        return EXIT_CONFIG
    parser = build_arg_parser()
    args = parser.parse_args(argv)
    here = Path(__file__).resolve().parent
    if args.self_check:
        return run_self_check(here / "config.example.json")
    if not args.config:
        parser.error("--config is required unless --self-check is used")
    config_path = Path(args.config)
    try:
        config = load_config(config_path)
        config = apply_cli_overrides(config, args)
        if args.no_definitions:
            config["no_definitions"] = True
            config["write_view_definitions"] = False
        validate_config(config)
    except (FileNotFoundError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return EXIT_CONFIG
    return run_extraction(config, config_path)


if __name__ == "__main__":
    sys.exit(main())
