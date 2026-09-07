# Research question: original offline HUD/window asset mapping

The Unity port currently uses synthetic HUD/windows and disables UI.nx. The inspected HeavenClient working tree mixes v83/v87/v92/modern UI branches and contains debug offsets/fallbacks. Determine the active classic layout and drawing/origin rules for the available UI.nx, and distinguish source behavior from experimental comments. Need concrete HUD/button/quickslot and inventory/equipment/skill/shop geometry that can be implemented without online play. Preserve gameplay, learned skills, and save/load. Do not implement UI code.

Available asset: C:/HeavenClient/MapleStory-Client/nx/UI.nx (82,624,041 bytes). Main agent is extracting selected sprite metadata/contact sheets into Logs/ui-assets. Key Unity runtime: GameView/UI/{StatusBar,ExperienceBar,SkillBar,InventoryView,SkillMenu,LocalPlayMenu}.cs; GameData/NXDataManager.cs (UI load commented at Initialize).

## IO/UITypes/UIStatusBar.cpp:70-117
```cpp
		nl::node statusBar = nl::nx::UI["StatusBar.img"];
		nl::node base = statusBar["base"];
		nl::node gauge = statusBar["gauge"];
		
		if (!statusBar || !base || !gauge) {
			// StatusBar.img assets not found - create with defaults
			return;
		}
		
		// v92: EXP bar stretches across the bottom of the status bar
		exp_pos = Point<int16_t>(0, 55); // Position at bottom of status bar

		// v92: Load status bar background components
		// Add main background if found - v92 uses canvas type for textures
		nl::node background = base["backgrnd"];
		base_width = 0;
		if (background) {
			Texture base_tex(background);
			base_width = base_tex.width();
			sprites.emplace_back(background, DrawArgument(Point<int16_t>(0, 0)));
		}
		
		// Add backgrnd2 - secondary background element
		nl::node background2 = base["backgrnd2"];
		if (background2) {
			sprites.emplace_back(background2, DrawArgument(Point<int16_t>(1, 0)));
		}

		// Add gauge.bar - the bar texture that goes under graduation
		nl::node bar = gauge["bar"];
		if (bar) {
			sprites.emplace_back(bar, Point<int16_t>(217, 38)); // +1 y
		}
		// Add gauge.graduation - shows the empty bar outlines for HP/MP/XP
		nl::node graduation = gauge["graduation"];
		if (graduation) {
			sprites.emplace_back(graduation, DrawArgument(Point<int16_t>(217, 37))); // -1 y
		}
		
		// Add base.chat - chat interface element
		nl::node chat = base["chat"];
		if (chat) {
			sprites.emplace_back(chat, DrawArgument(Point<int16_t>(0, 0)));
		}

		// Load additional base textures
		if (base["box"])
			base_box = Texture(base["box"]);
```

## IO/UITypes/UIStatusBar.cpp:180-275
```cpp
		// Menu positioning - dynamically calculated based on resolution
		// Base position for 800x600, then offset for larger resolutions
		int16_t menu_base_x = VWIDTH - 118;  // 118px from right edge
		menu_pos = Point<int16_t>(menu_base_x, -280);
		setting_pos = menu_pos + Point<int16_t>(0, 168);
		community_pos = menu_pos + Point<int16_t>(-26, 196);
		character_pos = menu_pos + Point<int16_t>(-61, 168);
		event_pos = menu_pos + Point<int16_t>(-94, 252);

		// Quickslot position - always relative to right edge
		quickslot_pos = Point<int16_t>(VWIDTH - 200, 5);

		// EXP text position is now fixed relative to the centered status bar
		// No dynamic adjustment needed since status bar is centered

		// V92: No separate HP/MP background sprites needed
		// The base background is already drawn, gauges render directly over it
		// Leave hpmp_sprites empty for v92

		// V92: Gauge width for v92 should be based on the actual texture size, not stretching
		// Use a more reasonable width for v92 gauges - they should not stretch across the screen
		int16_t hpmp_max = 120; // V92: Use a consistent, reasonable width that matches texture design

		// Create HP/MP gauges with v92 compatibility
		// V92: Use correct dedicated gauge textures as identified by researcher

		// Verify textures exist before creating gauges
		// Use V87_FILL_RIGHT to clip from right side (shows missing HP/MP portion)
		if (gauge["hpFlash"] && gauge["hpFlash"]["0"]) {
			Texture hpTexture(gauge["hpFlash"]["0"]);
			hpbar = Gauge(Gauge::Type::V87_FILL_RIGHT, hpTexture, hpmp_max, 1.0f);
			// Also load animation for invincibility effect
			hpflash_anim = Animation(gauge["hpFlash"]);
		}

		if (gauge["mpFlash"] && gauge["mpFlash"]["0"]) {
			Texture mpTexture(gauge["mpFlash"]["0"]);
			mpbar = Gauge(Gauge::Type::V87_FILL_RIGHT, mpTexture, hpmp_max, 1.0f);
			// Also load animation for invincibility effect
			mpflash_anim = Animation(gauge["mpFlash"]);
		} 

		// Create character sets with v92 compatibility
		// V92: Use StatusBar number sprites for proper display
		nl::node numbers = statusBar["number"];

		if (numbers) {
			// Create charsets for HP/MP/EXP display using StatusBar number sprites
			statset = Charset(numbers, Charset::Alignment::LEFT);
			hpmpset = Charset(numbers, Charset::Alignment::LEFT);

			// Load numbers from StatusBar/number children
			numset = Charset(numbers, Charset::Alignment::LEFT);

			// Load special character textures directly from StatusBar/number
			// Brackets (green)
			if (numbers["Lbracket"])
				green_lbracket = Texture(numbers["Lbracket"]);
			if (numbers["Rbracket"])
				green_rbracket = Texture(numbers["Rbracket"]);

			// Slash and percent
			if (numbers["slash"])
				white_slash = Texture(numbers["slash"]);
			if (numbers["percent"])
				white_percent = Texture(numbers["percent"]);

			// Dot - may not exist, will just skip if not present
			if (numbers["dot"])
				white_dot = Texture(numbers["dot"]);
		} else {
			// Fallback to default charsets if no number textures exist
			statset = Charset();
			hpmpset = Charset();
			numset = Charset();
		}

		// Load level digit textures from Basic.img/LevelNo (has background)
		nl::node level_node = nl::nx::UI["Basic.img"]["LevelNo"];
		for (int i = 0; i < 10; i++) {
			level_digits[i] = Texture(level_node[std::to_string(i)]);
		}

		// Class name label (first line) and player name label (second line)
		// Using A12M font for smaller text, white color
		classlabel = OutlinedText(Text::Font::A12M, Text::Alignment::LEFT, Color::Name::WHITE, Color::Name::MINESHAFT);
		namelabel = OutlinedText(Text::Font::A12M, Text::Alignment::LEFT, Color::Name::WHITE, Color::Name::MINESHAFT);

		// Set quickslot textures with v87 compatibility
		// V87: Use empty textures to avoid wrong quickslot textures
		quickslot[0] = Texture();
		quickslot[1] = Texture();

		Point<int16_t> buttonPos = Point<int16_t>(591 + pos_adj, 73);

		if (VWIDTH == 1024)
```

