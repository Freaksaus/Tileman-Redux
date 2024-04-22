namespace TilemanRedux;

class ModData
{
	public bool ToPlaceTiles { get; set; } = true;
	public bool DoCollision { get; set; } = true;
	public bool AllowPlayerPlacement { get; set; } = false;
	public bool ToggleOverlay { get; set; } = true;
	public double TilePrice { get; set; } = 1;
	public double TilePriceRaise { get; set; } = 0.0008;
	public int CavernsExtra { get; set; } = 0;
	public int DifficultyMode { get; set; } = 1;
	public int PurchaseCount { get; set; } = 0;
}
