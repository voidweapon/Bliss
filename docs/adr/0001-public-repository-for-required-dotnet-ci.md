---
status: accepted
---

# Make the repository public for required .NET CI checks

The current private-repository plan does not allow this repository to require a status check on `main`; GitHub reports that branch protection requires GitHub Pro or a public repository. We choose to make Bliss public so the .NET CI check can be required before merging and use standard GitHub-hosted Linux x64 runners without compute charges. This exposes repository contents, branches, tags, commit history, Issues, pull requests, comments, and CI artifacts to the public; the user confirmed these are approved for disclosure. The visibility change and CI workflow are accepted decisions but have not yet been applied.