## IO/UITypes/UIStatusBar.cpp:396-710
```cpp

		if (VWIDTH == 800)
		{
			fold += "800";
			extend += "800";
		}

		if (VWIDTH == 1366)
			quickslot_qs_adj = Point<int16_t>(213, 0);
		else
			quickslot_qs_adj = Point<int16_t>(211, 0);

		// V92: No quickslot buttons needed for v92 - removed quickslot button creation


#pragma region Menu
		// Set menu backgrounds with v92 compatibility

		// V92: Create empty textures for menu backgrounds
		menubackground[0] = Texture();
		menubackground[1] = Texture();
		menubackground[2] = Texture();

		// Create menu buttons based on version
		// Order: Shop, Trade, Menu, Shortcut - all at same y with 2 pixel spacing
		constexpr int16_t btn_y = 36;
		constexpr int16_t btn_spacing = 56;  // button width + 2 pixel gap
		int16_t btn_x = 572;  // Shop at (-6, 1) from original

		if (statusBar["BtShop"]) {
			buttons[Buttons::BT_CASHSHOP] = std::make_unique<MapleButton>(statusBar["BtShop"], Point<int16_t>(btn_x, btn_y));
			btn_x += btn_spacing;
		}
		if (statusBar["BtNPT"]) {
			buttons[Buttons::BT_OPTIONS] = std::make_unique<MapleButton>(statusBar["BtNPT"], Point<int16_t>(btn_x, btn_y));
			btn_x += btn_spacing;
		}
		if (statusBar["BtMenu"]) {
			buttons[Buttons::BT_MENU] = std::make_unique<MapleButton>(statusBar["BtMenu"], Point<int16_t>(btn_x, btn_y));
			btn_x += btn_spacing;
		}
		if (statusBar["BtShort"]) {
			buttons[Buttons::BT_FOLD_QS] = std::make_unique<MapleButton>(statusBar["BtShort"], Point<int16_t>(btn_x, btn_y));
		}
			
		if (statusBar["EquipKey"]) {
			buttons[Buttons::BT_CHARACTER_EQUIP] = std::make_unique<MapleButton>(statusBar["EquipKey"], Point<int16_t>(235, 3));
		}
		if (statusBar["InvenKey"]) {
			buttons[Buttons::BT_CHARACTER_ITEM] = std::make_unique<MapleButton>(statusBar["InvenKey"], Point<int16_t>(268, 3));
		}
		if (statusBar["StatKey"]) {
			buttons[Buttons::BT_CHARACTER_STAT] = std::make_unique<MapleButton>(statusBar["StatKey"], Point<int16_t>(301, 3));
		}
		if (statusBar["SkillKey"]) {
			buttons[Buttons::BT_CHARACTER_SKILL] = std::make_unique<MapleButton>(statusBar["SkillKey"], Point<int16_t>(334, 3));
		}

		// v92: Create empty textures for menu titles
		menutitle[0] = Texture();
		menutitle[1] = Texture();
		menutitle[2] = Texture();
		menutitle[3] = Texture();
		menutitle[4] = Texture();
#pragma endregion

		// V92: Status bar stretches across bottom of screen for all resolutions
		// Use dynamic height calculation based on resolution
		int16_t statusbar_height = (VWIDTH <= 1024) ? 75 : 80;

		// Center the status bar horizontally based on base texture width
		int16_t center_x = (VWIDTH - base_width) / 2;
		position = Point<int16_t>(center_x, VHEIGHT - statusbar_height + 9);
		position_x = position.x();
		position_y = position.y();
		dimension = Point<int16_t>(base_width, statusbar_height);
	}

	void UIStatusBar::draw(float alpha) const
	{
		UIElement::draw_sprites(alpha);

		// Draw all main buttons (including v92 hotkeys), except BT_EVENT which is drawn after base_box
		for (size_t i = 0; i <= Buttons::BT_SKILL; i++)
			if (i != Buttons::BT_EVENT && buttons.find(i) != buttons.end() && buttons.at(i))
				buttons.at(i)->draw(position);

		// V92: No HP/MP background sprites needed - they're part of the main background

		// Draw HP/MP flash gauges at fixed positions
		// Flash shows MISSING HP/MP - V87_FILL_RIGHT clips from left to show only missing portion
		// No position offset needed - just draw at fixed position and let clipping handle it
		if (hpbar.is_valid()) {
			hpbar.draw(DrawArgument(position + hpmp_pos, 1.0f));
		}
		if (mpbar.is_valid()) {
			mpbar.draw(DrawArgument(position + mp_pos + Point<int16_t>(111, 0), 1.0f));  // MP bar is 111px right of HP
		}
		if (expbar.is_valid())
			expbar.draw(position + exp_pos);

		int16_t level = stats.get_stat(MapleStat::Id::LEVEL);
		int16_t hp = stats.get_stat(MapleStat::Id::HP);
		int16_t mp = stats.get_stat(MapleStat::Id::MP);
		int32_t maxhp = stats.get_total(EquipStat::Id::HP);
		int32_t maxmp = stats.get_total(EquipStat::Id::MP);
		int64_t exp = stats.get_exp();

		// Format: [ currenthp/maxhp ] with green brackets
		// Draw HP: green [ + white hp + white / + white maxhp + green ]
		// HP numbers offset by (0, 1), brackets at base position
		Point<int16_t> hp_draw_pos = position + hpset_pos;
		Point<int16_t> hp_num_offset = Point<int16_t>(0, 1);  // HP text only offset
		int16_t hp_offset = 0;
		green_lbracket.draw(hp_draw_pos + Point<int16_t>(hp_offset, 0));
		hp_offset += green_lbracket.width() + 1;
		hp_offset += numset.draw(std::to_string(hp), hp_draw_pos + Point<int16_t>(hp_offset, 0) + hp_num_offset);
		white_slash.draw(hp_draw_pos + Point<int16_t>(hp_offset, 0) + hp_num_offset);
		hp_offset += white_slash.width() + 1;  // space after slash
		hp_offset += numset.draw(std::to_string(maxhp), hp_draw_pos + Point<int16_t>(hp_offset, 0) + hp_num_offset);
		hp_offset += 1;
		green_rbracket.draw(hp_draw_pos + Point<int16_t>(hp_offset, 0));

		// Format: [ currentMp/maxMp ] with green brackets
		// MP brackets offset by (0, -1), MP numbers offset by (1, 0)
		Point<int16_t> mp_draw_pos = position + mpset_pos;
		Point<int16_t> mp_bracket_offset = Point<int16_t>(0, -1);  // MP brackets -1 y
		Point<int16_t> mp_num_offset = Point<int16_t>(1, 0);  // MP text only offset
		int16_t mp_offset = 0;
		green_lbracket.draw(mp_draw_pos + Point<int16_t>(mp_offset, 0) + mp_bracket_offset);
		mp_offset += green_lbracket.width() + 1;
		mp_offset += numset.draw(std::to_string(mp), mp_draw_pos + Point<int16_t>(mp_offset, 0) + mp_num_offset);
		white_slash.draw(mp_draw_pos + Point<int16_t>(mp_offset, 0) + mp_num_offset);
		mp_offset += white_slash.width() + 1;  // space after slash
		mp_offset += numset.draw(std::to_string(maxmp), mp_draw_pos + Point<int16_t>(mp_offset, 0) + mp_num_offset);
		mp_offset += 1;
		green_rbracket.draw(mp_draw_pos + Point<int16_t>(mp_offset, 0) + mp_bracket_offset);

		// Format: currentXp[2.54%] with green brackets
		// Text parts offset by (0, 1), brackets at base position
		std::string expstring = std::to_string(100 * getexppercent());
		std::string exp_percent = expstring.substr(0, expstring.find('.') + 3); // e.g., "2.54"
		Point<int16_t> exp_draw_pos = position + statset_pos;
		Point<int16_t> exp_num_offset = Point<int16_t>(0, 1);  // text only offset
		int16_t exp_offset = 0;
		exp_offset += numset.draw(std::to_string(exp), exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
		exp_offset += 1;  // space before [
		green_lbracket.draw(exp_draw_pos + Point<int16_t>(exp_offset, 0));
		exp_offset += green_lbracket.width() + 1;  // space after [
		// Draw percentage - if dot texture exists, split and draw with dot; otherwise just draw digits
		size_t dot_pos = exp_percent.find('.');
		if (dot_pos != std::string::npos && white_dot.is_valid()) {
			// Draw digits before dot
			exp_offset += numset.draw(exp_percent.substr(0, dot_pos), exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
			// Draw dot with tight spacing
			exp_offset -= 1;  // less spacing before dot
			white_dot.draw(exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
			exp_offset += white_dot.width() - 1;  // less spacing after dot
			// Draw digits after dot
			exp_offset += numset.draw(exp_percent.substr(dot_pos + 1), exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
		} else if (dot_pos != std::string::npos) {
			// No dot texture - just show integer percentage
			exp_offset += numset.draw(exp_percent.substr(0, dot_pos), exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
		} else {
			exp_offset += numset.draw(exp_percent, exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
		}
		white_percent.draw(exp_draw_pos + Point<int16_t>(exp_offset, 0) + exp_num_offset);
		exp_offset += white_percent.width() + 1;  // space before ]
		green_rbracket.draw(exp_draw_pos + Point<int16_t>(exp_offset, 0));

		// Draw level using Basic.img/LevelNo digit textures (with background)
		std::string level_str = std::to_string(level);
		Point<int16_t> level_draw_pos = position + levelset_pos + Point<int16_t>(-15, 2);
		int16_t level_x_offset = 0;
		for (char c : level_str) {
			int digit = c - '0';
			if (digit >= 0 && digit <= 9) {
				level_digits[digit].draw(level_draw_pos + Point<int16_t>(level_x_offset, 0));
				level_x_offset += level_digits[digit].width();
			}
		}

		// Draw class name (first line) and player name (second line)
		classlabel.draw(position + namelabel_pos);
		namelabel.draw(position + namelabel_pos + Point<int16_t>(0, 13));  // 13px below class name (smaller font)

		// Draw hotkey box (base->box texture) at hotkey_box_pos
		if (base_box.is_valid()) {
			base_box.draw(DrawArgument(position + hotkey_box_pos));
		}

		// Draw BtClaim button ON TOP of base_box
		if (buttons.count(Buttons::BT_EVENT) && buttons.at(Buttons::BT_EVENT)) {
			buttons.at(Buttons::BT_EVENT)->draw(position);

			// Draw iconMemo to the right of BtClaim
			if (base_iconMemo.is_valid()) {
				Rectangle<int16_t> claim_bounds = buttons.at(Buttons::BT_EVENT)->bounds(position);
				Point<int16_t> memo_pos = Point<int16_t>(
					claim_bounds.right() + 4 - position.x(),  // 4px to the right of BtClaim
					hotkey_box_pos.y() + 5                     // 5px from top of box
				);
				base_iconMemo.draw(DrawArgument(position + memo_pos));
			}
		}

		// V92: Draw quickslot (only when active)
		// Get BtShort button bounds to position quickslot relative to it
		if (quickslot_active && base_quickSlot.is_valid() && buttons.count(Buttons::BT_FOLD_QS) && buttons.at(Buttons::BT_FOLD_QS)) {
			Rectangle<int16_t> btn_bounds = buttons.at(Buttons::BT_FOLD_QS)->bounds(position);

			// Quickslot starts 6px right of button, bottom aligned
			// Account for texture origin when calculating position
			Point<int16_t> qs_dims = base_quickSlot.get_dimensions();
			Point<int16_t> qs_origin = base_quickSlot.get_origin();
			Point<int16_t> qs_pos = Point<int16_t>(
				btn_bounds.right() + 6 - position.x() + qs_origin.x() + 0,
				btn_bounds.bottom() - qs_dims.y() - position.y() + qs_origin.y() + 3
			);

			// Draw quickslot background
			base_quickSlot.draw(DrawArgument(position + qs_pos));

			// Draw skill/item icons on quickslots FIRST (so they appear behind key labels)
			draw_quickslot_icons(qs_pos);

			// Draw key labels ON TOP: 4 columns x 2 rows, each slot is 35x35
			// Labels start at offset (10, 9) from quickslot top-left
			int16_t slot_size = 35;
			Point<int16_t> key_start = qs_pos + Point<int16_t>(10, 9);

			for (int i = 0; i < 8; i++) {
				if (quickslot_keys[i].is_valid()) {
					int row = i / 4;
					int col = i % 4;
					Point<int16_t> key_pos = key_start + Point<int16_t>(col * slot_size, row * slot_size);
					quickslot_keys[i].draw(DrawArgument(position + key_pos));
				}
			}
		}

#pragma region Menu
		Point<int16_t> pos_adj = Point<int16_t>(0, 0);

		if (quickslot_active)
		{
			if (VWIDTH == 800)
				pos_adj += Point<int16_t>(0, -73);
			else
				pos_adj += Point<int16_t>(0, -31);
		}

		Point<int16_t> pos;
		uint8_t button_count, menutitle_index;

		if (character_active)
		{
			pos = character_pos;
			button_count = 5;
			menutitle_index = 0;
		} 
		else if (community_active)
		{
			pos = community_pos;
			button_count = 4;
			menutitle_index = 1;
		}
		else if (event_active)
		{
			pos = event_pos;
			button_count = 2;
			menutitle_index = 2;
		}
		else if (menu_active)
		{
			pos = menu_pos;
			button_count = 11;
			menutitle_index = 3;
		}
		else if (setting_active)
		{
			pos = setting_pos;
			button_count = 5;
			menutitle_index = 4;
		}
		else
		{
			return;
		}

		Point<int16_t> mid_pos = Point<int16_t>(0, 29);

		uint16_t end_y = std::floor(28.2 * button_count);

		if (menu_active)
			end_y -= 1;

		uint16_t mid_y = end_y - mid_pos.y();

		menubackground[0].draw(position + pos + pos_adj);
		menubackground[1].draw(DrawArgument(position + pos + pos_adj) + DrawArgument(mid_pos, Point<int16_t>(0, mid_y)));
		menubackground[2].draw(position + pos + pos_adj + Point<int16_t>(0, end_y));

		menutitle[menutitle_index].draw(position + pos + pos_adj);

		for (size_t i = Buttons::BT_MENU_QUEST; i <= Buttons::BT_EVENT_DAILY; i++)
			if (buttons.find(i) != buttons.end() && buttons.at(i))
				buttons.at(i)->draw(position);
#pragma endregion
	}

	void UIStatusBar::update()
	{
		UIElement::update();

```

