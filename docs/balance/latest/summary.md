# Balance probe: Act I, 2026-10-02 08:44

| Scenario | Way of playing | Walks out on run | Counters |
| --- | --- | --- | --- |
| shipped | A: wisps only | — | lab_visits 0, talk_attempts 0 |
| shipped | B: 15 hall candles, no phials | 25 | lab_visits 7, talk_attempts 1 |
| shipped | C: 15 hall candles, 10 phials (tidy) | 9 | lab_visits 3, talk_attempts 2 |
| shipped | D: 8 hall candles, 5 phials (loose) | 15 | lab_visits 3, talk_attempts 2 |
| drain growth 0.45 | A: wisps only | — | lab_visits 0, talk_attempts 0 |
| drain growth 0.45 | B: 15 hall candles, no phials | 29 | lab_visits 7, talk_attempts 1 |
| drain growth 0.45 | C: 15 hall candles, 10 phials (tidy) | 11 | lab_visits 4, talk_attempts 2 |
| drain growth 0.45 | D: 8 hall candles, 5 phials (loose) | 24 | lab_visits 7, talk_attempts 4 |
| talk x3.6 | A: wisps only | — | lab_visits 0, talk_attempts 0 |
| talk x3.6 | B: 15 hall candles, no phials | 24 | lab_visits 7, talk_attempts 1 |
| talk x3.6 | C: 15 hall candles, 10 phials (tidy) | 8 | lab_visits 2, talk_attempts 1 |
| talk x3.6 | D: 8 hall candles, 5 phials (loose) | 15 | lab_visits 3, talk_attempts 2 |

Scenarios and their changes:
- **shipped**: shipped numbers
- **drain growth 0.45**: LoopSettings.drainGrowthPerMinute = 0.45
- **talk x3.6**: TalkToRoland.durationMultiplier = 3.6
