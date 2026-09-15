#!/usr/bin/env python3
"""Cross-check generated GN coordinates against OpenStreetMap.

OpenStreetMap is independent of both DCS and NSDI, so agreement between a generated
Grama Niladhari coordinate and an OSM place node of the same name is real corroboration
rather than a restatement of the join.

OSM data is ODbL-licensed. This script uses it **only to verify** LankaLens output; it
never imports OSM coordinates into the dataset, and nothing it downloads is committed.

Usage:
    python3 scripts/verify-coordinates-osm.py                 # fetch from Overpass, then compare
    python3 scripts/verify-coordinates-osm.py --cache osm.json  # reuse a previous download

Interpreting the result: GN divisions are areas and OSM place nodes are single points, so
exact agreement is not expected. A median around 1 km with the bulk inside a few kilometres
indicates the coordinates are attached to the right divisions. A large median, or a district
whose points sit far from every same-named OSM node, indicates a join problem.
"""

from __future__ import annotations

import argparse
import collections
import csv
import json
import math
import re
import statistics
import sys
import urllib.parse
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_CSV = REPO_ROOT / "data" / "generated" / "gnd-list-with-coordinates.csv"
OVERPASS_URL = "https://overpass-api.de/api/interpreter"
USER_AGENT = "LankaLens-verification/1.0 (coordinate cross-check; contact via repository)"

OVERPASS_QUERY = """
[out:json][timeout:300];
area["ISO3166-1"="LK"][admin_level=2]->.lk;
(
  node(area.lk)["place"~"^(city|town|village|suburb|hamlet|neighbourhood|locality)$"]["name"];
);
out body;
"""

# Allow a place node slightly outside the district's own coordinate extent.
DISTRICT_MARGIN_DEGREES = 0.05


def normalize(value: str | None) -> str:
    return re.sub(r"[^a-z0-9]", "", (value or "").lower())


def haversine_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    radius = 6371.0088
    rad = math.pi / 180
    a = (
        math.sin((lat2 - lat1) * rad / 2) ** 2
        + math.cos(lat1 * rad) * math.cos(lat2 * rad) * math.sin((lon2 - lon1) * rad / 2) ** 2
    )
    return 2 * radius * math.asin(math.sqrt(a))


def fetch_osm(cache: Path | None) -> list[dict]:
    if cache and cache.exists():
        print(f"Using cached OSM extract: {cache}")
        return json.loads(cache.read_text(encoding="utf-8"))["elements"]

    print("Querying Overpass for Sri Lankan place nodes (this takes a minute)...")
    request = urllib.request.Request(
        OVERPASS_URL,
        data=urllib.parse.urlencode({"data": OVERPASS_QUERY}).encode(),
        headers={"User-Agent": USER_AGENT},
    )
    with urllib.request.urlopen(request, timeout=300) as response:
        payload = response.read().decode("utf-8")

    if cache:
        cache.write_text(payload, encoding="utf-8")
        print(f"Cached OSM extract to {cache}")

    return json.loads(payload)["elements"]


def index_places(elements: list[dict]) -> dict[str, list[tuple[float, float]]]:
    places: dict[str, list[tuple[float, float]]] = collections.defaultdict(list)
    for element in elements:
        tags = element.get("tags", {})
        for raw in filter(None, [tags.get("name:en"), tags.get("name")]):
            # OSM sometimes packs alternates into one value with ";".
            for part in raw.split(";"):
                key = normalize(part)
                if key:
                    places[key].append((element["lat"], element["lon"]))
    return places


def district_extents(rows: list[dict]) -> dict[str, list[float]]:
    extents: dict[str, list[float]] = collections.defaultdict(lambda: [90.0, 180.0, -90.0, -180.0])
    for row in rows:
        if not row["latitude"]:
            continue
        box = extents[row["District_Name"]]
        lat, lon = float(row["latitude"]), float(row["longitude"])
        box[0] = min(box[0], lat)
        box[1] = min(box[1], lon)
        box[2] = max(box[2], lat)
        box[3] = max(box[3], lon)
    return extents


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--csv", type=Path, default=DEFAULT_CSV, help="coordinate CSV to verify")
    parser.add_argument("--cache", type=Path, default=None, help="path to cache/reuse the OSM extract")
    args = parser.parse_args()

    if not args.csv.exists():
        print(f"Coordinate CSV not found: {args.csv}\nRun: dotnet run --project tools/LankaLens.DataBuilder -- build-geo")
        return 2

    with args.csv.open(encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))

    places = index_places(fetch_osm(args.cache))
    extents = district_extents(rows)

    distances: list[float] = []
    per_district: dict[str, list[float]] = collections.defaultdict(list)
    compared = 0

    for row in rows:
        key = normalize(row["GND_Name"])
        if not row["latitude"] or key not in places:
            continue

        box = extents[row["District_Name"]]
        nearby = [
            point
            for point in places[key]
            if box[0] - DISTRICT_MARGIN_DEGREES <= point[0] <= box[2] + DISTRICT_MARGIN_DEGREES
            and box[1] - DISTRICT_MARGIN_DEGREES <= point[1] <= box[3] + DISTRICT_MARGIN_DEGREES
        ]
        if not nearby:
            continue

        lat, lon = float(row["latitude"]), float(row["longitude"])
        best = min(haversine_km(lat, lon, p[0], p[1]) for p in nearby)
        distances.append(best)
        per_district[row["District_Name"]].append(best)
        compared += 1

    if not distances:
        print("No comparable rows found.")
        return 1

    ordered = sorted(distances)
    total = len(rows)
    with_coords = sum(1 for r in rows if r["latitude"])

    print()
    print(f"Rows: {total:,}   with coordinates: {with_coords:,}   comparable to OSM: {compared:,}")
    print(f"Median distance to same-named OSM place: {statistics.median(ordered):.2f} km")
    for pct in (0.90, 0.95):
        print(f"  p{int(pct * 100)}: {ordered[int(len(ordered) * pct)]:.2f} km")
    for threshold in (1, 2, 5, 10):
        share = 100 * sum(1 for d in ordered if d <= threshold) / len(ordered)
        print(f"  within {threshold:2} km: {share:5.1f}%")

    print("\nWorst districts by median distance (investigate these first):")
    ranked = sorted(
        ((statistics.median(v), k, len(v)) for k, v in per_district.items() if len(v) >= 10),
        reverse=True,
    )
    for median, district, count in ranked[:8]:
        print(f"  {district:18} median {median:6.2f} km  (n={count})")

    return 0


if __name__ == "__main__":
    sys.exit(main())