## IO/UITypes/UIItemInventory.cpp:47-143
```cpp
		nl::node Item;
		if (is_v83) {
			// V83/V87 structure
			Item = nl::nx::UI["UIWindow.img"]["Item"];
			LOG(LOG_DEBUG, "[UIItemInventory] Using v83/v87 UI structure");
		} else if (is_v92) {
			// V92 structure - uses UIWindow.img but has tabs
			Item = nl::nx::UI["UIWindow.img"]["Item"];
			LOG(LOG_DEBUG, "[UIItemInventory] Using v92 UI structure");
		} else {
			// Modern structure
			Item = nl::nx::UI["UIWindow2.img"]["Item"];
			LOG(LOG_DEBUG, "[UIItemInventory] Using modern UI structure");
		}
		
		if (Item.name().empty()) {
			LOG(LOG_ERROR, "[UIItemInventory] Item node not found!");
		}
		
		// Debug: List all child nodes of Item
		LOG(LOG_DEBUG, "[UIItemInventory] Available Item child nodes:");
		for (auto child : Item) {
			LOG(LOG_DEBUG, "  - " << child.name() << " (type: " << (int)child.data_type() << ")");
		}
		
		// Handle position data based on version
		// Note: slot_space = icon_size + padding between slots
		// The padding is typically 3-4 pixels between item slots
		constexpr int16_t SLOT_PADDING = 4;  // Padding between slots (adjustable)

		if (is_v83) {
			// V83/V87 defaults - no position data in NX files
			LOG(LOG_DEBUG, "[UIItemInventory] Using v83/v87 defaults - no pos data in NX");
			slot_col = 4;
			slot_pos = Point<int16_t>(11, 51); // Position for first slot in v87
			slot_row = 6;
			// Will be recalculated below based on actual icon dimensions
			slot_space_x = 36;
			slot_space_y = 35;
		} else if (is_v92) {
			// V92 uses same defaults as v83/v87 but has tabs
			LOG(LOG_DEBUG, "[UIItemInventory] Using v92 defaults - similar to v87 but with tabs");
			slot_col = 4;
			slot_pos = Point<int16_t>(11, 51);
			slot_row = 6;
			// Will be recalculated below based on actual icon dimensions
			slot_space_x = 36;
			slot_space_y = 35;
		} else {
			// Modern MapleStory layout
			nl::node pos = Item["pos"];
			slot_col = pos["slot_col"];
			slot_pos = pos["slot_pos"];
			slot_row = pos["slot_row"];
			slot_space_x = pos["slot_space_x"];
			slot_space_y = pos["slot_space_y"];
		}
		
		// Validate slot_pos
		if (slot_pos.x() == 0 && slot_pos.y() == 0) {
			LOG(LOG_DEBUG, "[UIItemInventory] slot_pos is (0,0), using default");
			slot_pos = Point<int16_t>(11, 51); // Default position for v87
		}
		LOG(LOG_DEBUG, "[UIItemInventory] slot_pos=" << slot_pos.x() << "," << slot_pos.y());

		// Initialize with default values
		max_slots = 24;
		max_full_slots = 96;

		// These nodes don't exist in v83/v87, and may not exist in v92
		nl::node AutoBuild = (is_v83 || is_v92) ? nl::node() : Item["AutoBuild"];
		nl::node FullAutoBuild = (is_v83 || is_v92) ? nl::node() : Item["FullAutoBuild"];

		// Load backgrounds - simplified to use only main background
		nl::node backgrnd_node = Item["backgrnd"];
		if (backgrnd_node && backgrnd_node.data_type() == nl::node::type::bitmap) {
			backgrnd = backgrnd_node;
			LOG(LOG_DEBUG, "[UIItemInventory] Loaded backgrnd");
		} else {
			// Fallback for modern versions that might use productionBackgrnd
			backgrnd_node = Item["productionBackgrnd"];
			if (backgrnd_node && backgrnd_node.data_type() == nl::node::type::bitmap) {
				backgrnd = backgrnd_node;
				LOG(LOG_DEBUG, "[UIItemInventory] Loaded productionBackgrnd as fallback");
			} else {
				LOG(LOG_ERROR, "[UIItemInventory] No background found!");
				backgrnd = Texture();
			}
		}
		
		// Don't load backgrnd2/3 - not needed
		backgrnd2 = Texture();
		backgrnd3 = Texture();
		
		// Load full background if available
		nl::node full_bg_node = Item["FullBackgrnd"];
		if (full_bg_node && full_bg_node.data_type() == nl::node::type::bitmap) {
```

