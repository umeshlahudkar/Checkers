# Global Design Tokens — Checkers (Coral theme)

## Surfaces
- App background: #17131c (radial gradient to #0e0c12 at edges, #201a2b at center-top glow)
- Panel / card: #221c2b
- Frame / recessed well (board frame, chip backgrounds): #2c2434
- Hairline border: #3a3044

## Text
- Primary text: #f6f0ea
- Secondary / dim text: #a99fb0
- On-accent text (buttons): #141018 (near-black, not white — better contrast on coral)

## Accent & semantic
- Accent (brand + CTA + active states): #ff6d5a
- Coin / currency: #f6b93b
- Danger (quit/forfeit only): #c23b2e

## Board
- Light square: #f1e7d6
- Dark square: #b98a63

## Pieces
- Player 1 (you): fill #ff6d5a, ring #ffb0a5, king glyph #7a1f16
- Player 2 (opponent): fill #f3ebdd, ring #ffffff, king glyph #8a7458

## Fonts
- Display / headings / numerals: Space Grotesk (500/600/700)
- Body / labels: DM Sans (400/500/600/700)

## Border & shadow rule
Only give a shape a border/stroke if it also carries elevation (a drop shadow) — the border implies a raised edge catching light. Flat, non-elevated shapes (tile fills, flat icon glyphs, plate/backdrop shapes) should have NO border. This applies to both the colored asset set (unity_assets/) and the white asset set (unity_assets_white/).

## White sprite set (unity_assets_white/)
Pure white silhouettes on transparent background, NO shadow, NO border/stroke — tint and add elevation yourself in Unity (e.g. via Image color + a separate shadow object). Structurally identical to unity_assets/, just recolorable.
