# ProbHammer
A live-game companion tool for Warhammer 40,000, built for a phone or tablet at the table.

## Description
Paste a real Warhammer 40k App (or BattleScribe/NewRecruit) 11th-edition army list export at
`/Import`. It's parsed and resolved against real BSData catalogue data, then rendered at
`/LivePlay`: a read-only reference view of your army — statlines, weapons, abilities, keywords,
detachment and army-wide rules, all with tap-to-view rule text — plus live casualty tracking
(alive/dead per model, half-strength and battle-shocked status) for the duration of a game.

The eventual goal is a two-roster attacker/defender flow with instant Monte Carlo combat
simulation for expected damage and kills. That isn't wired up yet — today's app covers the
army-reference and live-tracking half of that vision.

## Solution layout
The app lives under `Src/ProbHammer` — see that directory's own `CLAUDE.md` for architecture,
solution structure, and how to run it locally.