## IO/UITypes/UIItemInventory.cpp:145-235
```cpp
			LOG(LOG_DEBUG, "[UIItemInventory] Loaded FullBackgrnd");
		} else {
			// Use regular background as fallback for full view
			full_backgrnd = backgrnd;
			LOG(LOG_DEBUG, "[UIItemInventory] Using backgrnd as full_backgrnd fallback");
		}
		
		// Don't load full_backgrnd2/3 - not needed
		full_backgrnd2 = Texture();
		full_backgrnd3 = Texture();

		bg_dimensions = backgrnd.get_dimensions();
		bg_full_dimensions = full_backgrnd.get_dimensions();

		// V83/V87 doesn't have these assets, v92 might have some
		if (is_v83) {
			// Use empty textures for v83/v87
			newitemslot = Animation();
			newitemtabdis = Animation();
			newitemtaben = Animation();
			projectile = Texture();
			disabled = Texture();
		} else if (is_v92) {
			// V92 has basic assets like modern versions
			nl::node New = Item["New"];
			if (New) {
				newitemslot = New["inventory"];
				newitemtabdis = New["Tab0"];
				newitemtaben = New["Tab1"];
			} else {
				newitemslot = Animation();
				newitemtabdis = Animation();
				newitemtaben = Animation();
			}
			projectile = Item["activeIcon"];
			disabled = Item["disabled"];
		} else {
			// Modern versions have all assets
			nl::node New = Item["New"];
			newitemslot = New["inventory"];
			newitemtabdis = New["Tab0"];
			newitemtaben = New["Tab1"];
			projectile = Item["activeIcon"];
			disabled = Item["disabled"];
		}

		Point<int16_t> icon_dimensions = disabled.get_dimensions();
		icon_width = icon_dimensions.x();
		icon_height = icon_dimensions.y();

		// Adaptive slot spacing: recalculate based on actual icon dimensions if available
		// This fixes misalignment when v92 assets have different icon sizes than expected
		if ((is_v83 || is_v92) && icon_width > 0 && icon_height > 0) {
			// Recalculate slot spacing based on actual icon dimensions + padding (uses SLOT_PADDING from above)
			slot_space_x = icon_width + SLOT_PADDING;
			slot_space_y = icon_height + SLOT_PADDING - 1;  // Slight vertical adjustment
			LOG(LOG_DEBUG, "[UIItemInventory] Adaptive spacing: icon(" << icon_width << "x" << icon_height
				<< ") -> space(" << slot_space_x << "x" << slot_space_y << ")");
		}

		// Handle tabs based on version
		if (is_v92) {
			// V92 has 5 tabs (0-4) in UIWindow.img
			nl::node Tab = Item["Tab"];
			nl::node taben = Tab["enabled"];
			nl::node tabdis = Tab["disabled"];

			// v92: Since origins are (0,0), calculate positions manually
			// Tabs should be positioned horizontally across the top of the inventory
			// Base position for first tab, then space them out according to actual tab widths
			Point<int16_t> tab_base = Point<int16_t>(9, 26);
			
			// Calculate positions with proper spacing to prevent overlap
			// Tab widths from JSON: 22, 13, 26, 13, 20 - add 2px padding between tabs
			Point<int16_t> tab_pos0 = tab_base;                                  // Equip: x=9
			Point<int16_t> tab_pos1 = tab_base + Point<int16_t>(26, 0);         // Use: x=35 (22+4 spacing)
			Point<int16_t> tab_pos2 = tab_base + Point<int16_t>(43, 0);         // ETC: x=52 (26+13+4 spacing)
			Point<int16_t> tab_pos3 = tab_base + Point<int16_t>(73, 0);         // Setup: x=82 (43+26+4 spacing)  
			Point<int16_t> tab_pos4 = tab_base + Point<int16_t>(90, 0);         // Cash: x=99 (73+13+4 spacing)
			
			// Create tab buttons only if textures are valid
			if (tabdis["0"] && taben["0"]) {
				buttons[Buttons::BT_TAB_EQUIP] = std::make_unique<TwoSpriteButton>(tabdis["0"], taben["0"], tab_pos0);
			}
			if (tabdis["1"] && taben["1"]) {
				buttons[Buttons::BT_TAB_USE] = std::make_unique<TwoSpriteButton>(tabdis["1"], taben["1"], tab_pos1);
			}
			if (tabdis["2"] && taben["2"]) {
				buttons[Buttons::BT_TAB_ETC] = std::make_unique<TwoSpriteButton>(tabdis["2"], taben["2"], tab_pos2);
			}
			if (tabdis["3"] && taben["3"]) {
```

