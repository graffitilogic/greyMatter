using System;
using System.Collections.Generic;

namespace GreyMatter.Core
{
    /// <summary>
    /// Synapse: Represents connections between neurons with dynamic properties
    /// Handles weight adjustments, signal transmission, and connection strength
    /// </summary>
    public class Synapse
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PresynapticNeuronId { get; private set; }
        public Guid PostsynapticNeuronId { get; private set; }
        
        // Connection properties
        public double Weight { get; set; }
        public double Strength { get; private set; } = 1.0; // Biological connection strength
        public SynapseType Type { get; set; } = SynapseType.Excitatory;
        
        // Activity tracking
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public int TransmissionCount { get; private set; } = 0;
        public double AverageSignalStrength { get; private set; } = 0.0;
        
        // Plasticity properties
        public double PlasticityRate { get; set; } = 0.01;
        public double MinWeight { get; set; } = -1.0;
        public double MaxWeight { get; set; } = 1.0;
        public bool IsPlastic { get; set; } = true;
        
        // Transmission delay (biological realism)
        public TimeSpan TransmissionDelay { get; set; } = TimeSpan.FromMilliseconds(1);
        
        public Synapse(Guid presynapticId, Guid postsynapticId, double initialWeight = 0.1, SynapseType type = SynapseType.Excitatory)
        {
            PresynapticNeuronId = presynapticId;
            PostsynapticNeuronId = postsynapticId;
            Weight = initialWeight;
            Type = type;
        }

        /// <summary>
        /// Transmit signal from presynaptic to postsynaptic neuron
        /// </summary>
        public double TransmitSignal(double presynapticOutput)
        {
            LastActive = DateTime.UtcNow;
            TransmissionCount++;
            
            // Apply synaptic transformation
            double signal = presynapticOutput * Weight * Strength;
            
            // Apply synaptic type modifier
            signal = Type switch
            {
                SynapseType.Inhibitory => -Math.Abs(signal),
                SynapseType.Excitatory => Math.Abs(signal),
                SynapseType.Modulatory => signal * 0.5, // Weaker but can be positive or negative
                _ => signal
            };
            
            // Update running average of signal strength
            UpdateAverageSignalStrength(Math.Abs(signal));
            
            return signal;
        }

        /// <summary>
        /// Learn and adjust weight based on presynaptic and postsynaptic activity
        /// Implements Hebbian learning with STDP (Spike-Timing Dependent Plasticity)
        /// </summary>
        public void Learn(double presynapticActivity, double postsynapticActivity, TimeSpan timingDifference)
        {
            if (!IsPlastic) return;
            
            // Hebbian learning: "Neurons that fire together, wire together"
            double hebbianChange = PlasticityRate * presynapticActivity * postsynapticActivity;
            
            // STDP: Timing matters
            double stdpModifier = CalculateSTDPModifier(timingDifference);
            
            // Apply weight change
            double weightChange = hebbianChange * stdpModifier;
            Weight = Math.Clamp(Weight + weightChange, MinWeight, MaxWeight);
            
            // Update connection strength based on usage
            UpdateConnectionStrength();
        }

        /// <summary>
        /// Spike-Timing Dependent Plasticity modifier
        /// </summary>
        private double CalculateSTDPModifier(TimeSpan timingDifference)
        {
            double dt = timingDifference.TotalMilliseconds;
            
            // If presynaptic fires before postsynaptic (dt > 0), strengthen connection
            // If postsynaptic fires before presynaptic (dt < 0), weaken connection
            if (dt > 0)
            {
                return Math.Exp(-dt / 20.0); // Exponential decay, 20ms time constant
            }
            else
            {
                return -Math.Exp(dt / 20.0); // Negative for weakening
            }
        }

