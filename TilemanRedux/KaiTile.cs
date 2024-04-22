using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace TilemanRedux;

public class KaiTile
{
	public int Width { get; } = Game1.tileSize;
	public int Height { get; } = Game1.tileSize;

	public int X { get; set; } = 0;
	public int Y { get; set; } = 0;

	public string Location { get; set; } = null;

	public KaiTile(int TileX, int TileY, string TileIsWhere)
	{
		X = TileX;
		Y = TileY;
		Location = TileIsWhere;
	}

	public void DrawTile(Texture2D texture, SpriteBatch spriteBatch)
	{
		float offsetX = Game1.viewport.X;
		float offsetY = Game1.viewport.Y;

		spriteBatch.Draw(texture,
			Utility.getRectangleCenteredAt(new Vector2((this.X + 1) * 64 - offsetX - 32, (this.Y + 1) * 64 - offsetY - 32), 64),
			Color.White);
	}

	public bool IsSpecifiedTile(int TileX, int TileY, string TileIsWhere)
	{
		if (TileX == X && TileY == Y && TileIsWhere == Location) return true;
		return false;
	}
}
