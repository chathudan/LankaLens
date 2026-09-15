# Changelog

All notable changes to **LankaLens.AdministrativeDivisions** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0/).

## [Unreleased]

### Added

- DataBuilder `acquire-geo` and `build-geo` commands producing `data/generated/gnd-list-with-coordinates.csv`
  — the DCS GNDList rows with latitude/longitude, centroid, bounding box, and area appended
- NSDI Grama Niladhari boundary layer as a provenance-tracked source, joined to DCS codes on `admin_code`
- Confirmed NSDI→DCS recode mappings in `data/mappings/geo-to-dcs.json` (5 DS recodes, 70 GN-level)
- Within-DS-block name realignment: corrects 89 divisions where DCS and NSDI assign different codes
  to the same named division, which a code-only join attaches to the wrong polygon
- `scripts/verify-coordinates-osm.py` — independent OpenStreetMap cross-check of generated coordinates
  (verification only; no OSM data is imported)

### Changed

- Corrected the NSDI assessment in `docs/multilingual-source-analysis.md` §F: the layer *is* joinable
  to DCS codes via `admin_code` (the earlier "not production-ready" finding examined `gnd_census_code`,
  which is null throughout). The layer remains unsuitable as a multilingual authority.

### Note

- Coordinates are a derived side output only. They are **not** embedded in the NuGet package, and the
  public API is unchanged — no new types, members, or behaviour.

### Changed

- Removed the permission-based NuGet publication gate (DCS/MOHA written permission is no longer required before publish); technical release gates remain

### Planned

- First public NuGet prerelease **`0.1.0-preview.1`** (not yet published to NuGet.org)

### Added

- Phase 5 release hardening: NuGet metadata, Source Link, symbols, deterministic CI packaging
- SDK package validation baseline under `eng/api-compat/`
- Package-oriented README for NuGet.org rendering
- Immutability, concurrency, culture-independence, and Unicode integrity tests
- Dataset SHA-256 fingerprint in `data/source/snapshot-expectations.json`
- Manual-only GitHub Actions publish workflow (disabled for automatic pushes)
- Public API review (`docs/api-review.md`)

### Data

- Bundled production snapshot: 9 provinces, 25 districts, 340 DS, 14,008 GN
- Multilingual support with authoritative Sinhala/Tamil (GN 13,723/14,008; 285 GN values intentionally null)
- Offline embedded dataset, hierarchy navigation, and multilingual search
- Known limitation: 285 GN Sinhala/Tamil values remain unresolved (no machine translation)

## [0.1.0] - TBD

Bootstrap through Phase 4. Remains pre-1.0; not published to NuGet.org.

### Added

- Solution and project structure for the library, tests, DataBuilder, and samples
- Shared `Directory.Build.props`, `.editorconfig`, MIT license, and README
- Minimal GitHub Actions CI (restore, build, test, pack — no publish)
- Phase 2 public domain models, `IAdministrativeDivisionProvider`, and multilingual search
- Phase 3 DataBuilder CLI (`inspect` / `validate` / `build` / `acquire-moha`)
- Phase 3.5–3.8 multilingual source discovery, MOHA join, mappings, overlays, and gap documentation
- Phase 4 production canonical dataset generation and runtime embedding
