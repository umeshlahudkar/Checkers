CHECKERS ROYAL ARENA - UI HANDOFF
=================================
Design canvas: 1080 x 1920 (portrait). Every sprite is authored 1:1 with the
design - the numbers in a filename are the exact pixels the element occupies
on screen. Nothing is ever upscaled.


01-Reference-Screens/   (12 screens, each exactly 1080x1920)
--------------------------------------------------------------------
      01-main-menu_1080x1920.png
      02-mode-selection_1080x1920.png
      03-gameplay_1080x1920.png
      04-victory_1080x1920.png
      05-defeat_1080x1920.png
      06-draw_1080x1920.png
      07-settings_1080x1920.png
      08-matchmaking_1080x1920.png
      09-rules-popup_1080x1920.png
      10-generic-popup_1080x1920.png
      11-popup-states_1080x1920.png
      12-text-popup_1080x1920.png

  Use these as the layout spec: position and size everything against them.


02-Game-Ready-Assets/   (162 sprites in 12 folders)
--------------------------------------------------------------------
  00-Generic/  (30 sprites)
      badge_gold_pill_120x44_border22.png
      badge_green_pill_120x44_border22.png
      badge_red_pill_120x44_border22.png
      bar_gold_gradient_256x64.png
      bg_page_1080x1920.png
      btn_add_gold_48.png
      btn_gold_400x120_border32.png
      btn_gold_pressed_400x120_border32.png
      btn_grey_400x120_border32.png
      btn_grey_pressed_400x120_border32.png
      btn_outline_400x120_border32.png
      coin_gold_64.png
      coin_gold_star_64.png
      crown_gold_252.png
      diamond_gold_32.png
      divider_1px.png
      dot_red_22.png
      field_dark_920x92_border28.png
      gem_blue_68.png
      glow_gold_radial_512.png
      highlighter_gold_gradient_256x64.png
      icon_btn_bg_80.png
      icon_btn_raised_80.png
      panel_active_gold_984x400_border44.png
      panel_card_984x400_border40.png
      panel_inset_dark_400x120_border28.png
      panel_raised_984x272_border36.png
      pill_dark_240x72_border44.png
      rule_gold_fade_400x4.png
      topbar_bg_1080x136.png
  01-Main-Menu/  (9 sprites)
      bg_main_menu_1080x1920.png
      bottomnav_bg_1080x180.png
      btn_play_gold_944x192_border96.png
      btn_play_gold_pressed_944x192_border96.png
      chip_streak_400x68_border40.png
      nav_indicator_gold_56x6.png
      nav_tab_active_248x152_border36.png
      pill_currency_240x72_border36.png
      topbar_bg_1080x136.png
  02-Mode-Selection/  (12 sprites)
      badge_online_green_140x36_border8.png
      board_thumb_512x512.png
      btn_arrow_circle_512x512.png
      btn_view_rules_920x92_border24.png
      card_ruleset_gold_984x560_border44.png
      chip_difficulty_off_296x88_border22.png
      chip_difficulty_on_296x88_border22.png
      chip_meta_260x48_border24.png
      dot_active_gold_44x12_border6.png
      dot_idle_12x12_border6.png
      row_opponent_984x152_border36.png
      tile_opponent_icon_512x512_border145.png
  03-Gameplay-HUD/  (18 sprites)
      action_btn_128.png
      avatar_tile_92x92_border26.png
      bg_gameplay_1080x1920.png
      board_frame_992x992_border48.png
      board_full_8x8_992.png
      board_tile_dark_120.png
      board_tile_light_120.png
      btn_undo_circle_512x512.png
      highlight_move_ring_94.png
      highlight_square_120.png
      piece_black_94.png
      piece_black_king_94.png
      piece_red_94.png
      piece_red_king_94.png
      player_card_992x136_border32.png
      player_card_active_992x136_border32.png
      turn_pip_off_28.png
      turn_pip_on_28.png
  04-Result-Victory/  (16 sprites)
      avatar_frame_gold_180x180_border48.png
      avatar_frame_muted_180x180_border48.png
      bg_result_victory_1080x1920.png
      btn_icon_grey_120x120_border32.png
      btn_mainmenu_grey_760x120_border32.png
      btn_mainmenu_grey_pressed_760x120_border32.png
      btn_rematch_gold_904x140_border36.png
      btn_rematch_gold_pressed_904x140_border36.png
      icon_victory_crown_512x512.png
      ray_burst_gold_720.png
      reward_chip_blue_440x104_border32.png
      reward_chip_gold_440x104_border32.png
      rule_white_fade_left_104x4.png
      rule_white_fade_right_104x4.png
      stat_panel_904x352_border40.png
      text_victory_1024x260.png
  05-Result-Defeat/  (3 sprites)
      bg_result_defeat_1080x1920.png
      icon_bg_circle_512x512.png
      text_defeat_1024x260.png
  06-Result-Draw/  (3 sprites)
      bg_result_draw_1080x1920.png
      icon_bg_circle_512x512.png
      text_draw_1024x260.png
  07-Settings/  (11 sprites)
      btn_header_circle_512x512.png
      card_group_984x400_border40.png
      divider_row_920x2.png
      slider_fill_gold_800x16_border8.png
      slider_knob_256x256.png
      slider_track_800x16_border8.png
      toggle_knob_off_256x256.png
      toggle_knob_on_256x256.png
      toggle_track_off_464x256_border128.png
      toggle_track_on_464x256_border128.png
      topbar_bg_1080x136.png
  08-Matchmaking/  (12 sprites)
      avatar_frame_gold_216x216_border56.png
      avatar_slot_dashed_216x216_border56.png
      bg_matchmaking_1080x1920.png
      btn_cancel_outline_904x120_border32.png
      glow_blue_radial_512.png
      info_panel_904x268_border40.png
      progress_fill_gold_560x16_border8.png
      progress_track_560x16_border8.png
      pulse_dot_gold_16.png
      spinner_ring_gold_gradient_144.png
      timer_ring_gold_gradient_144.png
      timer_ring_track_144.png
  09-Popups/  (13 sprites)
      btn_gotit_gold_872x120_border32.png
      btn_no_grey_392x120_border32.png
      btn_yes_gold_392x120_border32.png
      close_btn_bg_80.png
      overlay_dim_1080x1920.png
      popup_frame_gold_912x520_border48.png
      popup_frame_grey_912x520_border48.png
      popup_frame_red_912x520_border48.png
      popup_frame_rules_976x724_border52.png
      rule_gold_1px.png
      text_popup_856x148_border40.png
      text_popup_error_856x148_border40.png
      text_popup_success_856x148_border40.png
  10-Game-Logo/  (4 sprites)
      logo_complete_1024x620.png
      logo_icon_512x512.png
      logo_name_checkers_1024x260.png
      logo_name_royalarena_1024x140.png
  11-Game-Icons/  (31 sprites)
      icon_back_512.png
      icon_chevron_left_512.png
      icon_chevron_right_512.png
      icon_confirm_512.png
      icon_defeat_512.png
      icon_draw_512.png
      icon_help_512.png
      icon_hint_512.png
      icon_home_512.png
      icon_language_512.png
      icon_logout_512.png
      icon_menu_512.png
      icon_missions_512.png
      icon_multiplayer_512.png
      icon_music_512.png
      icon_play_with_friends_512.png
      icon_player_512.png
      icon_privacy_512.png
      icon_profile_512.png
      icon_rules_512.png
      icon_settings_512.png
      icon_share_512.png
      icon_shop_512.png
      icon_sound_512.png
      icon_undo_512.png
      icon_vibration_512.png
      icon_vs_bot_512.png
      icon_vs_player_512.png
      player_icon_bg_512.png
      player_icon_border_gold_512.png
      player_icon_border_slate_512.png


