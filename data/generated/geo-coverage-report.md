# Grama Niladhari coordinate coverage

Derived from the NSDI Boundaries Grama Niladhari polygon layer, attributed to DCS codes.

## Source snapshot

- Endpoint: `https://gisapps.nsdi.gov.lk/server/rest/services/Srilanka/Boundaries/MapServer/1/query`
- Retrieved: 2026-09-15
- Generalization: `maxAllowableOffset=5E-05` degrees
- Combined SHA-256: `AEF7543236AE0DBC5A20505C9EAE1467A49BD772B032BDA6DE586FBDD5EDB867`
- Features in service: 14,051

## Coverage

| Measure | Count |
|---|---:|
| DCS Grama Niladhari divisions | 14,008 |
| NSDI features | 14,051 |
| NSDI features with joinable code | 14,019 |
| **Matched (total)** | **13,959** |
| — by direct code | 13,567 |
| — by confirmed DS recode | 233 |
| — by confirmed GN mapping | 70 |
| — realigned by name within DS block | 89 |
| Unmatched (no coordinates) | 49 |
| NSDI features left unused | 60 |
| English-name disagreements | 498 |
| Centroid outside polygon | 255 |
| **Coverage** | **99.65%** |

`latitude`/`longitude` carry a representative point guaranteed to fall inside the
division. `centroid_latitude`/`centroid_longitude` carry the area-weighted centroid,
which lies outside its own polygon for 255 division(s) with
concave or multi-part shapes.

## Divisions without coordinates (49)

| DCS code | English name |
|---|---|
| `3128005` | Weihena |
| `3128010` | Indurupathvila |
| `3128015` | Polgahavila |
| `3128020` | Kumbalamalahena |
| `3128025` | Ihala Lelwala |
| `3128030` | Pahala Lelwala |
| `3128035` | Kirindalahena |
| `3128040` | Kokawala |
| `3128045` | Panvila |
| `3128050` | Wanduramba |
| `3128055` | Gulugahakanda |
| `3135005` | Urawatta |
| `3135010` | Dewagoda West |
| `3135015` | Dewagoda East |
| `3135020` | Deldoowa |
| `3135025` | Idanthota |
| `3135030` | Kuleegoda West |
| `3135035` | Galagoda East |
| `3135040` | Galagoda West |
| `3135045` | Kuleegoda East |
| `3135050` | Wellabada |
| `3135055` | Usmudulawa |
| `3135060` | wenamulla |
| `3135065` | Dimbuldoowa |
| `3135070` | Andurangoda |
| `3135075` | Galdoowa |
| `3135080` | Akurala |
| `3135085` | Akurala North |
| `3135090` | Akurala South |
| `3135095` | Uduwaragoda North |
| `3135100` | Uduwaragoda South |
| `3135105` | Kahawa |
| `3135110` | Weragoda |
| `3135115` | Delmar Colony |
| `3135120` | Harannagala |
| `3135125` | Godagama South |
| `3135130` | Godagama North |
| `3135135` | Daluwathumulla |
| `4415095` | Kokuthoduvai South |
| `5104005` | Punanai East |
| `9119005` | Thanjanthenna |
| `9119010` | Kalthota |
| `9119015` | Uggalkalthota Left Bank South |
| `9119020` | Medabedda |
| `9119025` | Neluyaya |
| `9119030` | Uggalkalthota Left Bank Left |
| `9119035` | Welipathayaya |
| `9119040` | Kuragala |
| `9119045` | Molamura |

## Conflicts (96)

