# AGENTS.md

## First rule

Read `GOAL.md` first.

Then read only documents required by the current task.

## Documentation-first

For any module task:

1. Read relevant `docs/product/` scope.
2. Read the module `<Module>.md`.
3. Read `bugs.md`.
4. Read `fixed.md`.
5. Read relevant architecture/data/security/building-block docs.
6. Confirm scope and dependencies.
7. Implement.
8. Run automated tests.
9. Use Computer Use for real UI flow when applicable.
10. Record bugs.
11. Fix, retest and update `fixed.md`.

## Current UI rule

`design-template/` HTML/CSS is the approved visual source before Vue implementation.

Do not use Figma as product UI source of truth.

## No architecture invention

Do not introduce:
- new architectural style;
- new business module;
- new messaging pattern;
- new shared abstraction;
- new cross-module contract;
- new dependency

unless documented need exists and scope is updated/approved.

A new sidebar item is NOT automatically a new backend module.

## Tests

Do not delete tests to make builds pass.

Build success != done.

## Permissions

Never hard-code permission behavior by display role name.

## Docs are part of implementation

If behavior changes, update the corresponding Markdown in the same task.

Task is incomplete if docs and implementation disagree.
