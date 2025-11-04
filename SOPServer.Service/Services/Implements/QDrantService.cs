using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using SOPServer.Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class QDrantService : IQdrantService
    {
        private readonly QDrantClientSettings _qdrantSettings;
        private readonly QdrantClient _client;

        public QDrantService(IOptions<QDrantClientSettings> qdrantClientSettings)
        {
            _qdrantSettings = qdrantClientSettings.Value;
            _client = new QdrantClient(
                        host: qdrantClientSettings.Value.Host,
                        port: qdrantClientSettings.Value.Port,
                        apiKey: qdrantClientSettings.Value.SecretKey,
                        https: false
            );
        }

        public async Task EnsureCollectionExistsAsync()
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

        public async Task<bool> UpSertItem(List<float> embedding, Dictionary<string, object> payload, long id)
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

        public async Task<bool> DeleteItem(long id)
        {
            var result = await _client.DeleteAsync(
                collectionName: _qdrantSettings.Collection,
                id: (ulong) id
            );

            return result.Status == UpdateStatus.Completed;
        }

        public async Task<List<QDrantSearchModels>> SearchSimilarityByUserId(List<float> embedding, long userId, SlotItem slotItem)
        {
            List<QDrantSearchModels> result = new List<QDrantSearchModels>();
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
                                    Keyword = userId.ToString()
                                }
                            }
                        },
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "Category",
                                Match = new Match
                                {
                                    Keyword = slotItem.ToString()
                                }
                            }
                        }
                    }
                },
                limit: 5
            );

            foreach (var item in searchResult)
            {
                if(item.Score > 0.6)
                {
                    result.Add(new QDrantSearchModels
                    {
                        id = int.Parse(item.Id.ToString()),
                        score = item.Score
                    });
                }
                
            }

            return result;
        }
    }
}