UNITY IMPORT SETTINGS
--------------------------------------------------------------------
1. Texture Type      Sprite (2D and UI)
2. Filter Mode       Bilinear
3. Compression       None  (these are flat UI gradients - compression bands them)
4. Mip Maps          Off
5. Max Size          2048 or higher so nothing is downsampled on import

9-SLICE
--------------------------------------------------------------------
Any filename ending _borderNN is meant to stretch. In the Sprite Editor set
Border = NN on all four sides, then set the Image component to
Image Type = SLICED. Only the flat middle stretches; the rounded corners are
always drawn 1:1, so one sprite covers every width you need.
Sprites with no _borderNN suffix are fixed size - use Image Type = Simple.

ICONS
--------------------------------------------------------------------
11-Game-Icons/ are white 512x512 masters, used at 40-62px in the layouts.
Tint them per state with Image.color:
      gold     #D4AF37   active / selected
      light    #CBD5E1   default
      muted    #8494AC   inactive
Player avatar is three parts: player_icon_bg (navy fill),
player_icon_border_gold (active turn), player_icon_border_slate (idle).

PALETTE
--------------------------------------------------------------------
      Background        #0F172A
      Top bar           #1E293B
      Card / popup      #334155 .. #16223C
      Primary (gold)    #D4AF37      pressed  #B68A1F
      Secondary (grey)  #475569
      Board light       #EBD6B3      Board dark  #8B5A2B
      Red pieces        #D14343      Black pieces  #2A2A2A
      Primary text      #F8FAFC      Secondary text  #CBD5E1
      Success           #22C55E      Error  #EF4444

TYPE
--------------------------------------------------------------------
      Display / titles / buttons    Cinzel  (700-900)
      UI text / labels / body       Barlow  (400-800)
      Icons                         Material Symbols Rounded
Minimum on-screen text size in the 1080x1920 canvas: 24px.
