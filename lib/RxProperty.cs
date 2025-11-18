using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;

/// <summary>
/// Reactive property that holds a value and notifies subscribers on change.
/// </summary>
public class RxProperty<T>
{
	private readonly BehaviorSubject<T> _subject;

	public RxProperty(T initialValue = default)
	{
		_subject = new BehaviorSubject<T>(initialValue);
	}

	public T Value
	{
		get => _subject.Value;
		set => _subject.OnNext(value);
	}

	public IObservable<T> Observe() => _subject.AsObservable();

	public IObservable<T> ObserveDistinct() => _subject.DistinctUntilChanged();

	public IDisposable Subscribe(Action<T> onNext) => _subject.Subscribe(onNext);

	public void Dispose() => _subject.OnCompleted();
}
