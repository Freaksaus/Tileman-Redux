﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;


namespace Tileman
{
    public class ModEntry : Mod
    {



        bool do_loop = true;
        bool do_collision = true;
        bool toggle_overlay = true;
        public double tile_price = 1.0;
        double tile_price_raise = 0.0008;
        int caverns_extra = 0;


        int purchase_count=0;
        int tile_count;

        int amountLocations = 200;
        int locationDelay = 0;


        List<KaiTile> allTiles = new();
        List<KaiTile> ThisLocationTiles = new();

        Texture2D tileTexture  = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
        Texture2D tileTexture2 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);
        Texture2D tileTexture3 = new(Game1.game1.GraphicsDevice, Game1.tileSize, Game1.tileSize);

        public static readonly string dictPath = "Mods/SpicyKai.Tileman/tileData.json";

        Dictionary<string, List<KaiTile>> tileDict = new();

       




        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedWorld += this.DrawUpdate;


            helper.Events.GameLoop.Saved += this.SaveModData;
            helper.Events.GameLoop.SaveLoaded += this.LoadModData;
            helper.Events.GameLoop.DayStarted += this.DayStartedUpdate;
            helper.Events.GameLoop.ReturnedToTitle += this.TitleReturnUpdate;
      
            tileTexture = helper.ModContent.Load<Texture2D>("assets/tile.png");
            tileTexture2 = helper.ModContent.Load<Texture2D>("assets/tile_2.png");
            tileTexture3 = helper.ModContent.Load<Texture2D>("assets/tile_3.png");



