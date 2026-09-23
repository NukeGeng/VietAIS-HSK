# Repository layout and UI runtime boundary

## Runtime ownership

The repository has one production UI runtime:

```text
frontend/                  Vue 3 + Vite production UI
backend/VietAisHsk.Api/    ASP.NET Core API
backend/VietAisHsk.Workers/async worker process
```

`design-template/` is the approved panda visual reference and browser QA fixture. It is not a
second frontend, is not imported by Vue, and must not be included in the production image. It may
be maintained as a separate local template checkout while the approved visual is ported into
`frontend/`.

The static preview and the Vue application can be open at the same time during visual comparison,
but only `frontend/` is a deployable UI. A preview tab is not an additional product runtime.

## Naming invariant

Backend code must live below `backend/`. A root-level `src/` directory is invalid for this
repository because it makes the API boundary ambiguous and contradicts the solution layout.

Production frontend source must not reference the static preview server, its scripts, or the
`design-template` path. It may reuse approved visual tokens and component behavior by implementing
them in Vue-owned files.

## Verification

Run from the repository root:

```bash
node scripts/verify-repository-layout.mjs
```

The check verifies the required `frontend/` and `backend/` directories, rejects root `src/`, and
rejects template/preview references inside `frontend/src/`.
