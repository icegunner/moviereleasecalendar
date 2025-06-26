using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace MovieReleaseCalendar.API.Data
{
    public class RavenDbDocumentStore
    {
        private readonly IDocumentStore _store;

        public RavenDbDocumentStore(IDocumentStore store)
        {
            _store = store;
        }

        public IDocumentSession OpenSession()
        {
            return _store.OpenSession();
        }

        public async Task<IAsyncDocumentSession> OpenAsyncSession()
        {
            return await Task.FromResult(_store.OpenAsyncSession());
        }
    }
}
