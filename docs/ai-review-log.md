# AI review log

Use this log for material AI output that was rejected, substantially corrected, based on a false assumption, unnecessarily complex, changed after validation, or accepted only after a specific verification step. Do not log trivial edits to create activity. Entries must describe work that actually happened.

## Entry format

For each real example, record:

- Date, milestone, and the affected artifact/revision.
- Link to the relevant prompt record, when prompt recording applies.
- The original claim, output, or approach under review, without secrets.
- The actual finding and its severity or significance.
- Disposition: rejected, corrected, or accepted after verification; explain why.
- The specific check or authoritative reference used, its actual result, and an evidence link when available.
- The correction and any remaining limitation or follow-up.

Keep original evidence and subsequent corrections distinguishable. Add follow-up notes rather than silently changing a past finding. Routine check output belongs in `evidence/test-output/` or `evidence/reviews/`; reference it here only for a material example.

## 2026-09-14: Generated-output exclusions were too narrow

- Context: repository-tooling follow-up after milestone 1. This did not complete a new product milestone.
- Initial AI output: `.gitignore` excluded `coverage`, `logs`, and `artifacts` only at the repository root. The initial checks covered root outputs but missed the corresponding nested project paths.
- Finding: the final reviewer identified an Important gap. `git check-ignore --no-index -q` returned exit code 1 for `tests/Sample.Tests/coverage/coverage.cobertura.xml`, `tests/Sample.Tests/logs/test.log`, and `tests/Sample.Tests/artifacts/report.xml`, confirming that these generated paths remained trackable.
- Correction: remove the root-only restriction and add directory-specific exceptions beneath `evidence/` for deliberately retained reports. Keep secret-file exclusions active inside the evidence directories.
- Validation: an inline Python loop invoking `git check-ignore --no-index -q` passed 43 ignored-path and 32 trackable-path cases after the correction. These included nested outputs, evidence in original output directories, and private environment/credential files beneath evidence. No probe files were created. The project-owned staged whitespace check passed.
- Disposition: corrected and accepted after the focused re-review. The reviewer returned `APPROVE_WITH_MINOR_NOTES`, independently verified 11 ignored and 12 trackable paths, and reported no remaining Critical or Important findings. Its only Minor note was inherited formatting in the imported skills. No runtime or skill-behavior testing is claimed.
- Separate inherited observation: the full staged whitespace check reports nine trailing-space lines and twelve blank lines at EOF in the imported skill files. Those supplied files are preserved byte-for-byte; the full check is not represented as passing.
