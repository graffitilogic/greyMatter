using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace greyMatter.Core
{
    /// <summary>
    /// Interface for bidirectional communication between cortical columns and learning brain.
    /// Enables biologically-aligned integration where:
    /// - Columns can trigger persistent learning (Column → Brain)
    /// - Columns can query existing knowledge (Brain → Column)
    /// 
    /// Mirrors biological cortical column ↔ synaptic learning communication.
    /// </summary>
    public interface IIntegratedBrain
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Column → Brain: Pattern-Triggered Learning
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Notify brain of significant pattern detected in column activity.
        /// Triggers Hebbian learning when columns reach consensus on a pattern.
        /// Biological analogy: Cortical column activity triggers long-term potentiation.
        /// </summary>
        Task NotifyColumnPatternAsync(ColumnPattern pattern);
        
        /// <summary>
        /// Strengthen association between two concepts based on column co-activation.
        /// Implements spike-timing dependent plasticity principle.
        /// Biological analogy: Neurons that fire together, wire together.
        /// </summary>
        Task StrengthenAssociationAsync(string concept1, string concept2, double strength);
        
        /// <summary>
        /// Register a novel word detected by column consensus.
        /// Adds to vocabulary if confidence is high enough.
        /// Biological analogy: Novel stimuli create new neural representations.
        /// </summary>
        Task RegisterWordFromColumnsAsync(string word, double confidence);
        
        // ═══════════════════════════════════════════════════════════════════════
        // Brain → Column: Knowledge-Guided Processing
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Query existing knowledge about a concept.
        /// Returns familiarity, frequency, associations, and connection strength.
        /// Biological analogy: Columns query existing synaptic weights.
        /// </summary>
        Task<ConceptKnowledge> QueryKnowledgeAsync(string word);
        
        /// <summary>
        /// Get related concepts through learned associations.
        /// Used by columns for context-aware message routing.
        /// Biological analogy: Spreading activation through associative memory.
        /// </summary>
        Task<List<string>> GetRelatedConceptsAsync(string word, int maxResults = 10);
        
        /// <summary>
        /// Get familiarity score for a word (0.0 = unknown, 1.0 = very familiar).
        /// Fast check for boosting activation of known concepts.
        /// Biological analogy: Recognition strength from existing connections.
        /// </summary>
        Task<double> GetWordFamiliarityAsync(string word);
        
        /// <summary>
        /// Fast check if word exists in vocabulary.
        /// Optimized for high-frequency queries from columns.
        /// </summary>
        bool IsKnownWord(string word);
        
        // ═══════════════════════════════════════════════════════════════════════
        // Integration Statistics
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Get statistics about integration activity.
        /// Tracks Column→Brain triggers, Brain→Column queries, and synergy metrics.
        /// </summary>
        IntegrationStats GetIntegrationStats();
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // Supporting Data Structures
    // ═══════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Pattern detected by column consensus
    /// </summary>
    public class ColumnPattern
    {
        public string PrimaryConcept { get; set; } = "";
        public List<string> RelatedConcepts { get; set; } = new List<string>();
        public double Confidence { get; set; }
        public int ColumnCount { get; set; }
        public int MessageCount { get; set; }
        public string Context { get; set; } = "";
        public PatternType Type { get; set; }
        public DateTime DetectedAt { get; set; }
    }
    
    /// <summary>
    /// Type of pattern detected
    /// </summary>
    public enum PatternType
    {
        /// <summary>New word never seen before</summary>
        NovelWord,
        
        /// <summary>New connection between known concepts</summary>
        NewAssociation,
        
        /// <summary>Reinforcement of existing pattern</summary>
        Reinforcement,
        
        /// <summary>Syntactic structure pattern</summary>
        SyntacticStructure,
        
        /// <summary>Semantic clustering pattern</summary>
        SemanticCluster,
        
        /// <summary>Cross-domain conceptual link</summary>
        CrossDomainLink
    }
    
    /// <summary>
    /// Existing knowledge about a concept
    /// </summary>
    public class ConceptKnowledge
    {
        public string Concept { get; set; } = "";
        public double Familiarity { get; set; }
        public int Frequency { get; set; }
        public DateTime LastSeen { get; set; }
        public List<(string concept, double strength)> Associations { get; set; } = new List<(string, double)>();
        public List<int> NeuronIds { get; set; } = new List<int>();
        public double ConnectionStrength { get; set; }
    }
    
    /// <summary>
    /// Statistics tracking integration activity
    /// </summary>
    public class IntegrationStats
    {
        // Column → Brain metrics
        public int PatternsNotified { get; set; }
        public int AssociationsStrengthened { get; set; }
        public int WordsRegisteredFromColumns { get; set; }
        public int LearningTriggersTotal { get; set; }
        
        // Brain → Column metrics
        public int KnowledgeQueriesTotal { get; set; }
        public int KnowledgeHits { get; set; }
        public int KnowledgeMisses { get; set; }
        public int RelatedConceptsRequests { get; set; }
        public int FamiliarityChecks { get; set; }
        
        // Synergy metrics
        public double KnowledgeUtilizationRate => 
            KnowledgeQueriesTotal > 0 ? (double)KnowledgeHits / KnowledgeQueriesTotal : 0.0;
        
        public double LearningEfficiency =>
            PatternsNotified > 0 ? (double)LearningTriggersTotal / PatternsNotified : 0.0;
        
        // Timestamps
        public DateTime IntegrationStarted { get; set; }
        public DateTime LastActivity { get; set; }
        
        public override string ToString()
        {
            return $"Integration Stats:\n" +
                   $"  Column→Brain: {PatternsNotified} patterns, {AssociationsStrengthened} associations, {WordsRegisteredFromColumns} words\n" +
                   $"  Brain→Column: {KnowledgeQueriesTotal} queries ({KnowledgeHits} hits, {KnowledgeMisses} misses)\n" +
                   $"  Synergy: {KnowledgeUtilizationRate:P1} knowledge use, {LearningEfficiency:P1} learning efficiency";
        }
    }
}
