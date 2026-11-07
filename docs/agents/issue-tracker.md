# Issue tracker: GitHub

Issues and specs for this repository live in GitHub Issues. Use the `gh` CLI for issue operations; infer the repository from `git remote -v`.

## Conventions

- Create an issue with `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- Read an issue with `gh issue view <number> --json number,title,body,labels,comments`.
- List issues with `gh issue list --state open --json number,title,body,labels,comments`.
- Comment with `gh issue comment <number> --body "..."`.
- Add or remove labels with `gh issue edit <number> --add-label "..."` or `--remove-label "..."`.
- Close an issue with `gh issue close <number>`; add an explanation with `gh issue comment` first.
- Represent parent-child work using GitHub sub-issues where available. Otherwise, add `Part of #<parent>` to the child issue body.
- Represent blocking with GitHub issue dependencies where available. Otherwise, add `Blocked by: #<number>` to the blocked issue body. A ticket is unblocked when all blockers are closed.

## Associating implementation commits

When implementing a numbered issue, include its reference (for example, `#123`) in the commit message so GitHub links the pushed commit to the issue. Keep the issue open unless closure is explicitly requested: do not use closing keywords such as `Fixes`, `Closes`, or `Resolves` in commit messages or pull request descriptions.

## Pull requests as a triage surface

**PRs as a request surface: no.** Pull requests are not triaged as feature requests by default.

## Publishing specs

When a skill says to publish a spec to the issue tracker, create a GitHub issue.