## IO/UITypes/UISkillBook.cpp:117-150
```cpp
	UISkillBook::UISkillBook(const CharStats& in_stats, const SkillBook& in_skillbook) : UIDragElement<PosSKILL>(), stats(in_stats), skillbook(in_skillbook), grabbing(false), tab(0), macro_enabled(false), sp_enabled(false), visible_rows(ROWS)
	{
		
		// Use UIWindow.img/Skill structure
		nl::node Skill = nl::nx::UI["UIWindow.img"]["Skill"];
		
		if (!Skill) {
			// If no Skills window assets found, create minimal window
			return;
		}
		
		
		// Load main background
		nl::node ui_backgrnd = Skill["backgrnd"];
		if (ui_backgrnd) {
			bg_dimensions = Texture(ui_backgrnd).get_dimensions();
		} else {
			bg_dimensions = Point<int16_t>(400, 300); // Default size
		}

		// Load skill display elements
		skilld = Skill["skill0"];
		skille = Skill["skill1"];
		
		nl::node skillBlankNode = Skill["skillBlank"];
		if (skillBlankNode) {
			skillb = skillBlankNode;
		} else {
			skillb = Skill["skill0"]; // Fallback if skillBlank doesn't exist
		}
		
		// Validate the texture by checking if the node exists
		if (!skillBlankNode && !Skill["skill0"]) {
		}
```

