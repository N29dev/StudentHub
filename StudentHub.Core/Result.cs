namespace StudentHub.Core;

// Errors are values, so failed validation short-circuits instead of throwing.
public abstract record Result<T>
{
    public Result<U> Map<U>(Func<T, U> map) => this switch
    {
        Ok<T> ok => new Ok<U>(map(ok.Value)),
        Fail<T> fail => new Fail<U>(fail.Error),
        _ => throw new InvalidOperationException()
    };

    public Result<U> Bind<U>(Func<T, Result<U>> bind) => this switch
    {
        Ok<T> ok => bind(ok.Value),
        Fail<T> fail => new Fail<U>(fail.Error),
        _ => throw new InvalidOperationException()
    };
}
public sealed record Ok<T>(T Value) : Result<T>;
public sealed record Fail<T>(string Error) : Result<T>;

public static class Functions
{
    public static Func<A, C> Compose<A, B, C>(this Func<A, B> first, Func<B, C> second)
        => value => second(first(value));

    public static Func<A, Func<B, C>> Curry<A, B, C>(this Func<A, B, C> fn)
        => a => b => fn(a, b);
}
