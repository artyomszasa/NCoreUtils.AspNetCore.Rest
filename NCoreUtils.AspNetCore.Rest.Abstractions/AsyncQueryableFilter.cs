namespace NCoreUtils.AspNetCore.Rest;

public delegate ValueTask<IQueryable> AsyncQueryFilter(IQueryable source, CancellationToken cancellationToken);