| Code | Issue | Detail |
|---|---|---|
| `2203110` | CODE_NAME_REALIGNED | Code match pointed at NSDI 2203110 'Puwakpitiya', but 'Silwathgama' is uniquely NSDI 2203145 within DS block 2203; attached the name-matched polygon instead. |
| `2203145` | CODE_NAME_REALIGNED | Code match pointed at NSDI 2203145 'Silwathgama', but 'Palu Hombawa' is uniquely NSDI 2203155 within DS block 2203; attached the name-matched polygon instead. |
| `2203150` | CODE_NAME_REALIGNED | Code match pointed at NSDI 2203150 'Weragalawatta', but 'Dambawatawana' is uniquely NSDI 2203160 within DS block 2203; attached the name-matched polygon instead. |
| `2203155` | CODE_NAME_REALIGNED | Code match pointed at NSDI 2203155 'Palu Hombawa', but 'Puwakpitiya' is uniquely NSDI 2203110 within DS block 2203; attached the name-matched polygon instead. |
| `2203160` | CODE_NAME_REALIGNED | Code match pointed at NSDI 2203160 'Dambawatawana', but 'Weragalawatta' is uniquely NSDI 2203150 within DS block 2203; attached the name-matched polygon instead. |
| `2302020` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2303020 English name 'Dambagalla' differs from DCS 'Dambagolla'. |
| `2307560` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2306560 English name 'Merrygoll' differs from DCS 'Merrygold'. |
| `2313085` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2312085 English name 'Bearwell' differs from DCS 'Barewell'. |
| `2313340` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2312340 English name 'Albion' differs from DCS 'Albian'. |
| `2314235` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2315235 English name 'Injuster' differs from DCS 'Injustry'. |
| `2314245` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2315245 English name 'Newvalleygama' differs from DCS 'New Weligama'. |
| `2314275` | MAPPED_NAME_DISAGREEMENT | Mapped NSDI 2315275 English name 'Mocha' differs from DCS 'Moca'. |
| `3127115` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127115 'Weihena', but 'Mahalapitiya' is uniquely NSDI 3127120 within DS block 3127; attached the name-matched polygon instead. |
| `3127120` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127120 'Mahalapitiya', but 'Pilagoda' is uniquely NSDI 3127140 within DS block 3127; attached the name-matched polygon instead. |
| `3127125` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127125 'Polgahavila', but 'Thilaka Udagama' is uniquely NSDI 3127135 within DS block 3127; attached the name-matched polygon instead. |
| `3127130` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127130 'Indurupathvila', but 'Ganegama East' is uniquely NSDI 3127145 within DS block 3127; attached the name-matched polygon instead. |
| `3127135` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127135 'Thilaka Udagama', but 'Ganegama South' is uniquely NSDI 3127150 within DS block 3127; attached the name-matched polygon instead. |
| `3127140` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127140 'Pilagoda', but 'Ganegama West' is uniquely NSDI 3127155 within DS block 3127; attached the name-matched polygon instead. |
| `3127145` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127145 'Ganegama East', but 'Lelkada' is uniquely NSDI 3127160 within DS block 3127; attached the name-matched polygon instead. |
| `3127150` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127150 'Ganegama South', but 'Ginimellagaha West' is uniquely NSDI 3127165 within DS block 3127; attached the name-matched polygon instead. |
| `3127155` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127155 'Ganegama West', but 'Ginimellagaha East' is uniquely NSDI 3127170 within DS block 3127; attached the name-matched polygon instead. |
| `3127160` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127160 'Lelkada', but 'Dodangoda' is uniquely NSDI 3127175 within DS block 3127; attached the name-matched polygon instead. |
| `3127165` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127165 'Ginimellagaha West', but 'Keembiela' is uniquely NSDI 3127190 within DS block 3127; attached the name-matched polygon instead. |
| `3127170` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127170 'Ginimellagaha East', but 'Kasideniya' is uniquely NSDI 3127200 within DS block 3127; attached the name-matched polygon instead. |
| `3127175` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127175 'Dodangoda', but 'Warakapitikanda' is uniquely NSDI 3127250 within DS block 3127; attached the name-matched polygon instead. |
| `3127180` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127180 'Thelikada', but 'Pahala Keembiya' is uniquely NSDI 3127245 within DS block 3127; attached the name-matched polygon instead. |
| `3127190` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127190 'Keembiela', but 'Thelikada' is uniquely NSDI 3127180 within DS block 3127; attached the name-matched polygon instead. |
| `3127195` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127195 'Kohombanadeniya', but 'Ginimellagaha South' is uniquely NSDI 3127270 within DS block 3127; attached the name-matched polygon instead. |
| `3127200` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127200 'Kasideniya', but 'Pituwalgoda' is uniquely NSDI 3127275 within DS block 3127; attached the name-matched polygon instead. |
| `3127205` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127205 'Kirindalahena', but 'Thelikada Nagaraya' is uniquely NSDI 3127280 within DS block 3127; attached the name-matched polygon instead. |
| `3127210` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127210 'Pahala Lelwala', but 'Balagoda' is uniquely NSDI 3127265 within DS block 3127; attached the name-matched polygon instead. |
| `3127215` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127215 'Ihala Lelwala', but 'Adurathvila' is uniquely NSDI 3127255 within DS block 3127; attached the name-matched polygon instead. |
| `3127220` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127220 'Kumbalamalahena', but 'Kohombanadeniya' is uniquely NSDI 3127195 within DS block 3127; attached the name-matched polygon instead. |
| `3127225` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127225 'Wanduramba', but 'Walpita South' is uniquely NSDI 3127260 within DS block 3127; attached the name-matched polygon instead. |
| `3127230` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127230 'Gulugahakanda', but 'Gonapura' is uniquely NSDI 3127290 within DS block 3127; attached the name-matched polygon instead. |
| `3127235` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127235 'Panvila', but 'Horagampita Central' is uniquely NSDI 3127285 within DS block 3127; attached the name-matched polygon instead. |
| `3127240` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3127240 'Kokawala', but 'Horagampita' is uniquely NSDI 3127295 within DS block 3127; attached the name-matched polygon instead. |
| `3136005` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136005 'Urawatta', but 'Wellawatta' is uniquely NSDI 3136195 within DS block 3136; attached the name-matched polygon instead. |
| `3136010` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136010 'Dewagoda West', but 'Nakanda' is uniquely NSDI 3136220 within DS block 3136; attached the name-matched polygon instead. |
| `3136015` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136015 'Dewagoda East', but 'Hikkaduwa Central' is uniquely NSDI 3136225 within DS block 3136; attached the name-matched polygon instead. |
| `3136020` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136020 'Deldoowa', but 'Hikkaduwa West' is uniquely NSDI 3136215 within DS block 3136; attached the name-matched polygon instead. |
| `3136025` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136025 'Idanthota', but 'Hikkaduwa Nagarikaya' is uniquely NSDI 3136200 within DS block 3136; attached the name-matched polygon instead. |
| `3136030` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136030 'Kuleegoda West', but 'Wavulagoda West' is uniquely NSDI 3136205 within DS block 3136; attached the name-matched polygon instead. |
| `3136035` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136035 'Galagoda East', but 'Wavulagoda East' is uniquely NSDI 3136210 within DS block 3136; attached the name-matched polygon instead. |
| `3136040` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136040 'Galagoda West', but 'Nalagasdeniya' is uniquely NSDI 3136230 within DS block 3136; attached the name-matched polygon instead. |
| `3136045` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136045 'Kuleegoda East', but 'Millagoda' is uniquely NSDI 3136235 within DS block 3136; attached the name-matched polygon instead. |
| `3136050` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136050 'Wellabada', but 'Wewala' is uniquely NSDI 3136245 within DS block 3136; attached the name-matched polygon instead. |
| `3136055` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136055 'Usmudulawa', but 'Pannamgoda' is uniquely NSDI 3136240 within DS block 3136; attached the name-matched polygon instead. |
| `3136060` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136060 'Wenamulla', but 'Narigama Wellabada' is uniquely NSDI 3136250 within DS block 3136; attached the name-matched polygon instead. |
| `3136065` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136065 'Dimbuldoowa', but 'Narigama' is uniquely NSDI 3136255 within DS block 3136; attached the name-matched polygon instead. |
| `3136070` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136070 'Andurangoda', but 'Kuda Wewala' is uniquely NSDI 3136260 within DS block 3136; attached the name-matched polygon instead. |
| `3136075` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136075 'Galdoowa', but 'Delgahadoowa' is uniquely NSDI 3136265 within DS block 3136; attached the name-matched polygon instead. |
| `3136080` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136080 'Akurala', but 'Katukoliha' is uniquely NSDI 3136270 within DS block 3136; attached the name-matched polygon instead. |
| `3136085` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136085 'Akurala North', but 'Thiranagama' is uniquely NSDI 3136275 within DS block 3136; attached the name-matched polygon instead. |
| `3136090` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136090 'Akurala South', but 'Thiranagama Wellabada' is uniquely NSDI 3136280 within DS block 3136; attached the name-matched polygon instead. |
| `3136095` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136095 'Uduwaragoda North', but 'Patuwatha' is uniquely NSDI 3136285 within DS block 3136; attached the name-matched polygon instead. |
| `3136100` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136100 'Uduwaragoda South', but 'Gammaduwatta' is uniquely NSDI 3136290 within DS block 3136; attached the name-matched polygon instead. |
| `3136105` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136105 'Kahawa', but 'Hennathota' is uniquely NSDI 3136295 within DS block 3136; attached the name-matched polygon instead. |
| `3136110` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136110 'Weragoda', but 'Pinkanda' is uniquely NSDI 3136300 within DS block 3136; attached the name-matched polygon instead. |
| `3136115` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136115 'Delmar Colony', but 'Handaudumulla' is uniquely NSDI 3136305 within DS block 3136; attached the name-matched polygon instead. |
| `3136120` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136120 'Harannagala', but 'Dodandugoda' is uniquely NSDI 3136340 within DS block 3136; attached the name-matched polygon instead. |
| `3136125` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136125 'Godagama South', but 'Modara Patuwatha' is uniquely NSDI 3136345 within DS block 3136; attached the name-matched polygon instead. |
| `3136130` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136130 'Godagama North', but 'Dodandoowa' is uniquely NSDI 3136350 within DS block 3136; attached the name-matched polygon instead. |
| `3136135` | CODE_NAME_REALIGNED | Code match pointed at NSDI 3136135 'Daluwathumulla', but 'Uduhalpitiya' is uniquely NSDI 3136355 within DS block 3136; attached the name-matched polygon instead. |
| `5212200` | CODE_NAME_REALIGNED | Code match pointed at NSDI 5212200 'Uhana Thissapura East', but 'Uhana Thissapura West' is uniquely NSDI 5212205 within DS block 5212; attached the name-matched polygon instead. |
| `5212205` | CODE_NAME_REALIGNED | Code match pointed at NSDI 5212205 'Uhana Thissapura West', but 'Uhana Thissapura East' is uniquely NSDI 5212200 within DS block 5212; attached the name-matched polygon instead. |
| `9118060` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118060 'Thanjanthenna', but 'Rajawaka' is uniquely NSDI 9118105 within DS block 9118; attached the name-matched polygon instead. |
| `9118065` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118065 'Kalthota', but 'Bowatta' is uniquely NSDI 9118110 within DS block 9118; attached the name-matched polygon instead. |
| `9118070` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118070 'Uggalkalthota Left Bank South', but 'Vikiliya' is uniquely NSDI 9118115 within DS block 9118; attached the name-matched polygon instead. |
| `9118075` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118075 'Medabedda', but 'Thotupalathenna' is uniquely NSDI 9118120 within DS block 9118; attached the name-matched polygon instead. |
| `9118080` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118080 'Neluyaya', but 'Kirimetithenna' is uniquely NSDI 9118125 within DS block 9118; attached the name-matched polygon instead. |
| `9118085` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118085 'Uggalkalthota Left Bank Left', but 'Dehigasthalawa' is uniquely NSDI 9118130 within DS block 9118; attached the name-matched polygon instead. |
| `9118090` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118090 'Welipathayaya', but 'Balangoda Town' is uniquely NSDI 9118135 within DS block 9118; attached the name-matched polygon instead. |
| `9118095` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118095 'Kuragala', but 'Balangoda' is uniquely NSDI 9118140 within DS block 9118; attached the name-matched polygon instead. |
| `9118100` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118100 'Molamura', but 'Jahinkanda' is uniquely NSDI 9118145 within DS block 9118; attached the name-matched polygon instead. |
| `9118105` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118105 'Rajawaka', but 'Meddekanda' is uniquely NSDI 9118150 within DS block 9118; attached the name-matched polygon instead. |
| `9118110` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118110 'Bowatta', but 'Ellewatta' is uniquely NSDI 9118155 within DS block 9118; attached the name-matched polygon instead. |
| `9118115` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118115 'Vikiliya', but 'Polwathugoda' is uniquely NSDI 9118160 within DS block 9118; attached the name-matched polygon instead. |
| `9118120` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118120 'Thotupalathenna', but 'Durakanda' is uniquely NSDI 9118165 within DS block 9118; attached the name-matched polygon instead. |
| `9118125` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118125 'Kirimetithenna', but 'Rassagala' is uniquely NSDI 9118170 within DS block 9118; attached the name-matched polygon instead. |
| `9118130` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118130 'Dehigasthalawa', but 'Ampitiyawatta' is uniquely NSDI 9118175 within DS block 9118; attached the name-matched polygon instead. |
| `9118135` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118135 'Balangoda Town', but 'Massenna' is uniquely NSDI 9118180 within DS block 9118; attached the name-matched polygon instead. |
| `9118140` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118140 'Balangoda', but 'Pettigala' is uniquely NSDI 9118185 within DS block 9118; attached the name-matched polygon instead. |
| `9118145` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118145 'Jahinkanda', but 'Pallekanda' is uniquely NSDI 9118190 within DS block 9118; attached the name-matched polygon instead. |
| `9118150` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118150 'Meddekanda', but 'Ellepola' is uniquely NSDI 9118195 within DS block 9118; attached the name-matched polygon instead. |
| `9118155` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118155 'Ellewatta', but 'Thalangama' is uniquely NSDI 9118200 within DS block 9118; attached the name-matched polygon instead. |
| `9118160` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118160 'Polwathugoda', but 'Kumara Gama' is uniquely NSDI 9118205 within DS block 9118; attached the name-matched polygon instead. |
| `9118165` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118165 'Durakanda', but 'Damahana' is uniquely NSDI 9118210 within DS block 9118; attached the name-matched polygon instead. |
| `9118170` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118170 'Rassagala', but 'Godakumbura' is uniquely NSDI 9118230 within DS block 9118; attached the name-matched polygon instead. |
| `9118175` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118175 'Ampitiyawatta', but 'Imbulamura' is uniquely NSDI 9118235 within DS block 9118; attached the name-matched polygon instead. |
| `9118180` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118180 'Massenna', but 'Mahawalathenna' is uniquely NSDI 9118240 within DS block 9118; attached the name-matched polygon instead. |
| `9118185` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118185 'Pettigala', but 'Theladiriya' is uniquely NSDI 9118245 within DS block 9118; attached the name-matched polygon instead. |
| `9118190` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118190 'Pallekanda', but 'Mawela' is uniquely NSDI 9118250 within DS block 9118; attached the name-matched polygon instead. |
| `9118195` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118195 'Ellepola', but 'Welage' is uniquely NSDI 9118255 within DS block 9118; attached the name-matched polygon instead. |
| `9118200` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118200 'Thalangama', but 'Gawaranhena' is uniquely NSDI 9118260 within DS block 9118; attached the name-matched polygon instead. |
| `9118205` | CODE_NAME_REALIGNED | Code match pointed at NSDI 9118205 'Kumara Gama', but 'Horaketiya' is uniquely NSDI 9118265 within DS block 9118; attached the name-matched polygon instead. |

