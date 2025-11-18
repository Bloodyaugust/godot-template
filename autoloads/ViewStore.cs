using Godot;

/// <summary>
/// Define all your application state fields here.
/// Add new RxProperty fields to extend the store.
/// </summary>
public struct ViewState
{
	public RxProperty<bool> Debug;
}

public partial class ViewStore : Node
{
	private static ViewStore _instance;
	public static ViewStore Instance => _instance;

	public ViewState State;

	public override void _Ready()
	{
		_instance = this;
		InitializeState();
	}

	private void InitializeState()
	{
		State = new ViewState
		{
			Debug = new RxProperty<bool>(false)
		};
	}

	public override void _ExitTree()
	{
		State.Debug?.Dispose();
	}
}
