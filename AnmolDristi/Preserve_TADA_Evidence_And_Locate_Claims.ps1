#requires -Version 5.1
$ErrorActionPreference = 'Stop'

# Run this script from the repository root in PowerShell.
# It preserves the reflog-only stash payload and verifies every extracted Git blob
# before touching documentation. It then prints exact candidate lines for review.
# It does NOT stage or commit anything.

$ExpectedHead = '7a87804f329c924771858871c8ff9eb530aff009'
$RelPath = 'AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation'
$Targets = @(
  'docs/INDEX.md',
  'docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md'
)

$root = (& git rev-parse --show-toplevel 2>$null).Trim()
if (-not $root) { throw 'Not inside a Git working tree. Run from the repository.' }
Set-Location $root

$head = (& git rev-parse HEAD).Trim()
if ($head -ne $ExpectedHead) {
  throw "STOP: HEAD is $head, expected $ExpectedHead. Do not proceed without reconciling repository state."
}

foreach ($p in $Targets) {
  if (-not (Test-Path -LiteralPath (Join-Path $root $p) -PathType Leaf)) {
    throw "STOP: required target file missing: $p"
  }
}

# Confirm the specific reflog-only stash object still exists.
$stashCommit = (& git rev-parse 'stash@{1}^3' 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or -not $stashCommit) {
  throw 'STOP: stash@{1}^3 is unavailable. Do not edit documentation.'
}
$stashIdentity = (& git rev-parse 'stash@{1}' 2>$null).Trim()
$paths = @(& git ls-tree -r --name-only $stashCommit -- $RelPath)
$paths = @($paths | Where-Object { $_ -and ($_ -eq $RelPath -or $_.StartsWith("$RelPath/")) })
if ($paths.Count -ne 30) {
  throw "STOP: expected 30 matching paths in stash@{1}^3; found $($paths.Count)."
}

# Store backup outside the repository.
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupRoot = Join-Path (Split-Path $root -Parent) "TA_DA_ReflogEvidence_Backup_$stamp"
New-Item -ItemType Directory -Path $backupRoot | Out-Null
$archive = Join-Path $backupRoot 'TA_DA_SQL_Reconciliation_stash1_payload.tar'
& git archive --format=tar --output=$archive $stashCommit -- $RelPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $archive)) {
  throw 'STOP: git archive failed. Documentation was not changed.'
}

# Verify the archive includes the expected paths and that each archived file's
# Git blob ID matches the object database entry.
$manifest = Join-Path $backupRoot 'git_blob_manifest.txt'
$treeLines = @(& git ls-tree -r $stashCommit -- $RelPath)
$treeLines | Set-Content -LiteralPath $manifest -Encoding UTF8
$verifyScript = @'
import sys, tarfile, subprocess, hashlib, os
archive, repo, prefix = sys.argv[1:4]
with tarfile.open(archive, "r") as tf:
    members = [m for m in tf.getmembers() if m.isfile()]
    if len(members) != 30:
        raise SystemExit(f"FAIL: archive contains {len(members)} files, expected 30")
    verified = 0
    for m in members:
        f = tf.extractfile(m)
        data = f.read()
        p = m.name
        # Git archive may strip or retain a leading ./; normalize it.
        p = p[2:] if p.startswith("./") else p
        r = subprocess.run(["git", "hash-object", "--stdin"], input=data, cwd=repo,
                           stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
        blob = r.stdout.decode().strip()
        t = subprocess.run(["git", "ls-tree", "-r", prefix, "--", p], cwd=repo,
                           stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
        fields = t.stdout.decode().strip().split()
        if len(fields) < 3 or fields[2] != blob:
            # Archive path may be prefixed by the requested path; retry matching by suffix.
            raise SystemExit(f"FAIL: Git blob verification failed for {p}")
        verified += 1
print(f"VERIFIED: {verified} archived files match Git blob IDs.")
'@
$py = Join-Path $backupRoot 'verify_archive.py'
$verifyScript | Set-Content -LiteralPath $py -Encoding UTF8
$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) { throw "Backup archive created at $archive, but Python was not found; do not edit docs until blob verification is completed." }
& python $py $archive $root $stashCommit
if ($LASTEXITCODE -ne 0) { throw "Backup exists at $backupRoot, but verification failed. Documentation was not changed." }

@"
Backup directory: $backupRoot
Archive: $archive
Manifest: $manifest
Original stash ref: $stashIdentity
Stash third-parent commit: $stashCommit
Verified path count: $($paths.Count)
"@ | Set-Content -LiteralPath (Join-Path $backupRoot 'BACKUP_RECORD.txt') -Encoding UTF8

Write-Host "`nBACKUP COMPLETE AND VERIFIED. No repository files have been changed." -ForegroundColor Green
Write-Host "Backup: $backupRoot"
Write-Host "`nNow locating the two inaccurate statements; review exact text before applying any edit:"
foreach ($p in $Targets) {
  Write-Host "`n--- $p ---" -ForegroundColor Cyan
  Select-String -LiteralPath (Join-Path $root $p) -Pattern 'TA_DA_SQL_Reconciliation|no stash|no commit|no commits' -Context 1,1 |
    ForEach-Object { "{0}: {1}" -f $_.LineNumber, $_.Line }
}
Write-Host "`nNEXT: apply the narrow wording correction to the identified clause only, preserving every other clause. Then run git diff --check and inspect git diff. Do not stage or commit."
