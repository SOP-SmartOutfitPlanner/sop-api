using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using SOPServer.Service.Utils;
using System.Diagnostics;

namespace SOPServer.Service.Services.Implements
{
    public partial class QDrantService : IQdrantService
    {
        private readonly QDrantClientSettings _qdrantSettings;
        private readonly QdrantClient _client;
        private readonly IGeminiService _geminiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public QDrantService(IOptions<QDrantClientSettings> qdrantClientSettings, IGeminiService geminiService, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _qdrantSettings = qdrantClientSettings.Value;
            _client = new QdrantClient(
                        host: qdrantClientSettings.Value.Host,
                        port: qdrantClientSettings.Value.Port,
                        apiKey: qdrantClientSettings.Value.SecretKey,
                        https: false
            );
            _geminiService = geminiService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
        {
            // Kiểm tra xem collection đã tồn tại chưa
            var collection = await _client.CollectionExistsAsync(_qdrantSettings.Collection);

            if (!collection)
            {
                await _client.CreateCollectionAsync(
                    collectionName: _qdrantSettings.Collection,
                    vectorsConfig: new VectorParams
                    {
                        Size = ulong.Parse(_qdrantSettings.Size),
                        Distance = Distance.Cosine
                    }
                );
            }
        }

        public async Task<bool> UpSertItem(List<float> embedding, Dictionary<string, object> payload, long id, CancellationToken cancellationToken = default)
        {
            var pointStruct = new PointStruct
            {
                Id = (ulong)id,
                Vectors = embedding.ToArray()
            };

            foreach (var kvp in payload)
            {
                pointStruct.Payload.Add(kvp.Key, QdrantUtils.ConvertToQdrantValue(kvp.Value));
            }

            var result = await _client.UpsertAsync(
                collectionName: _qdrantSettings.Collection,
                points: new List<PointStruct> { pointStruct }
            );

            return result.Status == UpdateStatus.Completed;
        }

        public async Task<bool> DeleteItem(long id, CancellationToken cancellationToken = default)
        {
            var result = await _client.DeleteAsync(
                collectionName: _qdrantSettings.Collection,
                id: (ulong)id
            );

            return result.Status == UpdateStatus.Completed;
        }

        public async Task<List<QDrantSearchModels>> SearchSimilarityByUserId(string descriptionItem, long userId, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("SearchSimilarityByUserId");
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            var embedding = await _geminiService.EmbeddingText(descriptionItem);
            var searchResult = await _client.SearchAsync(
                collectionName: _qdrantSettings.Collection,
                vector: embedding.ToArray(),
                filter: new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "UserId",
                                Match = new Match
                                {
                                    Integer = userId
                                }
                            }
                        }
                    }
                },
                limit: 2
            );

            var result = new List<QDrantSearchModels>();

            foreach (var searchItem in searchResult)
            {
                if (searchItem.Score > 0.6)
                {
                    var itemId = long.Parse(searchItem.Id.Num.ToString());

                    // Get item directly from repository with includes
                    var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(itemId,
                        include: query => query.Include(x => x.Category)
                                              .Include(x => x.User)
                                              .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                                              .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                                              .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

                    if (item != null)
                    {
                        var itemModel = _mapper.Map<ItemModel>(item);
                        var mappedItem = _mapper.Map<QDrantSearchModels>(itemModel);
                        mappedItem.Score = searchItem.Score;
                        result.Add(mappedItem);
                    }
                }
            }
            sw.Stop();
            Console.WriteLine("SearchSimilarityByUserId " + sw.ElapsedMilliseconds + "ms");
            return result;
        }

        public async Task<List<QDrantSearchModels>> SearchSimilarityItemSystem(string descriptionItem, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
            Console.WriteLine("SearchSimilarityItemSystem");
            var embedding = await _geminiService.EmbeddingText(descriptionItem);
            var searchResult = await _client.SearchAsync(
                collectionName: _qdrantSettings.Collection,
                vector: embedding.ToArray(),
                filter: new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "ItemType",
                                Match = new Match
                                {
                                    Integer = 1
                                }
                            }
                        }
                    }
                },
                limit: 2
            );

            var result = new List<QDrantSearchModels>();

            foreach (var searchItem in searchResult)
            {
                if (searchItem.Score > 0.6)
                {
                    var itemId = long.Parse(searchItem.Id.Num.ToString());

                    // Get item directly from repository with includes
                    var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(itemId,
                        include: query => query.Include(x => x.Category)
                                              .Include(x => x.User)
                                              .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                                              .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                                              .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

                    if (item != null)
                    {
                        var itemModel = _mapper.Map<ItemModel>(item);
                        var mappedItem = _mapper.Map<QDrantSearchModels>(itemModel);
                        mappedItem.Score = searchItem.Score;
                        result.Add(mappedItem);
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine("SearchSimilarityItemSystem " + stopwatch.ElapsedMilliseconds + "ms");
            return result;
        }

        public async Task<List<ItemSearchResult>> SearchItemIdsByUserId(string descriptionItem, long userId, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("SearchItemIdsByUserId");
            var sw = Stopwatch.StartNew();

            var embedding = await _geminiService.EmbeddingText(descriptionItem);
            var searchResult = await _client.SearchAsync(
                collectionName: _qdrantSettings.Collection,
                vector: embedding.ToArray(),
                filter: new Filter
                {
                    Must = 
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "UserId",
                                Match = new Match
                                {
                                    Integer = userId
                                }
                            }
                        }
                    }
                },
                limit: 2
            );

            var result = searchResult != null && searchResult.Any()
                ? searchResult
                    .Where(x => x.Score > 0.6)
                    .Select(x => new ItemSearchResult
                    {
                        ItemId = long.Parse(x.Id.Num.ToString()),
                        Score = x.Score
                    })
                    .ToList()
                : new List<ItemSearchResult>();

            sw.Stop();
            Console.WriteLine($"SearchItemIdsByUserId {sw.ElapsedMilliseconds}ms - Found {result.Count} items");

            return result;
        }
    }
}
