# Import & Validation

## HSK curriculum import

Phải validate:
- SyllabusVersion;
- duplicate keys;
- HSK level mapping;
- Pinyin format;
- Hanzi format;
- Grammar mapping;
- lesson reference integrity.

## Pinyin / tone

Validate:
- normalized syllable representation;
- tone marks/numbers conversion nếu pipeline dùng cả hai;
- impossible/invalid combinations theo approved dataset rules;
- source/version.

## Hanzi stroke dataset

Validate:
- Hanzi exists;
- stroke count matches dataset record;
- ordered strokes non-empty;
- geometry/path format parseable;
- source/version/license metadata present.

## Import safety

- import phải idempotent theo key/version strategy;
- không silently overwrite platform-authored Vietnamese enrichment;
- lỗi phải có report;
- dry-run/preview nên có cho batch import lớn;
- rollback/re-import strategy phải được test trên staging trước production.
