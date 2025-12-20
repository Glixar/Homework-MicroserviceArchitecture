using System.Collections;

namespace SharedKernel;

public class ErrorList : IEnumerable<Error>
{
    public static readonly ErrorList Empty = new(Array.Empty<Error>());
    private readonly List<Error> _errors;

    public ErrorList(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        _errors = new List<Error>(errors);
    }

    public int Count => _errors.Count;

    public bool IsEmpty => _errors.Count == 0;

    public IEnumerator<Error> GetEnumerator() => _errors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static ErrorList From(Error error) => new(new[] { error });

    public static ErrorList From(IEnumerable<Error> errors) => new(errors);

    public static implicit operator ErrorList(List<Error> errors) => new(errors);

    public static implicit operator ErrorList(Error error) => new(new[] { error });
}