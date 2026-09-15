# Data directory

This folder holds authoritative source materials and generated artifacts for LankaLens administrative divisions.

## Layout

| Path | Purpose |
|------|---------|
| `source/` | Original DCS/MOHA files (`.xlsx`/`.pdf`/MOHA HTML gitignored until licensing is clear) |
| `source/moha-life/` | MOHA LIFe cache (`README.md` committed; reports/manifest gitignored) |
| `source/sources.json` | Machine-readable provenance (URL, hash, dates) — committed |
| `source/snapshot-expectations.json` | Expected counts and multilingual coverage for the current source snapshot — committed |
| `source/nsdi-boundaries/` | NSDI GN polygon cache (`pages/` and `manifest.json` gitignored) |
| `mappings/` | Confirmed MOHA→DCS and NSDI→DCS code mappings, and authoritative name overlays — committed |
| `generated/` | DataBuilder outputs including production `administrative-divisions.json`, validation, coverage, join, deltas, and unresolved gaps |

## DataBuilder

```bash
dotnet run --project tools/LankaLens.DataBuilder -- inspect
dotnet run --project tools/LankaLens.DataBuilder -- acquire-moha
dotnet run --project tools/LankaLens.DataBuilder -- acquire-geo
dotnet run --project tools/LankaLens.DataBuilder -- validate
dotnet run --project tools/LankaLens.DataBuilder -- build
dotnet run --project tools/LankaLens.DataBuilder -- build-geo
```

`acquire-moha` downloads official MOHA LIFe GN reports once (rate-limited, cached). `validate` / `build` never call MOHA; they read the local snapshot and apply confirmed mappings from `data/mappings/moha-to-dcs.json` plus overlays from `data/mappings/authoritative-name-overlays.json`.

Successful `build` writes `data/generated/administrative-divisions.json` and copies the same bytes into `src/LankaLens.AdministrativeDivisions/Data/` for embedding.

`acquire-geo` downloads official NSDI Grama Niladhari boundary polygons once (paginated, rate-limited, cached). `build-geo` never calls NSDI; it reads the local snapshot, joins polygons to DCS codes on the NSDI `admin_code` attribute, applies confirmed mappings from `data/mappings/geo-to-dcs.json`, and writes:

| Output | Contents |
|--------|----------|
| `generated/gnd-list-with-coordinates.csv` | Every DCS GNDList row with `latitude`/`longitude` (a point guaranteed inside the division), the area-weighted centroid, bounding box, and area in km² appended |
| `generated/geo-coverage-report.md` | Join coverage, name disagreements, and every division left without coordinates |
| `generated/unresolved-coordinate-gaps.json` | Machine-readable unmatched DCS codes, unused NSDI features, and conflicts |

Coordinates are **not** embedded in the NuGet package — see [`docs/data-sources.md`](../docs/data-sources.md) for why. Divisions that cannot be resolved keep empty coordinate columns; they are never zero-filled or guessed.

### Verifying the coordinates

`build-geo` self-checks against NSDI's own hierarchy attributes, but those come from the same source as the geometry. For an independent check, [`scripts/verify-coordinates-osm.py`](../scripts/verify-coordinates-osm.py) compares each coordinate against an OpenStreetMap place node of the same name:

```bash
python3 scripts/verify-coordinates-osm.py                      # queries Overpass
python3 scripts/verify-coordinates-osm.py --cache /tmp/osm.json  # reuse a download
```

Current result: **median 0.80 km**, 76.6% within 2 km, 83.7% within 5 km across 2,756 comparable divisions. GN divisions are areas and OSM places are points, so exact agreement is not expected — a median around a kilometre means the polygons are attached to the right divisions. Rural districts with large divisions (Mannar, Polonnaruwa, Ampara) sit higher at 1.7–2.6 km, which is the expected consequence of division size rather than a defect.

OSM is ODbL-licensed and is used for **verification only** — no OSM coordinate is ever imported into the dataset, and the downloaded extract is not committed.

Place gitignored snapshots under `source/` using the filenames in `sources.json`. Do not silently overwrite an existing snapshot with a different SHA-256.

## Provenance policy

- Never invent geographic master data.
- Never silently replace a source file without updating provenance.
- If original files cannot be redistributed, store download URL, filename, retrieval date, SHA-256 hash, source organization, and effective/reference date instead.
- Production records must be traceable to an authoritative source.
- Unresolved Sinhala/Tamil values are emitted as JSON `null`, never placeholders.

See also:

- [`DATA-NOTICE.md`](../DATA-NOTICE.md) — software vs data licence boundary; data-source notice
- [`docs/data-licensing-review.md`](../docs/data-licensing-review.md) — Phase 5.1 redistribution analysis (historical)
- [`docs/data-sources.md`](../docs/data-sources.md)
- [`docs/contributing-data.md`](../docs/contributing-data.md)
