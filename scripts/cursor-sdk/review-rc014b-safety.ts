/**
 * Local one-shot review of the untracked RC-014B SQL tests.
 *
 * Cloud agents clone a remote ref and would miss these working-tree files.
 * Agent.prompt disposes the local agent when the review finishes.
 *
 * Exit codes:
 *   0  review finished
 *   1  startup failure (missing key, auth, bad config)
 *   2  the agent ran and the run ended in error
 *   75 transient startup failure (safe to retry)
 */
import path from "node:path";
import { fileURLToPath } from "node:url";
import { Agent, CursorAgentError } from "@cursor/sdk";

const repoRoot = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../..",
);

const prompt = `Read only these two files. Do not modify anything. Do not run SQL. Do not open AnmolDristi/Resources, packages, or other tests.

- Database/Tests/RC014B/01_definition_shape_readonly.sql
- Database/Tests/RC014B/02_behavior_tran_rollback.sql

Check each file against the contract written in its own header. Report only violations, with the file name and the line that breaks the contract. If a file holds, say so in one line.

01_definition_shape_readonly.sql must:
- stay read-only: no BEGIN, COMMIT, ROLLBACK, and no call to dbo.usp_ProcessApprovalAction
- refuse any server other than MUMANMOAPP01\\SQLEXPRESS and any database other than AnmolDristiDev
- keep the three UNVERIFIED items unverified (no C# override, Employee cannot skip L1, universal L1 cannot skip Auditor) and never print PASS for them
- keep the authoring status as NOT EXECUTED and not UAT evidence

02_behavior_tran_rollback.sql must:
- never COMMIT
- implement the header's rollback assumption: the outer transaction is rolled back only when this script opened it, the call returned without error, @@TRANCOUNT is still 1, and XACT_STATE() is 1
- not try to reverse writes if a non-error return already cleared the outer transaction
- treat the accepted non-Auditor Completed write as accepted baseline, not as a failure
- keep the authoring status as NOT EXECUTED and not UAT evidence, and not authorize G4-G7`;

async function main(): Promise<void> {
  const apiKey = process.env.CURSOR_API_KEY?.trim();
  if (!apiKey) {
    console.error(
      "Missing CURSOR_API_KEY. Mint one at https://cursor.com/dashboard/cloud-agents.",
    );
    process.exit(1);
  }

  try {
    const result = await Agent.prompt(prompt, {
      apiKey,
      model: { id: "composer-2" },
      local: { cwd: repoRoot, settingSources: [] },
    });

    console.log(result.result ?? "(no output)");
    if (result.status !== "finished") {
      console.error(`run ended as ${result.status}`);
      process.exit(2);
    }
  } catch (err) {
    if (err instanceof CursorAgentError) {
      console.error(
        `startup failed: ${err.message} retryable=${err.isRetryable}`,
      );
      process.exit(err.isRetryable ? 75 : 1);
    }
    throw err;
  }
}

main();
