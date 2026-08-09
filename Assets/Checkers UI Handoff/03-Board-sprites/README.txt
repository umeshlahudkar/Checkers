CHECKERS / DRAUGHTS SPRITE SET — game-ready, transparent PNG (straight alpha)

sprites/1024/   master resolution
sprites/512/    downscaled from the masters (nothing is upscaled)

  tile_light.png    1024 x 1024   seamless wood board tile (light maple) - tiles in both axes
  tile_dark.png     1024 x 1024   seamless wood board tile (dark cherry) - tiles in both axes
  piece_white.png   1024 x 1024   man, ivory, no padding (from your reference render)
  piece_black.png   1024 x 1024   man, black, no padding (same geometry, black lacquer finish)
  king_white.png    1024 x 1024   king, ivory with gold crown, no padding (from your reference render)
  king_black.png    1024 x 1024   king, black with silver crown, no padding (same geometry)
  frame_square.png  1024 x 1024   board frame for square boards, hollow centre
  board_assembled.png 1024 x 1024 preview: full 8x8 board with frame + starting position
                      (reference only - assemble the board from the pieces above in-engine,
                      don't ship this as a single sprite)
  frame_board.png   1408 x 1024   same frame, wide format, hollow centre

UNITY IMPORT
  Texture Type ......... Sprite (2D and UI)
  Sprite Mode .......... Single
  Alpha Is Transparency  ON   (straight alpha; prevents dark edge fringing)
  Mesh Type ............ Tight for pieces, Full Rect for tiles and the frame
  Wrap Mode ............ Repeat for tile_*.png, Clamp for everything else
  Filter Mode .......... Bilinear    Mip Maps: ON for tiles, OFF for the UI frame
  Compression .......... High Quality (BC7 desktop / ASTC 6x6 mobile)
  Pixels Per Unit ...... 1024, so one piece is exactly one board square wide at 1:1

FRAMES
  Use frame_square.png on a square (8x8) board at its native 1:1 - the inner opening
  starts 47 px in from each edge, i.e. 4.59% of the sprite, so the tile grid butts
  exactly against the wood. Never stretch a frame to a different aspect; 9-slice it.

FRAME 9-SLICE (frame_board.png, 1408 x 1024 master)
  Bar width ......... 44 px
  Brass plate ....... 99 px from each outer edge
  Border (L,B,R,T) .. 100, 100, 100, 100   <- keeps the brass plates unstretched,
                      only the wood bars stretch. Halve to 50 for the 512 version
                      (704 x 512).

NOTES
  - Pieces are centred in the canvas; pivot can stay Center, no offset needed.
  - No baked drop shadow, so shadows follow your in-engine lighting.
  - Tiles are seamless: one quad per square, or one tiled quad for the whole board.
  - Sprites are rendered at 1024; regenerate larger if you need 2K masters.
