# Task brief template

Create one file at docs/tasks/<queue-id>.md and link it from the active portion of TASK_QUEUE.md. Keep merged work in the completed-history section, not in the active queue.

Required fields:

-   Queue ID/title, dated snapshot status, and an explicit instruction to check current origin/main, claims and PRs before work.
-   Exact master section link and queue dependencies; distinguish queue IDs from master milestone numbering when needed.
-   Three to seven full repository-relative Markdown links to files that exist, each with relevant types/methods and exact applicable test files. State explicitly when an implementation module or test suite does not exist.
-   A bounded next change that preserves all existing master/catalog scope and separates raw data evidence, generic implementation, and original parity.
-   Specific acceptance evidence, unresolved behavior and limits; no broad completion claims from a narrow slice.
-   Only focused checks applicable under AGENTS.md, including exact command and any skip conditions.
-   Model and effort from AGENTS.md; record a closest available substitute if unavailable.
-   Canonical-byte preservation, NEW/provisional labels, and open feature boxes until their own acceptance passes.

Before publishing, verify every local link and source/test path. Do not create IDs, queue scope, dependencies, branch claims, or completion status through the template.
