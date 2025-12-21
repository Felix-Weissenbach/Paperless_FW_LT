using Elastic.Clients.Elasticsearch;

namespace Paperless.Common.ElasticSearch
{
    public class ElasticClientFactory
    {
        public static ElasticsearchClient Create()
        {
            var settings = new ElasticsearchClientSettings(
                new Uri(Environment.GetEnvironmentVariable("Elastic__Url") ?? "http://elasticsearch:9200")
            )
            .DefaultIndex("documents")
            .DisableDirectStreaming();

            return new ElasticsearchClient(settings);
        }
    }
}
