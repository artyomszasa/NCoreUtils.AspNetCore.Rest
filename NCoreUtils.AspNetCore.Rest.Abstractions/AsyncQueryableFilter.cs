using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public delegate ValueTask<IQueryable> AsyncQueryFilter(IQueryable source, CancellationToken cancellationToken);
}