            //CalculateTileSum();

        }

        private void removeSpecificTile(int xTile, int yTile, string gameLocation)
        {

            var tileData = this.Helper.Data.ReadJsonFile<MapData>($"jsons/{gameLocation}.json") ?? new MapData();
            var tempDict = tileData.AllKaiTilesDict;


            for (int i = 0; i < tileData.AllKaiTilesDict[gameLocation].Count; i++)
            {
                KaiTile t = tileData.AllKaiTilesDict[gameLocation][i];

                if (t.IsSpecifiedTile(xTile, yTile, gameLocation)) tempDict[gameLocation].Remove(t);

            }
            var mapData = new MapData
            {
                AllKaiTilesDict = tempDict,
            };



            Helper.Data.WriteJsonFile<MapData>($"jsons/{gameLocation}.json", mapData);
            tileDict = new();



        }
        public void RemoveTileExceptions()
        {

            this.Monitor.Log("Removing Unusual Tiles", LogLevel.Debug);

            removeSpecificTile(18,27,"Desert");

            removeSpecificTile(12, 9, "BusStop");




        }

        public void AddTileExceptions()
        {

            this.Monitor.Log("Placing Unusual Tiles", LogLevel.Debug);

            var tempName = "Town";

            var tileData = this.Helper.Data.ReadJsonFile<MapData>($"jsons/{tempName}.json") ?? new MapData();

            var tempTiles = tileData.AllKaiTilesDict[tempName];

            //ADD UNUSAL TILES HERE
            tempTiles.Add(new KaiTile(21, 42, tempName));
            tempTiles.Add(new KaiTile(21, 43, tempName));
            tempTiles.Add(new KaiTile(21, 44, tempName));
            tempTiles.Add(new KaiTile(21, 45, tempName));
            tempTiles.Add(new KaiTile(21, 46, tempName));

            tempTiles.Add(new KaiTile(49, 42, tempName));
            tempTiles.Add(new KaiTile(49, 43, tempName));
            tempTiles.Add(new KaiTile(49, 44, tempName));
            tempTiles.Add(new KaiTile(49, 45, tempName));
            tempTiles.Add(new KaiTile(49, 46, tempName));
            tempTiles.Add(new KaiTile(49, 47, tempName));
            tempTiles.Add(new KaiTile(49, 48, tempName));
            tempTiles.Add(new KaiTile(49, 49, tempName));
            tempTiles.Add(new KaiTile(49, 50, tempName));
            tempTiles.Add(new KaiTile(49, 51, tempName));

            tempTiles.Add(new KaiTile(55, 102, tempName));
            tempTiles.Add(new KaiTile(55, 103, tempName));
            tempTiles.Add(new KaiTile(55, 104, tempName));
            tempTiles.Add(new KaiTile(55, 105, tempName));
            tempTiles.Add(new KaiTile(55, 106, tempName));
            tempTiles.Add(new KaiTile(55, 107, tempName));

            tileDict.Add(tempName, tempTiles);
            tempTiles = new();


            var mapData = new MapData
            {
                AllKaiTilesDict = tileDict,
            };



            Helper.Data.WriteJsonFile<MapData>($"jsons/{tempName}.json", mapData);
            tileDict = new();

            //
            //specific tiles to add in /// COPY ABOVE
            //Mountain 3X3 50,6 -> 52,8

           
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet

            if (!Context.IsWorldReady) return;


            if (!Context.IsPlayerFree) return;

            if (Game1.CurrentEvent != null)
            {
                return;
            }

            if (e.Button == SButton.G)
            {
                toggle_overlay = !toggle_overlay;
                this.Monitor.Log($"Tileman Overlay set to:{toggle_overlay}", LogLevel.Debug);
                if(toggle_overlay) Game1.playSoundPitched("coin", 1000 );
                if(!toggle_overlay) Game1.playSoundPitched("coin", 600 );

            }

            if (!toggle_overlay) return;
            

            if (e.Button.IsUseToolButton() /*|| e.IsDown(SButton.MouseRight*/)
            {


                for (int i = 0; i < ThisLocationTiles.Count; i++)
                {

                    KaiTile t = ThisLocationTiles[i];
                    //controller or keyboard
                    
                        //if cursor on tile
                        if (/*Game1.curso && e.Cursor.Tile.X == t.tileX && e.Cursor.Tile.Y == t.tileY ||*/
                        Game1.player.nextPositionTile().X == t.tileX && Game1.player.nextPositionTile().Y == t.tileY)
                        {
                            PurchaseTileCheck(t);
                        }
                    
                    //mouse
                    /*else
                    {
                        if (e.Cursor.Tile.X == t.tileX && e.Cursor.Tile.Y == t.tileY )
                        {
                            PurchaseTileCheck(t);
                        }

                    }*/

                }



            }

            



        }

        private void DayStartedUpdate(object sender, DayStartedEventArgs e)
        {

            PlaceInMaps();




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

            if (Game1.CurrentEvent != null) {
                return;
            }


            GroupIfLocationChange();

            if (locationDelay > 0) locationDelay--;


            if (Game1.spriteBatch.Equals(null) == false && ThisLocationTiles != null)
            {

                for (int i = 0; i < ThisLocationTiles.Count; i++)
                {
                    KaiTile t = ThisLocationTiles[i];
                    if (t != null && Game1.getLocationFromName(t.tileIsWhere) == Game1.currentLocation)
                    {
                        if (toggle_overlay)
                        {
                            var texture = tileTexture;

                            /*if (Math.Floor((Double)((Game1.getMousePosition().X + Game1.viewport.X) / 64)) == t.tileX &&
                                Math.Floor((Double)((Game1.getMousePosition().Y + Game1.viewport.Y) / 64)) == t.tileY)
                            {
                                texture = tileTexture2;

                                e.SpriteBatch.DrawString(Game1.dialogueFont, $"${ (int)Math.Floor(tile_price)}",
                                 new Vector2(Game1.getMousePosition().X + Game1.viewport.X, Game1.getMousePosition().Y + Game1.viewport.Y), Color.Gold);

                                if (Game1.player.Money < (int)Math.Floor(tile_price)) { texture = tileTexture3; }

                            }*/
                            if (Game1.player.nextPositionTile().X == t.tileX && Game1.player.nextPositionTile().Y == t.tileY)
                            {
                                texture = tileTexture2;
                                var stringColor = Color.Gold;
                                if (Game1.player.Money < (int)Math.Floor(tile_price)) { 
                                    texture = tileTexture3;
                                    stringColor = Color.Red;
                                
                                }


                                e.SpriteBatch.DrawString(Game1.dialogueFont, $"${ (int)Math.Floor(tile_price)}",
                                 new Vector2((t.tileX ) * 64 - Game1.viewport.X, (t.tileY ) * 64 - 64 - Game1.viewport.Y), stringColor);




                            }

                            t.DrawTile(texture, e.SpriteBatch);

                        }
                         

                        //Prevent player from being pushed out of bounds
                        if(do_collision) PlayerCollisionCheck(t);

                    }




                }




            }



        }


        private static IEnumerable<GameLocation> GetLocations()
        {
            var locations = Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );

            

            return locations;
        }
        


        private bool IsTileAt(int tileX, int tileY, GameLocation TileIsAt)
        {
            foreach (KaiTile t in allTiles)
            {
                if (tileX == t.tileX && tileY == t.tileY && TileIsAt == Game1.getLocationFromName(t.tileIsWhere))
                {
                    return true;
                }

            }
            return false;


        }

        private void PurchaseTileCheck(KaiTile thisTile)
        {
            int floor_price = (int)Math.Floor(tile_price);

            if (Game1.player.Money >= floor_price)
            {
                
                Game1.player.Money -= floor_price;
                Monitor.Log($"Bought Tile({thisTile.tileX},{thisTile.tileY}) for ${floor_price}", LogLevel.Debug);

                //RAISE THIS NUMBER TO ADD AN INITIAL BUFFER BEFORE PRICE INCREASES
                if (purchase_count > 0)
                {
                    tile_price += tile_price_raise;
                }
                tile_count--;
                purchase_count++;

                
                

                Game1.playSoundPitched("purchase", 700 + (100* new Random().Next(0, 7)) );
                


                
                ThisLocationTiles.Remove(thisTile);
                allTiles.Remove(thisTile);

            }
            else Game1.playSoundPitched("grunt", 700 + (100 * new Random().Next(0, 7)));





        }

        private void PlaceInMaps()
        {
            if (Context.IsWorldReady)
            {



                if (do_loop == true)
                {


                    var locationCount = 0;
                    foreach (GameLocation location in GetLocations())
                    {
                        

                        Monitor.Log($"Placing Tiles in: {location.Name}", LogLevel.Debug);

                        locationCount++;

                        if (locationCount < amountLocations)
                        {
                            PlaceTiles(Game1.getLocationFromName(location.NameOrUniqueName));

                        }
                        else
                        {
                            break;
                        }


                        tileDict.Add(location.Name, allTiles);
                        allTiles = new();


                        var mapData = new MapData
                        {
                            AllKaiTilesDict = tileDict,
                        };



                        Helper.Data.WriteJsonFile<MapData>($"jsons/{location.Name}.json", mapData);
                        tileDict = new();
                    }

                    



                    //Place Tiles in the Mine // Mine 1-120 // Skull Caverns 121-???
                    for (int i = 1; i <= 220 + caverns_extra; i++)

                    {
                        var mineString = Game1.getLocationFromName("UndergroundMine" + i).Name;
                        if (Game1.getLocationFromName(mineString) != null)
                        {
                            PlaceTiles(Game1.getLocationFromName(mineString));
                            Monitor.Log($"Placing Tiles in: {mineString}", LogLevel.Debug);

                            tileDict.Add(mineString, allTiles);
                            allTiles = new();

                            var mapData = new MapData
                            {
                                AllKaiTilesDict = tileDict,
                            };



                            Helper.Data.WriteJsonFile<MapData>($"jsons/{mineString}.json", mapData);
                            tileDict = new();

                        }
                    }

                    //VolcanoDungeon0 - 9
                    for (int i = 0; i <= 9; i++)

                    {
                        var mineString = Game1.getLocationFromName("VolcanoDungeon" + i).Name;
                        if (Game1.getLocationFromName(mineString) != null)
                        {
                            PlaceTiles(Game1.getLocationFromName(mineString));
                            Monitor.Log($"Placing Tiles in: {mineString}", LogLevel.Debug);

                            tileDict.Add(mineString, allTiles);
                            allTiles = new();

                            var mapData = new MapData
                            {
                                AllKaiTilesDict = tileDict,
                            };



                            Helper.Data.WriteJsonFile<MapData>($"jsons/{mineString}.json", mapData);
                            tileDict = new();

                        }
                    }

                    AddTileExceptions();
                    RemoveTileExceptions();
                
                    this.GetLocationTiles(Game1.currentLocation);
                    

                    do_loop = false;

                    Monitor.Log("Press 'G' to toggle Tileman Overlay", LogLevel.Debug);

                }
            }

        }

        private void PlaceTiles(GameLocation mapLocation)
        {

            int mapWidth = mapLocation.map.Layers[0].LayerWidth;
            int mapHeight = mapLocation.map.Layers[0].LayerHeight;


            for (int i = 1; i < mapWidth - 1; i++)
            {
                for (int j = 1; j < mapHeight - 1; j++)
                {
                    if (/*!IsTileAt(i, j, mapLocation)
                        && */!mapLocation.isObjectAtTile(i, j) 
                        && !mapLocation.isOpenWater(i, j) 
                        && !mapLocation.isTerrainFeatureAt(i, j) 
                        && mapLocation.isTilePlaceable(new Vector2(i, j))
                        && mapLocation.isTileLocationTotallyClearAndPlaceable(new Vector2(i, j))
                        && mapLocation.Map.Layers[0].IsValidTileLocation(i,j) 

                        )
                    {
                        //mapLocation.map.Layers[0].PickTile().Properties
                        if (new Vector2(Game1.player.position.X, Game1.player.position.Y)!= new Vector2(i, j))
                        {
                            var t = new KaiTile(i, j, mapLocation.Name);
                            allTiles.Add(t);

                            if(mapLocation.Name == t.tileIsWhere)
                            {
                                //ThisLocationTiles.Add(t);
                            }




                            tile_count++;
                            ///this.Monitor.Log($"Tile #{tile_count} At {mapLocation.Name}", LogLevel.Debug);
                        }
                    }
                }
            }


            
            

        }

        

        private void GroupIfLocationChange()
        {
            if (Game1.locationRequest != null )
            {
                if (Game1.locationRequest.Location != Game1.currentLocation)
                {
                    SaveLocationTiles(Game1.currentLocation);

                    Monitor.Log($"Grouping Tiles At: {Game1.locationRequest.Location.NameOrUniqueName}",LogLevel.Debug);

                    GetLocationTiles(Game1.locationRequest.Location);

                    locationDelay = 20;

                }  
            }

        }
        private void SaveLocationTiles(GameLocation gameLocation)
        {
            var tileData = this.Helper.Data.ReadJsonFile<MapData>($"jsons/{gameLocation.Name}.json") ?? new MapData();
            if (tileData.AllKaiTilesDict.ContainsKey(gameLocation.Name)
                && ThisLocationTiles.Count > 0
                && gameLocation.Name == ThisLocationTiles[0].tileIsWhere)
            {
                tileData.AllKaiTilesDict[gameLocation.Name] = ThisLocationTiles;
                Helper.Data.WriteJsonFile<MapData>($"jsons/{gameLocation.Name}.json", tileData);
            }
        }
        private void GetLocationTiles(GameLocation gameLocation)
        {   
            var tileData = this.Helper.Data.ReadJsonFile<MapData>($"jsons/{gameLocation.Name}.json") ?? new MapData();
            if (tileData.AllKaiTilesDict.ContainsKey(gameLocation.Name))
            {
                ThisLocationTiles = tileData.AllKaiTilesDict[gameLocation.Name];
            }

        }

       
        private void ResetValues()
        {
            do_loop = true;
            toggle_overlay = true;

            tile_price = 1.0;
            tile_price_raise = 0.20;
            purchase_count = 0;
            tile_count = 0;

            allTiles.Clear();
            ThisLocationTiles.Clear();




    }


        
        private void CalculateTileSum()
        {
            var tileCount = 50000;
            var price = 1.0;
            var priceIncrease = 0.0008;
            var totalCost = 0;

            for(int i = 0; i < tileCount; i++)
            {
                totalCost += (int)Math.Floor(price);
                price += priceIncrease;

            }
            this.Monitor.Log($"Cost of {tileCount} tiles by the end: {totalCost}", LogLevel.Debug);
        }

        private void PlayerCollisionCheck(KaiTile tile)
        {

            if (Game1.getLocationFromName(tile.tileIsWhere) == Game1.currentLocation)
            {

                Rectangle tileBox = new(tile.tileX * 64, tile.tileY * 64, tile.tileW, tile.tileH);
                Rectangle playerBox = Game1.player.GetBoundingBox();

                if (playerBox.Center == tileBox.Center || playerBox.Intersects(tileBox) && locationDelay > 0)
                {

                    ThisLocationTiles.Remove(tile);
                    allTiles.Remove(tile);
                }

                else if (locationDelay <= 0 && Game1.player.nextPosition(Game1.player.facingDirection).Intersects(tileBox))
                {
                    Game1.player.Position = Game1.player.lastPosition;

                }

            }

        }

        

        private void SaveModData(object sender, SavedEventArgs e) {

            if (System.IO.File.Exists("config")) createJson("config");


            var tileData = new ModData
            {
                ToPlaceTiles = this.do_loop,
                ToggleOverlay = this.toggle_overlay,
                TilePrice = this.tile_price,
                TilePriceRaise = this.tile_price_raise
            };

            //this.Helper.Data.WriteSaveData("example-key", tileData);
            Helper.Data.WriteJsonFile<ModData>("config.json", tileData);
        }

        private void LoadModData(object sender, SaveLoadedEventArgs e) {

            Monitor.Log("Mod Data Loaded",LogLevel.Debug);

            if (System.IO.File.Exists("config.json")) createJson("config");

            var tileData = this.Helper.Data.ReadJsonFile<ModData>("config.json") ?? new ModData();
            //var tileData = this.Helper.Data.ReadSaveData<ModData>("example-key");

            this.do_loop = tileData.ToPlaceTiles;
            this.toggle_overlay = tileData.ToggleOverlay;
            this.do_collision = tileData.DoCollision;
            this.tile_price = tileData.TilePrice;
            this.tile_price_raise = tileData.TilePriceRaise;
            this.caverns_extra = tileData.CavernsExtra;



        }

        public void createJson(string fileName)
        {
            Monitor.Log($"Creating {fileName}.json", LogLevel.Debug);
            System.IO.File.Create($"jsons/{fileName}.json");
        }






    }


}