## IO/UITypes/UISkillBook.cpp:200-274
```cpp
		}

		sp_used = Text(Text::Font::A12B, Text::Alignment::RIGHT, Color::Name::WHITE);
		sp_remaining = Text(Text::Font::A12B, Text::Alignment::LEFT, Color::Name::SUPERNOVA);
		sp_name = Text(Text::Font::A12B, Text::Alignment::CENTER, Color::Name::WHITE);

		// Add background sprite
		if (ui_backgrnd) sprites.emplace_back(ui_backgrnd, Point<int16_t>(1, 0));

		nl::node macro = Skill["macro"];

		macro_backgrnd = macro["backgrnd"];
		macro_backgrnd2 = macro["backgrnd2"];
		macro_backgrnd3 = macro["backgrnd3"];

		if (macro["BtOK"]) {
			buttons[Buttons::BT_MACRO_OK] = std::make_unique<MapleButton>(macro["BtOK"], Point<int16_t>(bg_dimensions.x(), 0));
			buttons[Buttons::BT_MACRO_OK]->set_state(Button::State::DISABLED);
		}

		// Close button - try window-specific first, then fallback
		nl::node close = Skill["BtClose"];
		if (!close) close = nl::nx::UI["Basic.img"]["BtClose3"];
		if (close) {
			buttons[Buttons::BT_CLOSE] = std::make_unique<MapleButton>(close, Point<int16_t>(bg_dimensions.x() - 18, 5));
		}

		nl::node Tab = Skill["Tab"];
		if (Tab) {
			nl::node enabled = Tab["enabled"];
			nl::node disabled = Tab["disabled"];

			for (uint16_t i = Buttons::BT_TAB0; i <= Buttons::BT_TAB4; ++i)
			{
				uint16_t tabid = i - Buttons::BT_TAB0;
				if (disabled[tabid] && enabled[tabid]) {
					buttons[i] = std::make_unique<TwoSpriteButton>(disabled[tabid], enabled[tabid]);
				}
			}
		} else {
			// If no Tab structure exists, create invisible placeholder buttons
			// This prevents crashes when change_tab is called
			for (uint16_t i = Buttons::BT_TAB0; i <= Buttons::BT_TAB4; ++i)
			{
				// Create buttons that do nothing but prevent null pointer access
				buttons[i] = nullptr;
			}
		}

		uint16_t y_adj = 0;

		// Check if we should use single column layout based on background width
		bool use_single_column = (bg_dimensions.x() < 250);

		for (uint16_t i = Buttons::BT_SPUP0; i <= Buttons::BT_SPUP11; ++i)
		{
			uint16_t x_adj = 0;
			uint16_t spupid = i - Buttons::BT_SPUP0;

			// Only use second column if window is wide enough
			if (!use_single_column && spupid % 2)
				x_adj = ROW_WIDTH;

			Point<int16_t> spup_position = SKILL_OFFSET + Point<int16_t>(124 + x_adj, 20 + y_adj);
			nl::node btSpUp = Skill["BtSpUp"];
			if (btSpUp) {
				buttons[i] = std::make_unique<MapleButton>(btSpUp, spup_position);
				// Initially set all skill up buttons as inactive
				buttons[i]->set_active(false);
			}

			// Update y position
			if (use_single_column) {
				// Single column: always move down
				y_adj += ROW_HEIGHT;
```

## IO/UITypes/UIEquipInventory.cpp:35-145
```cpp
	const int16_t StartX = 5;
	const int16_t StartY = 34;
	const int16_t SlotSize = 33;
}

namespace ms
{
	UIEquipInventory::UIEquipInventory(const Inventory& invent) : UIDragElement<PosEQINV>(), inventory(invent), tab(Buttons::BT_TAB1), hasPendantSlot(false), hasPocketSlot(false), pet_active(true)
	{
		// Grid Configuration
		

		// Coordinate calculation helper
		auto getPos = [] (int col, int row) {
			return Point<int16_t>(
				EquipGrid::StartX + (col * EquipGrid::SlotSize),
				EquipGrid::StartY + (row * EquipGrid::SlotSize)
			);
			};

		// Row 0
		iconpositions[EquipSlot::Id::HAT] = getPos(1, 0); // CAP

		// Row 1
		iconpositions[EquipSlot::Id::MEDAL] = getPos(0, 1); // MEDAL
		iconpositions[EquipSlot::Id::FACE] = getPos(1, 1); // FOREHEAD
		iconpositions[EquipSlot::Id::RING1] = getPos(3, 1); // RING
		iconpositions[EquipSlot::Id::RING2] = getPos(4, 1); // RING

		// Row 2
		iconpositions[EquipSlot::Id::EYEACC] = getPos(2, 2); // EYE ACC
		iconpositions[EquipSlot::Id::EARACC] = getPos(3, 2); // EAR ACC
		iconpositions[EquipSlot::Id::SHOULDER] = getPos(4, 2); // SHOULDER

		// Row 3
		iconpositions[EquipSlot::Id::CAPE] = getPos(0, 3); // MANTLE
		iconpositions[EquipSlot::Id::TOP] = getPos(1, 3); // CLOTHES
		iconpositions[EquipSlot::Id::PENDANT1] = getPos(2, 3); // PENDANT
		iconpositions[EquipSlot::Id::WEAPON] = getPos(3, 3); // WEAPON
		iconpositions[EquipSlot::Id::SUBWEAPON] = getPos(4, 3); // WEAPON (Sub)

		// Row 4
		iconpositions[EquipSlot::Id::GLOVES] = getPos(0, 4); // GLOVES
		iconpositions[EquipSlot::Id::BOTTOM] = getPos(1, 4); // PANTS
		iconpositions[EquipSlot::Id::BELT] = getPos(2, 4); // BELT
		iconpositions[EquipSlot::Id::RING3] = getPos(3, 4); // RING
		iconpositions[EquipSlot::Id::RING4] = getPos(4, 4); // RING

		// Row 5
		iconpositions[EquipSlot::Id::SHOES] = getPos(2, 5); // SHOES

		// Row 6
		iconpositions[EquipSlot::Id::TAMEDMOB] = getPos(0, 6); // TAMING MOB
		//iconpositions[EquipSlot::Id::PETMP] = getPos(4, 5); // PET MP
		//iconpositions[EquipSlot::Id::MOBEQUIP] = getPos(2, 6); // MOB EQUIP
		//iconpositions[EquipSlot::Id::PETHP] = getPos(4, 6); // PET HP

		// Row 7
		iconpositions[EquipSlot::Id::SADDLE] = getPos(1, 7); // SADDLE

		tab_source[Buttons::BT_TAB0] = "Equip";
		tab_source[Buttons::BT_TAB1] = "Cash";
		tab_source[Buttons::BT_TAB2] = "Pet";
		tab_source[Buttons::BT_TAB3] = "Android";

		// Equip inventory is at UIWindow.img/Equip
		nl::node Equip = nl::nx::UI["UIWindow.img"]["Equip"];

		// Load background from Equip node (backgrnd is 175x291)
		nl::node main_bg = Equip["backgrnd"];

		// Get dimensions from background texture
		Point<int16_t> bg_dimensions = Point<int16_t>(175, 291);
		if (main_bg) {
			Texture bg_tex(main_bg);
			if (bg_tex.is_valid()) {
				bg_dimensions = bg_tex.get_dimensions();
			}
			// Add background to sprites - this is how other UI windows work
			sprites.emplace_back(main_bg, Point<int16_t>(0, 0));
		}

		totem_dimensions = bg_dimensions;
		totem_adj = Point<int16_t>(0, 0);

		nl::node close_node = Equip["BtClose"];

		// Close button using BtDetail texture at top right
		buttons[Buttons::BT_CLOSE] = std::make_unique<MapleButton>(close_node, Point<int16_t>(bg_dimensions.x() - 18, 5));

		// Load pet window texture - try with ["0"] child
		pet_window = Texture(Equip["pet"]["0"]);

		// BtDetail button at bottom right of equip window (opens pet equipment)
		buttons[Buttons::BT_DETAIL] = std::make_unique<MapleButton>(Equip["BtDetail"], Point<int16_t>(bg_dimensions.x() - 60, bg_dimensions.y() - 24));

		dimension = bg_dimensions;
		dragarea = Point<int16_t>(bg_dimensions.x(), 20);

		load_icons();
		// V92: No tabs, just show equip tab
		tab = Buttons::BT_TAB0;
	}

	void UIEquipInventory::draw(float alpha) const
	{
		// Draw sprites (background) and buttons
		UIElement::draw(alpha);

		// Draw equipped items on the equip tab
		for (auto iter : icons)
```

