#requires -Version 5.1
$ErrorActionPreference = 'Stop'

# Narrowly correct two documentation clauses. No staging or commit.
# Run from any directory inside the Anmol_Digitization Git worktree.

$ExpectedHead = '7a87804f329c924771858871c8ff9eb530aff009'
$Targets = @(
  'docs/INDEX.md',
  'docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md'
)

$root = (& git rev-parse --show-toplevel 2>$null).Trim()
if (-not $root) { throw 'STOP: not inside a Git worktree.' }
Set-Location $root
$head = (& git rev-parse HEAD).Trim()
if ($head -ne $ExpectedHead) { throw "STOP: HEAD is $head, expected $ExpectedHead. Review before editing." }

foreach ($p in $Targets) {
  if (-not (Test-Path -LiteralPath (Join-Path $root $p) -PathType Leaf)) {
    throw "STOP: target file missing: $p"
  }
}

$pythonScript = @'
import pathlib, shutil, subprocess, sys, datetime
root = pathlib.Path(sys.argv[1])
targets = [
    ("docs/INDEX.md",
     b"The path exists in no commit and no stash. Recorded evidence unavailable to this repository.",
     b"The path is absent from the inspected committed history and current worktree; 30 paths were found in the untracked payload of `stash@{1}^3`, reachable only through a reflog entry at the time of verification. This payload is not a committed or certified source of record."),
    ("docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md",
     b"AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`: in no commit and no stash.",
     b"AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`: absent from the inspected committed history and current worktree; 30 paths were found in the untracked payload of `stash@{1}^3`, reachable only through a reflog entry at the time of verification. This payload is not a committed or certified source of record.")
]
stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
backup = root.parent / f"TA_DA_DocFix_PreEdit_Backup_{stamp}"
backup.mkdir(parents=True, exist_ok=False)

# Preflight every target before writing any target.
prepared = []
for rel, old, new in targets:
    p = root / rel
    data = p.read_bytes()
    count = data.count(old)
    if count != 1:
        raise SystemExit(f"STOP: expected exactly one exact target clause in {rel}; found {count}. No docs edited.")
    prepared.append((rel, p, data, old, new))

for rel, p, data, old, new in prepared:
    (backup / pathlib.Path(rel).name).write_bytes(data)

# Apply only the two exact clause substitutions, preserving file bytes/newlines elsewhere.
for rel, p, data, old, new in prepared:
    p.write_bytes(data.replace(old, new, 1))

# Verify each new clause appears once and the old clause is absent.
for rel, p, data, old, new in prepared:
    updated = p.read_bytes()
    if updated.count(new) != 1 or old in updated:
        raise SystemExit(f"ERROR: post-write verification failed for {rel}. Original copies: {backup}")

record = (
    f"Pre-edit backup directory: {backup}\n"
    f"HEAD at edit: {subprocess.run(['git','rev-parse','HEAD'], cwd=root, text=True, capture_output=True, check=True).stdout.strip()}\n"
    "Changed files intended: docs/INDEX.md; docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md\n"
    "Operation: exact single-clause replacement in each file; no other content intentionally changed.\n"
)
(backup / "BACKUP_RECORD.txt").write_text(record, encoding="utf-8")
print(f"PRE_EDIT_BACKUP={backup}")
print("EDITED=2")
for rel, *_ in prepared:
    print(f"UPDATED={rel}")
'@
$temp = Join-Path $env:TEMP ("Apply_TADA_DocFix_" + [guid]::NewGuid().ToString('N') + ".py")
$pythonScript | Set-Content -LiteralPath $temp -Encoding UTF8
try {
  & python $temp $root
  if ($LASTEXITCODE -ne 0) { throw 'STOP: Python preflight/edit operation failed.' }
} finally {
  Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
}

Write-Host "`n--- diff check ---" -ForegroundColor Cyan
& git diff --check
if ($LASTEXITCODE -ne 0) { throw 'git diff --check failed; inspect the diff and pre-edit backup.' }

Write-Host "`n--- exact documentation diff ---" -ForegroundColor Cyan
& git --no-pager diff -- $Targets
Write-Host "`n--- status (nothing staged/committed by this script) ---" -ForegroundColor Cyan
& git status --short
Write-Host "`nCompleted narrow documentation correction. Do not stage or commit until the diff is reviewed."
