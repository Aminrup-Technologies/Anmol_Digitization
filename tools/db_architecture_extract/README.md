# SQL Server Architecture Extractor

Read-only catalog extractor for the `AnmolDristi` SQL Server database.

This tool collects architecture evidence from SQL Server system catalogs. It does not modify the database, does not execute stored procedures, functions, or triggers, and does not interpret business architecture.

The guiding principle is: **extract evidence first; interpret evidence later.**

---

## Purpose

`extract_metadata.py` connects to Microsoft SQL Server with `pyodbc` and reads documented catalog views such as `sys.tables`, `sys.columns`, `sys.foreign_keys`, `sys.procedures`, `sys.sql_modules`, and `sys.sql_expression_dependencies`.

Extraction starts from configured seed tables and seed procedures, then expands references, referenced-by edges, foreign keys, triggers, and table-valued parameters up to a configurable dependency depth. The result is the SO-TA/DA dependency boundary, not a full database dump.

---

## Prerequisites

- **Python 3.9 or later** (uses `zoneinfo` for Asia/Kolkata timestamps)
- **ODBC Driver for SQL Server** (example: `ODBC Driver 17 for SQL Server`)
- **pyodbc**
- A **read-only** SQL Server login with:

```text
CONNECT
VIEW DEFINITION
SELECT
```

Do not grant `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `CREATE`, `DROP`, or `EXECUTE`. The extractor never requires those permissions.

---

## Installation

```bash
cd tools/db_architecture_extract
pip install -r requirements.txt
```

---

## Configuration

Copy the example file and edit the copy. Never commit the copy.

```bash
cp config.example.json config.json
```

`config.json` is Git-ignored. Only `config.example.json` belongs in source control.

Required fields:

| Field | Meaning |
| --- | --- |
| `server` | SQL Server host |
| `database` | Target database (`AnmolDristi`) |
| `schema` | Default schema for unqualified seed names (`dbo`) |
| `driver` | ODBC driver name |
| `trusted_connection` | `true` for Windows authentication |
| `username` / `password` | Used only when `trusted_connection` is `false` |
| `seed_objects` | Starting tables / objects |
| `seed_procedures` | Starting stored procedures |
| `dependency_depth` | Maximum BFS depth (default `5`) |
| `read_only` | Must be `true` or the tool refuses to run |

Credential environment variables (never written to artifacts):

```text
DB_EXTRACT_SERVER
DB_EXTRACT_DATABASE
DB_EXTRACT_USERNAME
DB_EXTRACT_PASSWORD
SQLCMDUSER
SQLCMDPASSWORD
```

Environment values override the corresponding config fields.

---

## Running

```bash
python extract_metadata.py --config config.json
```

Optional flags:

```bash
python extract_metadata.py --config config.json --output output
python extract_metadata.py --config config.json --dependency-depth 5
python extract_metadata.py --config config.json --no-definitions
python extract_metadata.py --config config.json --tables-only
python extract_metadata.py --config config.json --procedures-only
python extract_metadata.py --self-check
```

`--self-check` validates SQL safety, example configuration, timestamp format, cycle tracking, and credential redaction without opening a database connection.

---

## Output

Artifacts are written under `output/` (Git-ignored). JSON is UTF-8, 2-space indented, and sorted for stable diffs. SQL module files keep SQL Server's stored text unchanged.

| Artifact | Contents |
| --- | --- |
| `manifest.json` | Run identity, IST timestamp, seeds, object counts, warnings |
| `tables.json` | Table metadata (`object_id`, create/modify dates) |
| `columns.json` | Column types, nullability, identity, computed/default definitions |
| `foreign_keys.json` | One row per FK column pair, including actions and trusted/disabled state |
| `indexes.json` | Index type, uniqueness, filter, key columns, included columns |
| `constraints.json` | Primary key, unique, default, and check constraints |
| `procedures.json` | Stored procedure metadata |
| `procedure_parameters.json` | Procedure parameters, including TVP type names |
| `procedure_definitions/` | Verbatim `sys.sql_modules` text as `<schema>.<procedure>.sql` |
| `user_defined_table_types.json` | User-defined table types |
| `table_type_columns.json` | Table-type columns |
| `views.json` | View metadata (when `include_views` is true) |
| `view_definitions/` | Verbatim view definitions |
| `functions.json` | Scalar / inline TVF / multi-statement TVF metadata |
| `function_definitions/` | Verbatim function definitions |
| `triggers.json` | Trigger metadata, parent object, events, disabled state |
| `trigger_definitions/` | Verbatim trigger definitions |
| `dependencies.json` | `sys.sql_expression_dependencies` rows, including unresolved references |
| `discovered_objects.json` | Inventory with provenance (`seed` / `dependency` / `unresolved` / `external`) |
| `extraction_log.json` | Start/end IST timestamps, settings, query categories, warnings, errors |

Files for disabled categories are not written.

---

## Security

```text
The extractor must be run with a read-only account.
No database data is modified.
Stored procedures and functions are never executed.
Credentials are not written to output artifacts.
```

Additional controls:

- `config.read_only` must be `true`
- Every SQL batch is checked to be a single `SELECT` before execution
- `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `ALTER`, `CREATE`, `DROP`, `TRUNCATE`, `EXEC`, and `sp_executesql` are rejected
- Connections set `ApplicationIntent=ReadOnly`
- Passwords are never printed or stored in `manifest.json` / `extraction_log.json`
- `config.json` and `output/` are Git-ignored

---

## Scope and discovery

1. Resolve configured seed objects and seed procedures.
2. Walk `sys.sql_expression_dependencies` in both directions (references and referenced-by).
3. Include related foreign-key tables.
4. Include related triggers when enabled.
5. Include user-defined table types used as procedure parameters.
6. Stop at `dependency_depth` (default 5). Object identity (`object_id`) prevents cycles.

Unrelated database objects are not exported merely because they exist in the same database.

---

## SQL Server metadata limitations

These are catalog limitations, not extractor defects:

- `sys.sql_expression_dependencies` can omit dynamic SQL and some late-bound names.
- Unresolved references (`referenced_id` is NULL) are preserved; they are not discarded or invented.
- Encrypted modules return `NULL` from `sys.sql_modules.definition`; a warning is logged and no `.sql` file is written.
- `sys.parameters.has_default_value` is often `0` even when a default exists; the extractor records the catalog value as exposed.
- Cross-database and cross-server references are recorded as `external` and are not recursed.
- Index usage statistics are intentionally not collected in Phase A1.
