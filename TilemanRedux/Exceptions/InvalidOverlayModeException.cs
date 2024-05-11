using System;

namespace TilemanRedux.Exceptions;
public sealed class InvalidOverlayModeException : Exception
{
	public InvalidOverlayModeException(int overlayMode) : base($"Tried to show tile purchase info for invalid overlay mode {overlayMode}")
	{ }
}
