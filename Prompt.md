# greyMatter

Neurobiologically-derived experiments in novel machine learning patterns and concepts.

## Purpose

Small-scale exerimentation around bridging some of the gaps between standard artificial neural
networks and their biological counterparts.

## North Star

Every design, architecture, information processing challenge related to cognitive function 
already has an answer in nature.   In some cases we may lack the complete understanding
necessary to capatialize on these solutions.  In some cases we may find the biologically equivalent 
algorithms to be suboptimal, misaligned with neighboring concepts or difficult to emulate
on account of scale.  In these cases, we will look for compatible solutions in technology
such as game engine concepts such as render-distance lazy loading, procedural generation,
compression, vector storage, etc.  

## Conceptual LEGOS
SNN, STDP, hebbian learning, sparse encoding, procedural generation, lazy loading,
interneuron inhibition, SDM

## Assertions and Rebuttals

Assertion:
The Von Neumann Bottleneck:   One structural challenge to bridging the divide between artificial
and biological neural networks is the memory-wall problem.   In the brain, processing and memory
are the same thing.    Moving trillions of parameters between memory, cpu and persistent storage is 
heavily resource dependant. 

Rebuttal:  
By modeling our neurons, synaptic connections to more closely mimick biology, we can model
cortical columns that emulate the same "processing/memory" shared role characteristic.  

Assertion:
Biological brains provide for billions-wide neuron scale and trillions-wide paralallism.
Equating this in computational terms is an arms race for resources.

Rebuttal:
LLM's are already trillions-wide in parameterization and even a CPU (let alone a GPU) is many-
thousands of times faster than my brain's clockspeed.   In practice, it took my childhood brain
years in reinforced conditioning to establish a basis of basic concepts.  The speed difference
is a force multiplier in favor of computational processing, not a detriment.  I assert this 
to be more of an algorithmic problem than a resource/scale difference.   In practical terms,
if we borrow from procedural generation and other just-in-time lifecycle patterns, we
should be able to emulate a massive scale of artificial neurons within the limited scope 
of a learning or recall cycle.  


## Deliverables

A system of short-lived neurons and synapses that are only needed for the scope of activation.
A minimalist storage scheme of activation recipes / conceptual engrams and a lookup scheme to 
determine which recipes may aide specific concepts. Store only what is absolutely needed and consider recall-matching 
in determining what is stored and how. Feel free to employ long term/ short term memory concepts but remember: the neurons
and their synapses ARE the data and the processor.   We will be trading recall accuracy for scale.   
A learning pipeline to ingest concepts, a testing pipeline to test recall under various scenarios. 
(Local wiki, sentence data, a nearby network LLM API can be re-enabled)

Configurable parameters to scale the neural network baseline size, activation dept and size.

## Guardrails

This was a failure if: 
It stores wordlists and concepts directly to disc.

If it only operates at hundreds wide and dozens deep in term of neural architecture

This was a success if: 
We can train on a random dataset and test the neural network performance for recall based on 
various neural-network scales.

If a concept can activate a comparable synaptic graph to a biologically brain by leveraging just-in-time
activation and balancing speed and parallelism on commodity hardware. 