## IO/UITypes/UIShop.cpp:40-144
```cpp
	{
		// v92 compatibility: Try UIWindow.img first, then UIWindow2.img
		nl::node src = nl::nx::UI["UIWindow.img"]["Shop"];
		if (!src) {
			src = nl::nx::UI["UIWindow2.img"]["Shop2"];
		}
		
		if (!src) {
			// No shop assets found, create minimal UI
			dimension = Point<int16_t>(500, 400);
			dragarea = Point<int16_t>(500, 20);
			return;
		}

		nl::node background = src["backgrnd"];
		Texture bg = background;

		auto bg_dimensions = bg.get_dimensions();

		// Only load main background for v92
		sprites.emplace_back(background);
		// Skip additional backgrounds for simplicity
		// sprites.emplace_back(src["backgrnd2"]);
		// sprites.emplace_back(src["backgrnd3"]);
		// sprites.emplace_back(src["backgrnd4"]);

		// Load buttons with null checks
		if (src["BtBuy"]) buttons[Buttons::BUY_ITEM] = std::make_unique<MapleButton>(src["BtBuy"]);
		if (src["BtSell"]) buttons[Buttons::SELL_ITEM] = std::make_unique<MapleButton>(src["BtSell"]);
		if (src["BtExit"]) buttons[Buttons::EXIT] = std::make_unique<MapleButton>(src["BtExit"]);

		// Load checkbox if available
		nl::node checkbox_node = src["checkBox"];
		if (checkbox_node && checkbox_node[0] && checkbox_node[1]) {
			Texture cben = checkbox_node[0];
			Texture cbdis = checkbox_node[1];

			Point<int16_t> cb_origin = cben.get_origin();
			int16_t cb_x = cb_origin.x();
			int16_t cb_y = cb_origin.y();

			checkBox[0] = cbdis;
			checkBox[1] = cben;

			buttons[Buttons::CHECKBOX] = std::make_unique<AreaButton>(Point<int16_t>(std::abs(cb_x), std::abs(cb_y)), cben.get_dimensions());
		}

		// Load buy tab if available
		nl::node tabbuy = src["TabBuy"];
		if (tabbuy && tabbuy["enabled"] && tabbuy["disabled"]) {
			nl::node buyen = tabbuy["enabled"];
			nl::node buydis = tabbuy["disabled"];
			
			if (buydis[0] && buyen[0]) {
				buttons[Buttons::OVERALL] = std::make_unique<TwoSpriteButton>(buydis[0], buyen[0]);
			}
		}

		// Load sell tabs if available
		nl::node tabsell = src["TabSell"];
		if (tabsell && tabsell["enabled"] && tabsell["disabled"]) {
			nl::node sellen = tabsell["enabled"];
			nl::node selldis = tabsell["disabled"];

			for (uint16_t i = Buttons::EQUIP; i <= Buttons::CASH; i++)
			{
				std::string tabnum = std::to_string(i - Buttons::EQUIP);
				if (selldis[tabnum] && sellen[tabnum]) {
					buttons[i] = std::make_unique<TwoSpriteButton>(selldis[tabnum], sellen[tabnum]);
				}
			}
		}

		int16_t item_y = 124;
		int16_t item_height = 36;

		buy_x = 8;
		buy_width = 257;

		for (uint16_t i = Buttons::BUY0; i <= Buttons::BUY8; i++)
		{
			Point<int16_t> pos(buy_x, item_y + 42 * (i - Buttons::BUY0));
			Point<int16_t> dim(buy_width, item_height);
			buttons[i] = std::make_unique<AreaButton>(pos, dim);
		}

		sell_x = 284;
		sell_width = 200;

		for (uint16_t i = Buttons::SELL0; i <= Buttons::SELL8; i++)
		{
			Point<int16_t> pos(sell_x, item_y + 42 * (i - Buttons::SELL0));
			Point<int16_t> dim(sell_width, item_height);
			buttons[i] = std::make_unique<AreaButton>(pos, dim);
		}

		buy_selection = src["select"];
		sell_selection = src["select2"];
		meso = src["meso"];

		mesolabel = Text(Text::Font::A11M, Text::Alignment::RIGHT, Color::Name::MINESHAFT);

		// Use simpler slider type for v92 compatibility
		buyslider = Slider(
			Slider::Type::LINE_CYAN, Range<int16_t>(123, 484), 257, 5, 1,
```

