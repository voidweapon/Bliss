# Domain Docs

How engineering skills should consume this repository's domain documentation.

## Before exploring

- Read the root `GLOSSARY.md`, if it exists.
- Read relevant decisions in `docs/adr/`, if they exist.
- If these files do not exist, proceed silently; do not flag their absence or suggest creating them upfront.

## Use the glossary's vocabulary

When naming a domain concept in an issue, proposal, hypothesis, or test, use the term defined in `GLOSSARY.md`. If the needed concept is not documented, reconsider the terminology or note the gap for domain modeling.

## Flag ADR conflicts

If proposed work conflicts with an existing ADR, identify the ADR and explain the conflict rather than silently overriding it.
