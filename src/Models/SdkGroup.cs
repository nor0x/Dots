using Avalonia.Media;

namespace Dots.Models;

/// <summary>
/// One entry in the jump rail beside the SDK list: a major version, the badge color it is drawn
/// with, and the row the rail scrolls to. Rebuilt from the current view, so the rail only ever
/// lists groups that survived the search and the filter.
/// </summary>
public class SdkGroup
{
	public SdkGroup(int group, Sdk first)
	{
		Group = group;
		First = first;
	}

	public int Group { get; }

	/// <summary>First row of the group in view order - the scroll target.</summary>
	public Sdk First { get; }

	/// <summary>Taken from the row rather than regenerated, so the rail can never drift from the list.</summary>
	public IBrush Color => First.Color;

	public string Label => $".NET {Group}";
}