## IO/Components/Gauge.cpp:1-230
```cpp
//////////////////////////////////////////////////////////////////////////////////
//	This file is part of the continued Journey MMORPG client					//
//	Copyright (C) 2015-2019  Daniel Allendorf, Ryan Payton						//
//																				//
//	This program is free software: you can redistribute it and/or modify		//
//	it under the terms of the GNU Affero General Public License as published by	//
//	the Free Software Foundation, either version 3 of the License, or			//
//	(at your option) any later version.											//
//																				//
//	This program is distributed in the hope that it will be useful,				//
//	but WITHOUT ANY WARRANTY; without even the implied warranty of				//
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the				//
//	GNU Affero General Public License for more details.							//
//																				//
//	You should have received a copy of the GNU Affero General Public License	//
//	along with this program.  If not, see <https://www.gnu.org/licenses/>.		//
//////////////////////////////////////////////////////////////////////////////////
#include "Gauge.h"

namespace ms
{
	Gauge::Gauge(Type type, Texture front, int16_t maximum, float percentage) : Gauge(type, front, {}, maximum, percentage) {}
	Gauge::Gauge(Type type, Texture front, Texture middle, int16_t maximum, float percentage) : Gauge(type, front, {}, {}, maximum, percentage) {}
	Gauge::Gauge(Type type, Texture front, Texture middle, Texture end, int16_t maximum, float percentage) : type(type), barfront(front), barmid(middle), barend(end), maximum(maximum), percentage(percentage), target(percentage) {}

	void Gauge::draw(const DrawArgument& args) const
	{
		int16_t length = static_cast<int16_t>(percentage * maximum);

		if (length > 0)
		{
			if (type == Type::DEFAULT)
			{
				barfront.draw(args + DrawArgument(Point<int16_t>(0, 0), Point<int16_t>(length, 0)));
				barmid.draw(args);
				barend.draw(args + Point<int16_t>(length + 8, 20));
			}
			else if (type == Type::CASHSHOP)
			{
				Point<int16_t> pos_adj = Point<int16_t>(45, 1);

				barfront.draw(args - pos_adj);
				barmid.draw(args + DrawArgument(Point<int16_t>(0, 0), Point<int16_t>(length, 0)));
				barend.draw(args - pos_adj + Point<int16_t>(length + barfront.width(), 0));
			}
			else if (type == Type::WORLDSELECT)
			{
				barfront.draw(args, {}, Range<int16_t>(0, barfront.width() - length));
			}
			else if (type == Type::V87_FILL)
			{
				// V87: Draw texture clipped to the fill length, no stretching
				// Use Range to clip the texture to the desired width
				if (length <= barfront.width())
				{
					// Normal case: clip texture to show only the filled portion
					// Ensure full opacity by adding 1.0f to the draw arguments
					barfront.draw(args + 1.0f, {}, Range<int16_t>(0, length));
				}
				else
				{
					// Edge case: if length exceeds texture width, tile the texture
					int16_t remaining = length;
					int16_t offset = 0;
					while (remaining > 0)
					{
						int16_t tile_width = (remaining >= barfront.width()) ? barfront.width() : remaining;
						barfront.draw(args + Point<int16_t>(offset, 0) + 1.0f, {}, Range<int16_t>(0, tile_width));
						offset += tile_width;
						remaining -= tile_width;
					}
				}
			}
			else if (type == Type::V87_FILL_REVERSE)
			{
				// V87_FILL_REVERSE: Draw texture horizontally flipped
				// The flipped texture fills normally from left to right, but appears to fill right to left
				if (length <= barfront.width())
				{
					// Create a flipped draw argument by using the bool constructor
					// DrawArgument(position, flip) where flip = true for horizontal flip
					Point<int16_t> pos = args.getpos();
					DrawArgument flipped_args(pos, true);

					// Now draw with clipping to show only the filled portion
					barfront.draw(flipped_args, {}, Range<int16_t>(0, length));
				}
				else
				{
					// Edge case: if length exceeds texture width, just draw full texture flipped
					Point<int16_t> pos = args.getpos();
					DrawArgument flipped_args(pos, true);
					barfront.draw(flipped_args);
				}
			}
			else if (type == Type::V87_FILL_RIGHT)
			{
				// V87_FILL_RIGHT: Clip from the right side of the texture
				// Range is (left_crop, right_crop) - crop left side to show only right portion
				int16_t tex_width = barfront.width();
				int16_t clip_length = static_cast<int16_t>(percentage * tex_width);
				if (clip_length > 0 && clip_length <= tex_width)
				{
					int16_t left_crop = tex_width - clip_length;
					// Crop left_crop pixels from left, 0 from right - use full opacity
					barfront.draw(args, {}, Range<int16_t>(left_crop, 0));
				}
				// Don't draw anything if clip_length is 0 (nothing missing)
			}
		}
		else
		{
			if (type == Type::WORLDSELECT)
				barfront.draw(args, {}, Range<int16_t>(0, barfront.width() - 1));
			// V87_FILL: Draw nothing when length is 0 (empty gauge)
		}
	}

	void Gauge::update(float t)
	{
		if (target != t)
		{
			target = t;
			step = (target - percentage) / 24;
		}

		if (percentage != target)
		{
			percentage += step;

			if (step < 0.0f)
			{
				if (target - percentage >= step)
					percentage = target;
			}
			else if (step > 0.0f)
			{
				if (target - percentage <= step)
					percentage = target;
			}
		}
	}

	bool Gauge::is_valid() const
	{
		// A gauge is valid if it has a valid front texture and a positive maximum
		return barfront.is_valid() && maximum > 0;
	}
}
```

## Util/V83UIAssets.h:1-100
```cpp
//////////////////////////////////////////////////////////////////////////////////
//	This file is part of the continued Journey MMORPG client					//
//	Copyright (C) 2015-2019  Daniel Allendorf, Ryan Payton						//
//																				//
//	This program is free software: you can redistribute it and/or modify		//
//	it under the terms of the GNU Affero General Public License as published by	//
//	the Free Software Foundation, either version 3 of the License, or			//
//	(at your option) any later version.											//
//																				//
//	This program is distributed in the hope that it will be useful,				//
//	but WITHOUT ANY WARRANTY; without even the implied warranty of				//
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the				//
//	GNU Affero General Public License for more details.						//
//																				//
//	You should have received a copy of the GNU Affero General Public License	//
//	along with this program.  If not, see <https://www.gnu.org/licenses/>.		//
//////////////////////////////////////////////////////////////////////////////////
#pragma once

#include <nlnx/node.hpp>
#include <nlnx/nx.hpp>
#include "../Graphics/Texture.h"
#include "../Constants.h"
#include "../MapleStory.h"
#include <iostream>
#include <vector>
#include <string>

namespace ms
{
	// Centralized v83 UI asset compatibility layer
	class V83UIAssets
	{
	public:
		// Check if we're using v83/v87 assets
		static bool isV83Mode()
		{
			// Check for the existence of UIWindow2.img to determine version
			// v83/v87 uses UIWindow.img, newer versions use UIWindow2.img
			return !nl::nx::UI["UIWindow2.img"];
		}
		
		// Check if we're using v92 assets
		static bool isV92Mode()
		{
			// v92 has Title section with buttons, v83 doesn't have Title section
			nl::node login = nl::nx::UI["Login.img"];
			if (!login) {
				return false;
			}
			
			nl::node title = login["Title"];
			if (!title) {
				return false;
			}
			
			// Check if Title section has any children (buttons)
			if (title.size() > 0) {
				return true;
			}
			
			// Alternative check: look for specific v92 buttons in Title
			// Sometimes the node exists but reports size 0, so check specific children
			if (title["BtLogin"] || title["BtQuit"] || title["BtHomePage"] || 
			    title["BtPasswdLost"] || title["BtEmailLost"] || title["BtEmailSave"]) {
				return true;
			}
			
			return false;
		}

		// === Helper function to resolve paths ===
		static nl::node resolvePath(const std::string& path)
		{
			std::vector<std::string> parts;
			std::string current;
			for (char c : path) {
				if (c == '/') {
					if (!current.empty()) {
						parts.push_back(current);
						current.clear();
					}
				} else {
					current += c;
				}
			}
			if (!current.empty()) {
				parts.push_back(current);
			}
			
			if (parts.empty()) return nl::node();
			
			nl::node node;
			if (parts[0] == "UI") {
				node = nl::nx::UI;
			} else if (parts[0] == "Map") {
				node = nl::nx::Map;
			} else if (parts[0] == "Map001") {
				node = nl::nx::Map001;
			} else {
```