        /// <summary>
        /// Update connection strength based on usage patterns
        /// </summary>
        private void UpdateConnectionStrength()
        {
            // Strength increases with usage but decays over time
            double usageBoost = Math.Log(TransmissionCount + 1) / 100.0;
            double timeDecay = (DateTime.UtcNow - LastActive).TotalDays / 100.0;
            
            Strength = Math.Clamp(1.0 + usageBoost - timeDecay, 0.1, 2.0);
        }

        /// <summary>
        /// Update running average of signal strength
        /// </summary>
        private void UpdateAverageSignalStrength(double currentSignal)
        {
            double alpha = 0.1; // Smoothing factor
            AverageSignalStrength = alpha * currentSignal + (1 - alpha) * AverageSignalStrength;
        }

        /// <summary>
        /// Determine if synapse should be pruned
        /// </summary>
        public bool ShouldBePruned(double weightThreshold = 0.001, int daysInactive = 30)
        {
            bool tooWeak = Math.Abs(Weight) < weightThreshold;
            bool tooOld = (DateTime.UtcNow - LastActive).TotalDays > daysInactive;
            bool lowActivity = AverageSignalStrength < 0.01;
            
            return (tooWeak && tooOld) || (tooWeak && lowActivity);
        }

        /// <summary>
        /// Age the synapse - natural decay over time
        /// </summary>
        public void Age(TimeSpan timePassed)
        {
            if (!IsPlastic) return;
            
            // Natural weight decay
            double decayRate = 0.001 * timePassed.TotalDays;
            Weight *= (1.0 - decayRate);
            
            // Strength decay
            double strengthDecay = 0.01 * timePassed.TotalDays;
            Strength = Math.Max(0.1, Strength - strengthDecay);
        }

        /// <summary>
        /// Create snapshot for persistence
        /// </summary>
        public SynapseSnapshot CreateSnapshot()
        {
            return new SynapseSnapshot
            {
                Id = Id,
                PresynapticNeuronId = PresynapticNeuronId,
                PostsynapticNeuronId = PostsynapticNeuronId,
                Weight = Weight,
                Strength = Strength,
                Type = Type,
                LastActive = LastActive,
                TransmissionCount = TransmissionCount,
                AverageSignalStrength = AverageSignalStrength,
                PlasticityRate = PlasticityRate,
                IsPlastic = IsPlastic
            };
        }

        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public static Synapse FromSnapshot(SynapseSnapshot snapshot)
        {
            var synapse = new Synapse(snapshot.PresynapticNeuronId, snapshot.PostsynapticNeuronId, snapshot.Weight, snapshot.Type)
            {
                Strength = snapshot.Strength,
                LastActive = snapshot.LastActive,
                TransmissionCount = snapshot.TransmissionCount,
                AverageSignalStrength = snapshot.AverageSignalStrength,
                PlasticityRate = snapshot.PlasticityRate,
                IsPlastic = snapshot.IsPlastic
            };
            
            return synapse;
        }

        public override string ToString()
        {
            return $"Synapse[{PresynapticNeuronId:N}→{PostsynapticNeuronId:N}] W:{Weight:F3} S:{Strength:F3} T:{Type}";
        }
    }

    /// <summary>
    /// Types of synaptic connections
    /// </summary>
    public enum SynapseType
    {
        Excitatory,   // Increases postsynaptic activity
        Inhibitory,   // Decreases postsynaptic activity  
        Modulatory    // Modifies the strength of other connections
    }

    /// <summary>
    /// Lightweight synapse data for persistence
    /// </summary>
    public class SynapseSnapshot
    {
        public Guid Id { get; set; }
        public Guid PresynapticNeuronId { get; set; }
        public Guid PostsynapticNeuronId { get; set; }
        public double Weight { get; set; }
        public double Strength { get; set; }
        public SynapseType Type { get; set; }
        public DateTime LastActive { get; set; }
        public int TransmissionCount { get; set; }
        public double AverageSignalStrength { get; set; }
        public double PlasticityRate { get; set; }
        public bool IsPlastic { get; set; }
    }
}