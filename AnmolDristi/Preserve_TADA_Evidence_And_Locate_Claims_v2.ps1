#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$ExpectedHead = '7a87804f329c924771858871c8ff9eb530aff009'
$RelPath = 'AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation'
$Targets = @(
  'docs/INDEX.md',
  'docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md'
)

$root = (& git rev-parse --show-toplevel 2>$null).Trim()
if (-not $root) { throw 'Not inside a Git working tree.' }
Set-Location $root
$head = (& git rev-parse HEAD).Trim()
if ($head -ne $ExpectedHead) { throw "STOP: HEAD is $head; expected $ExpectedHead." }
foreach ($p in $Targets) {
  if (-not (Test-Path -LiteralPath (Join-Path $root $p) -PathType Leaf)) {
    throw "STOP: target missing: $p"
  }
}

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupRoot = Join-Path (Split-Path $root -Parent) "TA_DA_ReflogEvidence_RawBackup_$stamp"
New-Item -ItemType Directory -Path $backupRoot | Out-Null
$py = Join-Path $backupRoot 'preserve_verify.py'

$pythonScript = @'
import os, sys, subprocess, pathlib, hashlib, datetime
repo, relpath, expected_head, backup = sys.argv[1:5]

def git(*args, input_bytes=None):
    return subprocess.run(["git", *args], cwd=repo, input=input_bytes,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True).stdout

head = git("rev-parse", "HEAD").decode().strip()
if head != expected_head:
    raise SystemExit(f"STOP: HEAD changed: {head}")
stash = git("rev-parse", "stash@{1}").decode().strip()
payload = git("rev-parse", "stash@{1}^3").decode().strip()

raw = git("ls-tree", "-r", "-z", payload, "--", relpath)
entries = []
for rec in raw.split(b"\0"):
    if not rec:
        continue
    meta, path_b = rec.split(b"\t", 1)
    mode, typ, oid = meta.decode().split(" ")
    if typ != "blob":
        continue
    path = os.fsdecode(path_b)
    if not (path == relpath or path.startswith(relpath + "/")):
        continue
    entries.append((mode, oid, path))

if len(entries) != 30:
    raise SystemExit(f"STOP: expected 30 matching blobs, found {len(entries)}")

backup_path = pathlib.Path(backup)
payload_root = backup_path / "payload"
payload_root.mkdir(parents=True, exist_ok=True)
manifest_lines = ["mode\tblob\tpath\tbytes\tsha256"]
for mode, oid, path in entries:
    # Git paths must remain within the backup payload.
    rel = pathlib.PurePosixPath(path)
    if rel.is_absolute() or ".." in rel.parts:
        raise SystemExit(f"STOP: unsafe Git path: {path}")
    data = git("cat-file", "blob", oid)
    computed = git("hash-object", "--stdin", input_bytes=data).decode().strip()
    if computed != oid:
        raise SystemExit(f"STOP: raw blob hash mismatch for {path}: {computed} != {oid}")
    dest = payload_root.joinpath(*rel.parts)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(data)
    reread = dest.read_bytes()
    reread_oid = git("hash-object", "--stdin", input_bytes=reread).decode().strip()
    if reread_oid != oid:
        raise SystemExit(f"STOP: written backup failed hash verification: {path}")
    manifest_lines.append(f"{mode}\t{oid}\t{path}\t{len(data)}\t{hashlib.sha256(data).hexdigest()}")

manifest = backup_path / "git_blob_manifest.tsv"
manifest.write_text("\n".join(manifest_lines) + "\n", encoding="utf-8")
manifest_sha = hashlib.sha256(manifest.read_bytes()).hexdigest()
record = (
    f"Backup created UTC: {datetime.datetime.now(datetime.timezone.utc).isoformat()}\n"
    f"Repository root: {repo}\nHEAD: {head}\nStash ref: {stash}\n"
    f"Untracked-payload commit: {payload}\nVerified blobs/files: {len(entries)}\n"
    f"Manifest SHA256: {manifest_sha}\n"
    "Classification: raw Git-object preservation copy; not certified release evidence.\n"
)
(backup_path / "BACKUP_RECORD.txt").write_text(record, encoding="utf-8")
print(f"BACKUP_VERIFIED={len(entries)}")
print(f"STASH_REF={stash}")
print(f"PAYLOAD_COMMIT={payload}")
print(f"MANIFEST_SHA256={manifest_sha}")
print(f"BACKUP_PATH={backup_path}")
'@

$pythonScript | Set-Content -LiteralPath $py -Encoding UTF8
$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) { throw "Python not found. No repository files changed. Backup directory created at $backupRoot." }
& python $py $root $RelPath $ExpectedHead $backupRoot
if ($LASTEXITCODE -ne 0) { throw "STOP: raw-object preservation/verification failed. No repository files changed. Inspect $backupRoot." }

Write-Host "`n--- Affected statement candidates (read-only) ---" -ForegroundColor Cyan
foreach ($p in $Targets) {
  Write-Host "`n--- $p ---" -ForegroundColor Cyan
  Select-String -LiteralPath (Join-Path $root $p) -Pattern 'TA_DA_SQL_Reconciliation|no stash|no commit|no commits' -Context 1,1 |
    ForEach-Object { "{0}: {1}" -f $_.LineNumber, $_.Line }
}
Write-Host "`nPreservation complete. No repository files edited, staged, or committed."
Write-Host "Review the printed statements before authorizing the narrow documentation correction."
