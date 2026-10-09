# AnimalRace Unity V3.1

This folder is the formal Unity-side structure for the H743-authoritative game.

- `Core`: bootstrap, display snapshot, main-thread queue, display state controller.
- `Serial`: Android AAR bridge, line buffering, reconnect, serial debug panel.
- `Protocol`: V3.1 messages, parser, validation, and sender.
- `Views`: main-scene presentation, race/result/WIN/payout/fault/settings views.
- `Sync`: duplicate-sequence handling, ACK, READY, and PING/PONG routing.
- `Debug`: H743 message simulator and scenario runner.
- `Tests/Editor`: parser, display-state, WIN-collection, and recovery tests.

Current compatibility policy:

1. H743 remains the only authority for CREDIT, WIN, bets, odds, winner, payout,
   and fault state.
2. Unity accepts the current H743 `state` field and the formal V3.1
   `mode`/`phase` fields.
3. Unity accepts `credit` and the legacy `balance` alias during migration.
4. Unknown appended fields are ignored; missing required fields fail parsing.
5. WIN transfer messages contain absolute WIN/CREDIT snapshots. Unity never
   performs local ledger arithmetic.
