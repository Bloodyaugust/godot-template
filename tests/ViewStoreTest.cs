using System;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Tests;

[TestSuite]
public class ViewStoreTest
{
	[TestCase]
	public void Debug_DefaultValue_IsFalse()
	{
		var state = new ViewState
		{
			Debug = new RxProperty<bool>(false)
		};

		AssertThat(state.Debug.Value).IsEqual(false);
	}

	[TestCase]
	public void Debug_SetValue_UpdatesValue()
	{
		var state = new ViewState
		{
			Debug = new RxProperty<bool>(false)
		};

		state.Debug.Value = true;

		AssertThat(state.Debug.Value).IsEqual(true);
	}

	[TestCase]
	public void Debug_Subscribe_ReceivesUpdates()
	{
		var state = new ViewState
		{
			Debug = new RxProperty<bool>(false)
		};

		bool receivedValue = false;
		state.Debug.Subscribe(value => receivedValue = value);

		state.Debug.Value = true;

		AssertThat(receivedValue).IsEqual(true);
	}

	[TestCase]
	public void Debug_Subscribe_ReceivesInitialValue()
	{
		var state = new ViewState
		{
			Debug = new RxProperty<bool>(true)
		};

		bool receivedValue = false;
		state.Debug.Subscribe(value => receivedValue = value);

		AssertThat(receivedValue).IsEqual(true);
	}

	[TestCase]
	public void RxProperty_Dispose_CompletesSubject()
	{
		var property = new RxProperty<int>(42);
		bool completed = false;

		property.Observe().Subscribe(
			_ => { },
			_ => { },
			() => completed = true
		);

		property.Dispose();

		AssertThat(completed).IsEqual(true);
	}
}
