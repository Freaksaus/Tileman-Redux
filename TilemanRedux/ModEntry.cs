using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using TilemanRedux.Services;

namespace TilemanRedux;

public sealed class ModEntry : Mod
{
	private const string SAVE_CONFIG_KEY = "tilemanredux-config";
	private const string BUY_LOCATION_DIALOGUE_KEY = "tilemanredux-buy-location";
	private const string BUY_LOCATION_YES_KEY = "tilemanredux-buy-location-yes";
	private const string BUY_LOCATION_NO_KEY = "tilemanredux-buy-location-no";
	private const string TEMPORARY_LOCATION_NAME = "Temp";

	private bool tool_button_pushed = false;
	private bool location_changed = false;
	private bool isFishing = false;

	private float _currentTilePrice = 0;

	private int locationDelay = 0;

	private int collisionTick = 0;

	List<KaiTile> ThisLocationTiles = new();
	readonly Dictionary<string, List<KaiTile>> tileDict = new();

	Texture2D tileTexture = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
	Texture2D tileTexture2 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
	Texture2D tileTexture3 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
	Texture2D noBuyingTileTexture = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);

	private ModConfig _configuration;
	private ModData _data;

	private readonly ITilePricingService _tilePricingService;

	public ModEntry()
	{
		_tilePricingService = new TilePricingService();
	}

	public override void Entry(IModHelper helper)
	{
		_configuration = helper.ReadConfig<ModConfig>();

		helper.Events.Input.ButtonReleased += this.OnButtonReleased;
		helper.Events.Input.ButtonPressed += this.OnButtonPressed;

		helper.Events.Display.RenderedWorld += this.DrawUpdate;
		helper.Events.Display.MenuChanged += MenuChanged;

		helper.Events.GameLoop.GameLaunched += GameLaunched;
		helper.Events.GameLoop.SaveCreated += SaveCreated;
		helper.Events.GameLoop.Saving += Saving;
		helper.Events.GameLoop.Saved += this.SaveModData;
		helper.Events.GameLoop.SaveLoaded += this.LoadModData;
		helper.Events.GameLoop.DayStarted += this.DayStartedUpdate;
		helper.Events.GameLoop.ReturnedToTitle += this.TitleReturnUpdate;

		tileTexture = helper.ModContent.Load<Texture2D>("assets/tile.png");
		tileTexture2 = helper.ModContent.Load<Texture2D>("assets/tile_2.png");
		tileTexture3 = helper.ModContent.Load<Texture2D>("assets/tile_3.png");
		noBuyingTileTexture = helper.ModContent.Load<Texture2D>("assets/no_buying_tile.png");
	}

	private void MenuChanged(object sender, MenuChangedEventArgs e)
	{
		if (e.NewMenu is BobberBar)
		{
			isFishing = true;
		}
		else if (e.OldMenu is BobberBar)
		{
			isFishing = false;
		}
	}

	private void Saving(object sender, SavingEventArgs e)
	{
		Helper.Data.WriteSaveData(SAVE_CONFIG_KEY, _data);
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
			name: () => Helper.Translation.Get("toggle-overlay"),
			getValue: () => _configuration.ToggleOverlayKey,
			setValue: value => _configuration.ToggleOverlayKey = value
		);

		configurationMenu.AddKeybindList(
			mod: ModManifest,
			name: () => Helper.Translation.Get("toggle-overlay-mode"),
			getValue: () => _configuration.ToggleOverlayModeKey,
			setValue: value => _configuration.ToggleOverlayModeKey = value
		);

		configurationMenu.AddKeybindList(
			mod: ModManifest,
			name: () => Helper.Translation.Get("toggle-overlay-mode"),
			getValue: () => _configuration.ChangeDifficultyKey,
			setValue: value => _configuration.ChangeDifficultyKey = value
		);

		configurationMenu.AddKeybindList(
			mod: ModManifest,
			name: () => Helper.Translation.Get("buy-all-tiles"),
			getValue: () => _configuration.BuyLocationTilesKey,
			setValue: value => _configuration.BuyLocationTilesKey = value
		);

		configurationMenu.AddSectionTitle(
			mod: ModManifest,
			text: () => Helper.Translation.Get("default-settings"),
			tooltip: () => Helper.Translation.Get("default-settings-tooltip")
		);

		configurationMenu.AddParagraph(
			mod: ModManifest,
			text: () => Helper.Translation.Get("default-settings-warning")
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => Helper.Translation.Get("difficulty"),
			tooltip: () => Helper.Translation.Get("difficulty-tooltip"),
			getValue: () => _configuration.DifficultyMode,
			setValue: value => _configuration.DifficultyMode = value,
			min: 0,
			max: 2
		);

		configurationMenu.AddParagraph(
			mod: ModManifest,
			text: () => Helper.Translation.Get("tile-pricing-explanation")
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => Helper.Translation.Get("tile-price"),
			tooltip: () => Helper.Translation.Get("tile-price-tooltip"),
			getValue: () => _configuration.TilePrice,
			setValue: value => _configuration.TilePrice = value,
			min: 1
		);

		configurationMenu.AddNumberOption(
			mod: ModManifest,
			name: () => Helper.Translation.Get("tile-price-increase"),
			tooltip: () => Helper.Translation.Get("tile-price-increase-tooltip"),
			getValue: () => _configuration.TilePriceRaise,
			setValue: value => _configuration.TilePriceRaise = value,
			min: 0f
		);
	}

	private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
	{
		if (!Context.IsWorldReady)
		{
			return;
		}

		if (!Context.IsPlayerFree)
		{
			return;
		}

		if (Game1.player.isFakeEventActor)
		{
			return;
		}

		if (_configuration.ToggleOverlayKey.IsDown())
		{
			_data.ToggleOverlay = !_data.ToggleOverlay;
			this.Monitor.Log($"Tileman Overlay set to:{_data.ToggleOverlay}", LogLevel.Debug);

			if (_data.ToggleOverlay)
			{
				Game1.playSound("coin", 1000);
			}
			else
			{
				Game1.playSound("coin", 600);
			}
		}

		if (_configuration.ToggleOverlayModeKey.IsDown())
		{
			ChangeOverlayMode();
		}

		if (_configuration.ChangeDifficultyKey.IsDown())
		{
			_data.DifficultyMode++;
			if (_data.DifficultyMode > 2)
			{
				_data.DifficultyMode = 0;
			}

			Game1.addHUDMessage(new($"Changed difficulty to {_data.DifficultyMode}"));
		}

		if (_configuration.BuyLocationTilesKey.IsDown())
		{
			ShowLocationBuyoutMenu();
		}

		if (!_data.ToggleOverlay)
		{
			return;
		}

		if (e.Button.IsUseToolButton())
		{
			tool_button_pushed = true;
		}
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
		if (!ShoulDrawTiles())
		{
			return;
		}

		GroupIfLocationChange();

		var locationName = GetLocationName(Game1.currentLocation);
		for (int i = 0; i < ThisLocationTiles.Count; i++)
		{
			KaiTile t = ThisLocationTiles[i];
			if (t.Location == locationName)
			{
				if (_data.ToggleOverlay)
				{
					var texture = tileTexture;
					var stringColor = Color.Gold;

					if (_data.OverlayMode == OverlayMode.BUY_WITH_CURSOR)
					{
						if (Game1.currentCursorTile == new Vector2(t.X, t.Y))
						{
							texture = tileTexture2;

							if (Game1.player.Money < (int)Math.Floor(_currentTilePrice))
							{
								stringColor = Color.Red;
								texture = tileTexture3;
							}

							e.SpriteBatch.DrawString(Game1.dialogueFont, $"${(int)Math.Floor(_currentTilePrice)}",
								new Vector2(Game1.getMousePosition().X, Game1.getMousePosition().Y - Game1.tileSize), stringColor);
						}
					}
					else if (_data.OverlayMode == OverlayMode.BUY_WITH_TOOL)
					{
						if (Game1.player.nextPositionTile().X == t.X && Game1.player.nextPositionTile().Y == t.Y)
						{
							texture = tileTexture2;

							if (Game1.player.Money < (int)Math.Floor(_currentTilePrice))
							{
								texture = tileTexture3;
								stringColor = Color.Red;
							}

							e.SpriteBatch.DrawString(Game1.dialogueFont, $"${(int)Math.Floor(_currentTilePrice)}",
								new Vector2((t.X) * 64 - Game1.viewport.X, (t.Y) * 64 - 64 - Game1.viewport.Y), stringColor);
						}
					}
					else if (_data.OverlayMode == OverlayMode.NO_BUYING)
					{
						texture = noBuyingTileTexture;
					}

					t.DrawTile(texture, e.SpriteBatch);
				}

				//Prevent player from being pushed out of bounds
				if (_data.DoCollision)
				{
					PlayerCollisionCheck(t);
				}
			}
		}

		if (tool_button_pushed) PurchaseTilePreCheck();
	}

	private bool ShoulDrawTiles()
	{
		if (!Context.IsWorldReady)
		{
			return false;
		}

		if (Game1.CurrentEvent != null && !Game1.CurrentEvent.playerControlSequence)
		{
			return false;
		}

		if (Game1.farmEvent is not null)
		{
			return false;
		}

		if (isFishing)
		{
			return false;
		}

		return true;
	}

	private void PurchaseTilePreCheck()
	{
		for (int i = 0; i < ThisLocationTiles.Count; i++)
		{
			KaiTile t = ThisLocationTiles[i];

			if (_data.OverlayMode == OverlayMode.BUY_WITH_CURSOR)
			{
				if (Game1.currentCursorTile == new Vector2(t.X, t.Y))
				{
					TryAndPurchaseTile(t, true);
				}
			}
			else if (_data.OverlayMode == OverlayMode.BUY_WITH_TOOL)
			{
				if (Game1.player.nextPositionTile().X == t.X && Game1.player.nextPositionTile().Y == t.Y)
				{
					TryAndPurchaseTile(t, true);
				}
			}
		}
	}

	private void TryAndPurchaseTile(KaiTile thisTile, bool playSound)
	{
		int floor_price = (int)Math.Floor(_currentTilePrice);

		if (Game1.player.Money < floor_price)
		{
			if (playSound)
			{
				Game1.playSound("grunt", 700 + (100 * new Random().Next(0, 7)));
			}

			return;
		}

		Game1.player.Money -= floor_price;
		_data.TotalMoneySpent += floor_price;

		_data.PurchaseCount++;
		_currentTilePrice = _tilePricingService.CalculateTilePrice(
			_data.DifficultyMode,
			_currentTilePrice,
			_data.TilePrice,
			_data.TilePriceRaise,
			_data.PurchaseCount);

		if (playSound)
		{
			PlayPurchaseSound();
		}

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

	private static List<KaiTile> GetTilesForLocation(GameLocation mapLocation)
	{
		var locationName = GetLocationName(mapLocation);
		int mapWidth = mapLocation.map.Layers[0].LayerWidth;
		int mapHeight = mapLocation.map.Layers[0].LayerHeight;

		var tiles = new List<KaiTile>();

		for (int i = 1; i < mapWidth - 1; i++)
		{
			for (int j = 1; j < mapHeight - 1; j++)
			{
				var vector = new Vector2(i, j);
				if (!mapLocation.isObjectAtTile(i, j)
					&& !mapLocation.isOpenWater(i, j)
					&& !mapLocation.isTerrainFeatureAt(i, j)
					&& mapLocation.isTilePlaceable(vector)
					&& mapLocation.isTileLocationTotallyClearAndPlaceable(vector)
					&& mapLocation.Map.Layers[0].IsValidTileLocation(i, j)
					&& mapLocation.isCharacterAtTile(vector) == null
					&& new Vector2(Game1.player.position.X, Game1.player.position.Y) != vector)
				{
					tiles.Add(new(i, j, locationName));
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
			}
		}
		else if (location_changed)
		{
			if (locationDelay <= 0)
			{
				GetLocationTiles(Game1.currentLocation);
				location_changed = false;
			}

			locationDelay--;
		}
	}

	private void SaveLocationTiles(GameLocation gameLocation)
	{
		var locationName = gameLocation.Name;

		if (locationName == TEMPORARY_LOCATION_NAME)
		{
			locationName += Game1.whereIsTodaysFest;
		}

		Monitor.Log($"Saving in {locationName}", LogLevel.Debug);

		var tileData = Helper.Data.ReadJsonFile<MapData>($"jsons/{Constants.SaveFolderName}/{locationName}.json") ?? new MapData();

		if (gameLocation.Name == TEMPORARY_LOCATION_NAME)
		{
			tileData.AllKaiTilesList = ThisLocationTiles;
		}
		else
		{
			tileData.AllKaiTilesList = tileDict[locationName];
		}

		Helper.Data.WriteJsonFile<MapData>($"jsons/{Constants.SaveFolderName}/{locationName}.json", tileData);
	}

	private void GetLocationTiles(GameLocation location)
	{
		var locationName = GetLocationName(location);
		Monitor.Log($"Get location tiles: {locationName}", LogLevel.Debug);

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
			var tiles = GetTilesForLocation(location);
			tileDict.Add(locationName, tiles);
		}

		ThisLocationTiles = tileDict[locationName];

		if (ThisLocationTiles.Count == 0)
		{
			Monitor.Log($"All tiles for {locationName} have been bought", LogLevel.Debug);
		}

		if (location.Name != TEMPORARY_LOCATION_NAME)
		{
			for (int i = 0; i < ThisLocationTiles.Count; i++)
			{
				var t = ThisLocationTiles[i];

				if (!_data.AllowPlayerPlacement)
				{
					location.removeTileProperty(t.X, t.Y, "Back", "Diggable");

					location.setTileProperty(t.X, t.Y, "Back", "Buildable", "false");
					location.setTileProperty(t.X, t.Y, "Back", "NoFurniture", "true");
					location.setTileProperty(t.X, t.Y, "Back", "NoSprinklers", "");
					location.setTileProperty(t.X, t.Y, "Back", "Placeable", "");
				}
			}
		}
	}

	private void ResetValues()
	{
		_data = new();
		_currentTilePrice = 0;
		ThisLocationTiles.Clear();

		tileDict.Clear();
	}

	private void PlayerCollisionCheck(KaiTile tile)
	{
		if (Game1.getLocationFromName(tile.Location) == Game1.currentLocation || Game1.currentLocation.Name == TEMPORARY_LOCATION_NAME)
		{
			Rectangle tileBox = new(tile.X * 64, tile.Y * 64, tile.Width, tile.Height);
			Rectangle playerBox = Game1.player.GetBoundingBox();

			if (playerBox.Intersects(tileBox))
			{
				if (_data.OverlayMode != OverlayMode.NO_BUYING && collisionTick > 120)
				{
					Game1.player.Money += (int)_currentTilePrice;
					collisionTick = 0;
					TryAndPurchaseTile(tile, true);
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
				if (_data.OverlayMode != OverlayMode.NO_BUYING && collisionTick > 120)
				{
					Game1.player.Money += (int)_data.TilePrice;
					collisionTick = 0;
					TryAndPurchaseTile(tile, true);
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
	}

	private void LoadModData(object sender, SaveLoadedEventArgs e)
	{
		ConvertModConfigToSaveConfig();

		_data = Helper.Data.ReadSaveData<ModData>(SAVE_CONFIG_KEY)
			?? new();

		SetMoneySpentForOldSaves();

		_currentTilePrice = _tilePricingService.CalculateTilePrice(
			_data.DifficultyMode,
			_currentTilePrice,
			_data.TilePrice,
			_data.TilePriceRaise,
			_data.PurchaseCount);

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

	/// <summary>
	/// Money spent wasn't added until 1.0.5.
	/// In case the player doesn't have this data yet try and calculate it based on the current difficulty and tiles bought.
	/// </summary>
	private void SetMoneySpentForOldSaves()
	{
		if (_data.TotalMoneySpent > 0)
		{
			return;
		}

		var tilePrice = _data.TilePrice;
		for (int i = 0; i < _data.PurchaseCount; i++)
		{
			_data.TotalMoneySpent += tilePrice;
			tilePrice = _tilePricingService.CalculateTilePrice(
				_data.DifficultyMode,
				tilePrice,
				_data.TilePrice,
				_data.TilePriceRaise,
				i);
		}

		Monitor.Log($"Set total spent to {_data.TotalMoneySpent}", LogLevel.Debug);
	}

	private static void PlayPurchaseSound()
	{
		Game1.playSound("purchase", 700 + (100 * new Random().Next(0, 7)));
	}

	private void ChangeOverlayMode()
	{
		_data.OverlayMode = _data.OverlayMode switch
		{
			OverlayMode.BUY_WITH_TOOL => OverlayMode.BUY_WITH_CURSOR,
			OverlayMode.BUY_WITH_CURSOR => OverlayMode.NO_BUYING,
			OverlayMode.NO_BUYING => OverlayMode.BUY_WITH_TOOL,
			_ => OverlayMode.BUY_WITH_TOOL,
		};

		Monitor.Log($"Changed overlay mode to {_data.OverlayMode}", LogLevel.Info);
		Game1.playSound("coin", 1200);
	}

	private void ShowLocationBuyoutMenu()
	{
		var locationName = GetLocationName(Game1.currentLocation);
		var tilesToBuy = tileDict[locationName].Count;

		if (tilesToBuy == 0)
		{
			return;
		}

		(float tilePrice, float totalPrice) = GetPriceForTiles(tilesToBuy);

		Game1.currentLocation.afterQuestion = (Farmer farmer, string whichAnswer) =>
		{
			switch (whichAnswer)
			{
				case BUY_LOCATION_YES_KEY:
					if (totalPrice > farmer.Money)
					{
						Game1.drawObjectDialogue(Helper.Translation.Get("not-enough-money"));
						return;
					}

					BuyoutCurrentLocation();

					break;
				case BUY_LOCATION_NO_KEY:
					break;
			}
		};

		Game1.currentLocation.createQuestionDialogue(
			Helper.Translation.Get("buy-all-tiles-question", new { totalprice = totalPrice, tileprice = tilePrice }),
			new Response[]
			{
				new(BUY_LOCATION_YES_KEY, Helper.Translation.Get("buy-all-tiles-yes")),
				new(BUY_LOCATION_NO_KEY, Helper.Translation.Get("buy-all-tiles-no")),
			},
			BUY_LOCATION_DIALOGUE_KEY);
	}

	private (float tilePrice, float totalPrice) GetPriceForTiles(int tileCount)
	{
		float totalPrice = 0f;
		float tilePrice = 0f;

		var purchaseCount = _data.PurchaseCount;
		for (int i = 0; i < tileCount; i++)
		{
			tilePrice = _tilePricingService.CalculateTilePrice(
				_data.DifficultyMode,
				_currentTilePrice,
				_data.TilePrice,
				_data.TilePriceRaise,
				purchaseCount);
			totalPrice += tilePrice;
			purchaseCount++;
		}

		return (tilePrice, totalPrice);
	}

	private void BuyoutCurrentLocation()
	{
		while (ThisLocationTiles.Count > 0)
		{
			TryAndPurchaseTile(ThisLocationTiles[0], false);
		}

		PlayPurchaseSound();
	}

	private static string GetLocationName(GameLocation location)
	{
		if (location.Name == TEMPORARY_LOCATION_NAME)
		{
			return Game1.whereIsTodaysFest;
		}

		return location.Name;
	}
}
