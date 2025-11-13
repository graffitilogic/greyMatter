// Stub namespace for deleted components - allows build to pass
namespace GreyMatter.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Concept
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public ConceptType Type { get; set; }
    }

    public class SemanticStorageManager : IDisposable
    {
        public SemanticStorageManager(string basePath) { }
        
        public Task<bool> IndexExistsAsync(string indexType) => Task.FromResult(false);
        
        public Task SaveConceptsBatchAsync(List<Concept> concepts) => Task.CompletedTask;
        
        public void Dispose() { }
    }
}
