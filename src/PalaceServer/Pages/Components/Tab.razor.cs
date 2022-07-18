namespace PalaceServer.Pages.Components;

public partial class Tab : ComponentBase
{
	[CascadingParameter]
	public Tabs Parent { get; set; }

	[Parameter]
	public RenderFragment ChildContent { get; set; }

	[Parameter]
	public string Name { get; set; }

	[Parameter]
	public EventCallback OnClick { get; set; }

	[Parameter]
	public string Title { get; set; }

	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object> CapturedAttributes { get; set; } = new();

	public string Active { get; set; } = string.Empty;
	string _visibility;

	protected override void OnInitialized()
	{
		Parent.AddTab(this);
	}

	public void SetVisibility(bool visibility)
	{
		_visibility = (visibility) ? string.Empty : "visually-hidden";
		StateHasChanged();
	}
}
