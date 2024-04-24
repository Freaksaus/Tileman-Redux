﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace TilemanRedux;

public class ModEntry : Mod
{
	private const string SAVE_CONFIG_KEY = "tilemanredux-config";

	private bool do_loop = true;
	private bool do_collision = true;
	private readonly bool allow_player_placement = false;
	private bool toggle_overlay = true;
	private bool tool_button_pushed = false;
	private bool location_changed = false;

	private float tile_price = 1.0f;
	private float tile_price_raise = 0.0008f;
	private float dynamic_tile_price;

	private int difficulty_mode = 0;
	private int purchase_count = 0;
	private int overlay_mode = 0;

	private int locationDelay = 0;

	private int collisionTick = 0;

	List<KaiTile> ThisLocationTiles = new();
	readonly Dictionary<string, List<KaiTile>> tileDict = new();

	Texture2D tileTexture = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
	Texture2D tileTexture2 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
	Texture2D tileTexture3 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);

	private ModConfig _configuration;

	public override void Entry(IModHelper helper)
	{
		_configuration = helper.ReadConfig<ModConfig>();

		helper.Events.Input.ButtonReleased += this.OnButtonReleased;
		helper.Events.Input.ButtonPressed += this.OnButtonPressed;
		helper.Events.Display.RenderedWorld += this.DrawUpdate;

		helper.Events.GameLoop.GameLaunched += GameLaunched;
		helper.Events.GameLoop.SaveCreated += SaveCreated;
		helper.Events.GameLoop.Saved += this.SaveModData;
		helper.Events.GameLoop.SaveLoaded += this.LoadModData;
		helper.Events.GameLoop.DayStarted += this.DayStartedUpdate;
		helper.Events.GameLoop.ReturnedToTitle += this.TitleReturnUpdate;

		tileTexture = helper.ModContent.Load<Texture2D>("assets/tile.png");
		tileTexture2 = helper.ModContent.Load<Texture2D>("assets/tile_2.png");
		tileTexture3 = helper.ModContent.Load<Texture2D>("assets/tile_3.png");
	}

	private void SaveCreated(object sender, SaveCreatedEventArgs e)
	{
		var data = new ModData()
		{
			DifficultyMode = _configuration.DifficultyMode,
			TilePrice = _configuration.TilePrice,
			TilePriceRaise = _configuration.TilePriceRaise,
		};

		Helper.Data.WriteSaveData(SAVE_CONFIG_KEY, data);

		Monitor.Log($"Created mod settings based on default config", LogLevel.Debug);
	}

	private void GameLaunched(object sender, GameLaunchedEventArgs e)
	{
		var configurationMenu = this.Helper.ModRegistry.GetApi<Api.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (configurationMenu is null)
		{
			return;
		}

		configurationMenu.Register(
			mod: ModManifest,
			reset: () => _configuration = new ModConfig(),
			save: () => Helper.WriteConfig(_configuration)
		);

		_configuration = Helper.ReadConfig<ModConfig>();

		configurationMenu.AddKeybindList(
			mod: ModManifest,
			name: () => "Toggle overlay",
			getValue: () => _configuration.ToggleOverlayKey,
			setValue: value => _configuration.ToggleOverlayKey = value
		);

		configurationMenu.AddKeybindList(
			mod: ModManifest,
			name: () => "Toggle overlay mode",
			getValue: () => _configuration.ToggleOverlayModeKey,
			setValue: value => _configuration.ToggleOverlayModeKey = value
		);

		configurationMenu.AddSectionTitle(
			mod: ModManifest,
			text: () => "Default settings",
			tooltip: () => "The default settings used for any new save file.\nThis will not impact your current save!"
		);

		configurationMenu.AddParagraph(
			mod: ModManifest,
			text: () => "These settings will be used for any new save file\nThis will not impact your current save!"
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => "Difficulty",
			tooltip: () => "The default difficulty used for new save, 1 is the easiest, 3 is the hardest",
			getValue: () => _configuration.DifficultyMode,
			setValue: value => _configuration.DifficultyMode = value,
			min: 0,
			max: 2
		);

		configurationMenu.AddParagraph(
			mod: ModManifest,
			text: () => "Tile pricing is calculated differently based on the difficulty settings.\n- For difficulty 0 the tile price increases based on the tile price inrease setting\n- For difficulty 1 the tile price doubles at set intervals.These are 1 - 10, 10 - 100, 100 - 1000 etc.\n- For difficulty 2 the tile price increases with 1 for every tile purchased"
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => "Tile price",
			tooltip: () => "The default tile price upon which all tiles are calculated",
			getValue: () => _configuration.TilePrice,
			setValue: value => _configuration.TilePrice = value,
			min: 1
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => "Tile price increase",
			tooltip: () => "The tile price increase per tile, only used on the easiest difficulty",
			getValue: () => _configuration.TilePriceRaise,
			setValue: value => _configuration.TilePriceRaise = value,
			min: 0f
		);
	}

	private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
	{
		// ignore if player hasn't loaded a save yet
		if (!Context.IsWorldReady) return;

		if (!Context.IsPlayerFree) return;

		if (Game1.player.isFakeEventActor) return;

		if (_configuration.ToggleOverlayKey.IsDown())
		{
			toggle_overlay = !toggle_overlay;
			this.Monitor.Log($"Tileman Overlay set to:{toggle_overlay}", LogLevel.Debug);
			if (toggle_overlay) Game1.playSound("coin", 1000);
			if (!toggle_overlay) Game1.playSound("coin", 600);
		}

		if (_configuration.ToggleOverlayModeKey.IsDown())
		{
			overlay_mode++;
			var mode = "Mouse";
			if (overlay_mode > 1)
			{
				mode = "Controller";
				overlay_mode = 0;
			}

			Monitor.Log($"Tileman Overlay Mode set to:{mode}", LogLevel.Debug);
			Game1.playSound("coin", 1200);
		}

		if (!toggle_overlay) return;

		if (e.Button.IsUseToolButton()) tool_button_pushed = true;
	}

	private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
	{
		if (e.Button.IsUseToolButton()) tool_button_pushed = false;
	}

	private void DayStartedUpdate(object sender, DayStartedEventArgs e)
	{
		Monitor.Log("Day started", LogLevel.Debug);

		GetLocationTiles(Game1.currentLocation);
	}

	private void TitleReturnUpdate(object sender, ReturnedToTitleEventArgs e)
	{
		ResetValues();
	}

	private void DrawUpdate(object sender, RenderedWorldEventArgs e)
	{
		if (!Context.IsWorldReady)
		{
			return;
		}

		//Makes sure to not draw while a cutscene is happening
		if (Game1.CurrentEvent != null && !Game1.CurrentEvent.playerControlSequence)
		{
			return;
		}

		GroupIfLocationChange();

		for (int i = 0; i < ThisLocationTiles.Count; i++)
		{
			KaiTile t = ThisLocationTiles[i];
			if (t.Location == Game1.currentLocation.Name || Game1.currentLocation.Name == "Temp")
			{
				if (toggle_overlay)
				{
					var texture = tileTexture;
					var stringColor = Color.Gold;

					//Cursor
					if (overlay_mode == 1)
					{
						if (Game1.currentCursorTile == new Vector2(t.X, t.Y))
						{
							texture = tileTexture2;

							if (Game1.player.Money < (int)Math.Floor(dynamic_tile_price))
							{
								stringColor = Color.Red;
								texture = tileTexture3;
							}

							e.SpriteBatch.DrawString(Game1.dialogueFont, $"${(int)Math.Floor(dynamic_tile_price)}",
								new Vector2(Game1.getMousePosition().X, Game1.getMousePosition().Y - Game1.tileSize), stringColor);
						}
					}
					//Keyboard or Controller
					else
					{
						if (Game1.player.nextPositionTile().X == t.X && Game1.player.nextPositionTile().Y == t.Y)
						{
							texture = tileTexture2;

							if (Game1.player.Money < (int)Math.Floor(dynamic_tile_price))
							{
								texture = tileTexture3;
								stringColor = Color.Red;
							}

							e.SpriteBatch.DrawString(Game1.dialogueFont, $"${(int)Math.Floor(dynamic_tile_price)}",
								new Vector2((t.X) * 64 - Game1.viewport.X, (t.Y) * 64 - 64 - Game1.viewport.Y), stringColor);
						}
					}
					t.DrawTile(texture, e.SpriteBatch);
				}

				//Prevent player from being pushed out of bounds
				if (do_collision)
				{
					PlayerCollisionCheck(t);
				}
			}
		}

		if (tool_button_pushed) PurchaseTilePreCheck();
	}

	private void GetTilePrice()
	{
		switch (difficulty_mode)
		{
			case 0:
				//Slowly increase tile cost over time // Change 0 for initial buffer
				if (purchase_count > 0) dynamic_tile_price += tile_price_raise;
				break;

			case 1:
				//Increase tile cost through milestones
				if (purchase_count > 1) dynamic_tile_price = tile_price * 2;
				if (purchase_count > 10) dynamic_tile_price = tile_price * 4;
				if (purchase_count > 100) dynamic_tile_price = tile_price * 8;
				if (purchase_count > 1000) dynamic_tile_price = tile_price * 16;
				if (purchase_count > 10000) dynamic_tile_price = tile_price * 32;
				if (purchase_count > 100000) dynamic_tile_price = tile_price * 64;

				break;
			case 2:
				//Increment tile price with each one purchased
				dynamic_tile_price = purchase_count;
				break;
		}
	}

	private void PurchaseTilePreCheck()
	{
		for (int i = 0; i < ThisLocationTiles.Count; i++)
		{
			KaiTile t = ThisLocationTiles[i];

			//Cursor 
			if (overlay_mode == 1)
			{
				if (Game1.currentCursorTile == new Vector2(t.X, t.Y))
				{
					PurchaseTileCheck(t);
				}
			}
			//Keyboard or Controller
			else
			{
				if (Game1.player.nextPositionTile().X == t.X && Game1.player.nextPositionTile().Y == t.Y)
				{
					PurchaseTileCheck(t);
				}
			}
		}
	}

	private void PurchaseTileCheck(KaiTile thisTile)
	{
		int floor_price = (int)Math.Floor(dynamic_tile_price);

		if (Game1.player.Money < floor_price)
		{
			Game1.playSound("grunt", 700 + (100 * new Random().Next(0, 7)));
			return;
		}

		Game1.player.Money -= floor_price;

		GetTilePrice();

		purchase_count++;

		Game1.playSound("purchase", 700 + (100 * new Random().Next(0, 7)));

		var gameLocation = Game1.currentLocation;

		gameLocation.removeTileProperty(thisTile.X, thisTile.Y, "Back", "Buildable");
		if (gameLocation.doesTileHavePropertyNoNull(thisTile.X, thisTile.Y, "Type", "Back") == "Dirt"
				|| gameLocation.doesTileHavePropertyNoNull(thisTile.X, thisTile.Y, "Type", "Back") == "Grass") gameLocation.setTileProperty(thisTile.X, thisTile.Y, "Back", "Diggable", "true");

		gameLocation.removeTileProperty(thisTile.X, thisTile.Y, "Back", "NoFurniture");
		gameLocation.removeTileProperty(thisTile.X, thisTile.Y, "Back", "NoSprinklers");

		gameLocation.removeTileProperty(thisTile.X, thisTile.Y, "Back", "Passable");
		gameLocation.removeTileProperty(thisTile.X, thisTile.Y, "Back", "Placeable");

		ThisLocationTiles.Remove(thisTile);
	}

	private void PlaceInTempArea(GameLocation gameLocation)
	{
		Monitor.Log($"Placing Tiles in Temporary Area: {Game1.whereIsTodaysFest}", LogLevel.Debug);

		var tiles = GetTilesForLocation(gameLocation);
		ThisLocationTiles = tiles;
	}

	private static List<KaiTile> GetTilesForLocation(GameLocation mapLocation)
	{
		int mapWidth = mapLocation.map.Layers[0].LayerWidth;
		int mapHeight = mapLocation.map.Layers[0].LayerHeight;

		var tiles = new List<KaiTile>();

		for (int i = 1; i < mapWidth - 1; i++)
		{
			for (int j = 1; j < mapHeight - 1; j++)
			{
				if (!mapLocation.isObjectAtTile(i, j)
					&& !mapLocation.isOpenWater(i, j)
					&& !mapLocation.isTerrainFeatureAt(i, j)
					&& mapLocation.isTilePlaceable(new Vector2(i, j))
					&& mapLocation.isTileLocationTotallyClearAndPlaceable(new Vector2(i, j))
					&& mapLocation.Map.Layers[0].IsValidTileLocation(i, j)
					&& mapLocation.isCharacterAtTile(new Vector2(i, j)) == null
					&& new Vector2(Game1.player.position.X, Game1.player.position.Y) != new Vector2(i, j))
				{
					tiles.Add(new(i, j, mapLocation.Name));
				}
			}
		}

		return tiles;
	}

	private void GroupIfLocationChange()
	{
		if (Game1.locationRequest != null)
		{
			if (Game1.locationRequest.Location != Game1.currentLocation && !location_changed)
			{
				locationDelay = 35;
				location_changed = true;

				if (Game1.currentLocation.Name == "Temp")
				{
					SaveLocationTiles(Game1.currentLocation);
				}
			}
		}
		else if (location_changed)
		{
			if (locationDelay <= 0)
			{
				//First encounter with specific Temp area
				if (Game1.currentLocation.Name == "Temp")
				{
					if (Helper.Data.ReadJsonFile<MapData>($"jsons/" +
						$"{Constants.SaveFolderName}/" +
						$"{Game1.currentLocation.Name + Game1.whereIsTodaysFest}.json") == null)
					{
						PlaceInTempArea(Game1.currentLocation);
					}
					else
					{
						Monitor.Log($"Grouping Tiles At: {Game1.currentLocation.NameOrUniqueName}", LogLevel.Debug);
						GetLocationTiles(Game1.currentLocation);
					}

					location_changed = false;
				}
				else
				{
					Monitor.Log($"Grouping Tiles At: {Game1.currentLocation.NameOrUniqueName}", LogLevel.Debug);
					GetLocationTiles(Game1.currentLocation);

					location_changed = false;
				}
			}

			locationDelay--;
		}
	}

	private void SaveLocationTiles(GameLocation gameLocation)
	{
		var locationName = gameLocation.Name;

		if (locationName == "Temp") locationName += Game1.whereIsTodaysFest;
		Monitor.Log($"Saving in {locationName}", LogLevel.Debug);

		var tileData = Helper.Data.ReadJsonFile<MapData>($"jsons/{Constants.SaveFolderName}/{locationName}.json") ?? new MapData();

		if (gameLocation.Name == "Temp")
		{
			tileData.AllKaiTilesList = ThisLocationTiles;
		}
		else
		{
			tileData.AllKaiTilesList = tileDict[locationName];
		}
		Helper.Data.WriteJsonFile<MapData>($"jsons/{Constants.SaveFolderName}/{locationName}.json", tileData);
	}

	private void GetLocationTiles(GameLocation gameLocation)
	{
		Monitor.Log($"Get location tiles: {gameLocation.Name}", LogLevel.Debug);

		var locationName = gameLocation.Name;

		if (locationName == "Temp")
		{
			locationName += Game1.whereIsTodaysFest;
		}

		if (!tileDict.ContainsKey(locationName))
		{
			var savedTiles = Helper.Data.ReadJsonFile<MapData>($"jsons/{Constants.SaveFolderName}/{locationName}.json");
			if (savedTiles is not null)
			{
				tileDict.Add(locationName, savedTiles.AllKaiTilesList);
			}
		}

		if (!tileDict.ContainsKey(locationName))
		{
			var tiles = GetTilesForLocation(gameLocation);
			tileDict.Add(gameLocation.Name, tiles);
		}

		ThisLocationTiles = tileDict[locationName];
		if (ThisLocationTiles.Count == 0)
		{
			Monitor.Log($"All tiles for {locationName} have been bought?", LogLevel.Debug);
		}

		if (gameLocation.Name != "Temp")
		{
			for (int i = 0; i < ThisLocationTiles.Count; i++)
			{
				var t = ThisLocationTiles[i];

				if (!allow_player_placement)
				{
					gameLocation.removeTileProperty(t.X, t.Y, "Back", "Diggable");

					gameLocation.setTileProperty(t.X, t.Y, "Back", "Buildable", "false");
					gameLocation.setTileProperty(t.X, t.Y, "Back", "NoFurniture", "true");
					gameLocation.setTileProperty(t.X, t.Y, "Back", "NoSprinklers", "");
					gameLocation.setTileProperty(t.X, t.Y, "Back", "Placeable", "");
				}
			}
		}
	}
	private void ResetValues()
	{
		do_loop = true;
		toggle_overlay = true;
		do_collision = true;

		tile_price = 1.0f;
		tile_price_raise = 0.20f;
		purchase_count = 0;

		ThisLocationTiles.Clear();

		tileDict.Clear();
	}

	private void PlayerCollisionCheck(KaiTile tile)
	{
		if (Game1.getLocationFromName(tile.Location) == Game1.currentLocation || Game1.currentLocation.Name == "Temp")
		{
			Rectangle tileBox = new(tile.X * 64, tile.Y * 64, tile.Width, tile.Height);
			Rectangle playerBox = Game1.player.GetBoundingBox();

			if (playerBox.Intersects(tileBox))
			{
				if (collisionTick > 120)
				{
					Game1.player.Money += (int)dynamic_tile_price;
					collisionTick = 0;
					PurchaseTileCheck(tile);
				}

				var xDist = playerBox.Right - tileBox.Left;
				var xDist2 = tileBox.Right - playerBox.Left;
				var yDist = playerBox.Bottom - tileBox.Top;
				var yDist2 = tileBox.Bottom - playerBox.Top;
				var xOffset = 0;

				if (Game1.player.movementDirections.Count > 1
				&& (Game1.player.movementDirections[0] == 3 || Game1.player.movementDirections[1] == 3)) xOffset = 20;

				if (Math.Abs(xDist - xDist2) >= Math.Abs(yDist - yDist2) + xOffset)
				{
					if (xDist >= xDist2)
					{
						var newPos = new Vector2(Game1.player.Position.X + xDist2, Game1.player.Position.Y);
						Game1.player.Position = newPos;
					}
					//Collide from Right
					else
					{
						var newPos = new Vector2(Game1.player.Position.X - xDist, Game1.player.Position.Y);
						Game1.player.Position = newPos;
					}
				}
				else
				{
					//Collide from Top
					if (yDist >= yDist2)
					{
						var newPos = new Vector2(Game1.player.Position.X, Game1.player.Position.Y + yDist2);
						Game1.player.Position = newPos;
					}
					//Collide from Bottom
					else
					{
						var newPos = new Vector2(Game1.player.Position.X, Game1.player.Position.Y - yDist);
						Game1.player.Position = newPos;
					}
				}
				collisionTick++;
			}

			if (playerBox.Center == tileBox.Center || playerBox.Intersects(tileBox) && locationDelay > 0)
			{
				if (collisionTick > 120)
				{
					Game1.player.Money += (int)tile_price;
					collisionTick = 0;
					PurchaseTileCheck(tile);
				}

				Game1.player.Position = Game1.player.lastPosition;
				collisionTick++;
			}
		}
	}

	private void SaveModData(object sender, SavedEventArgs e)
	{
		foreach (KeyValuePair<string, List<KaiTile>> entry in tileDict)
		{
			SaveLocationTiles(Game1.getLocationFromName(entry.Key));
		}
		tileDict.Clear();

		var data = new ModData
		{
			ToPlaceTiles = do_loop,
			DoCollision = do_collision,
			ToggleOverlay = toggle_overlay,
			TilePrice = tile_price,
			TilePriceRaise = tile_price_raise,
			DifficultyMode = difficulty_mode,
			PurchaseCount = purchase_count
		};

		Helper.Data.WriteSaveData<ModData>(SAVE_CONFIG_KEY, data);
	}

	private void LoadModData(object sender, SaveLoadedEventArgs e)
	{
		ConvertModConfigToSaveConfig();

		var data = Helper.Data.ReadSaveData<ModData>(SAVE_CONFIG_KEY);

		do_loop = data.ToPlaceTiles;
		toggle_overlay = data.ToggleOverlay;
		do_collision = data.DoCollision;
		tile_price = data.TilePrice;
		tile_price_raise = data.TilePriceRaise;
		difficulty_mode = data.DifficultyMode;
		purchase_count = data.PurchaseCount;

		Monitor.Log("Mod Data Loaded", LogLevel.Debug);
	}

	/// <summary>
	/// Replace the old config file that was saved in the mod directory.
	/// </summary>
	/// <remarks>
	/// Converts and removes the old config if it exists and saves it to the actual save file so it gets synced with cloud saves
	/// </remarks>
	private void ConvertModConfigToSaveConfig()
	{
		var saveConfigFile = $"jsons/{Constants.SaveFolderName}/config.json";
		var saveConfigPath = System.IO.Path.Combine(Helper.DirectoryPath, saveConfigFile);

		if (System.IO.File.Exists(saveConfigPath))
		{
			var data = Helper.Data.ReadJsonFile<ModData>(saveConfigFile);
			data ??= new();

			Helper.Data.WriteSaveData(SAVE_CONFIG_KEY, data);

			System.IO.File.Delete(saveConfigPath);
			Monitor.Log("Converted old config file to save data", LogLevel.Debug);
		}
	}
}
