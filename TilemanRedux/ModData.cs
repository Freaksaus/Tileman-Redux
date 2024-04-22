namespace TilemanRedux;

class ModData
{
	public bool ToPlaceTiles { get; set; } = true;
	public bool DoCollision { get; set; } = true;
	public bool AllowPlayerPlacement { get; set; } = false;
	public bool ToggleOverlay { get; set; } = true;
	public decimal TilePrice { get; set; } = 1;
	public decimal TilePriceRaise { get; set; } = 0.0008M;
	public int CavernsExtra { get; set; } = 0;
	public int DifficultyMode { get; set; } = 1;
	public int PurchaseCount { get; set; } = 0;
}
