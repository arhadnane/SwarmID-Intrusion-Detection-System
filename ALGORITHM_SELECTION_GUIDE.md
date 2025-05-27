# SwarmID Algorithm Selection Guide

## How Algorithm Selection Works

The SwarmID system supports **three different swarm intelligence algorithms** for anomaly detection:

1. **ACO** - Ant Colony Optimization
2. **BEE** - Bee Algorithm  
3. **PSO** - Particle Swarm Optimization

## ❗ Important: Only ONE Algorithm Runs at a Time

**The system does NOT use all algorithms simultaneously.** Instead, you choose which algorithm to use via configuration, and that single algorithm handles all threat detection.

## Current Configuration

Both API and Dashboard are currently configured to use **PSO (Particle Swarm Optimization)**:

### API Configuration (`SwarmID.Api/appsettings.json`):
```json
{
  "SwarmAlgorithm": "PSO",
  "SwarmConfiguration": {
    "NumberOfParticles": 30,
    "MaxIterations": 100,
    "InertiaWeight": 0.729,
    "CognitiveComponent": 1.494,
    "SocialComponent": 1.494,
    "MinInertiaWeight": 0.4,
    "MaxVelocity": 4.0,
    "AnomalyThreshold": 0.7,
    "FeedbackWeight": 0.1
  }
}
```

### Dashboard Configuration (`SwarmID.Dashboard/appsettings.json`):
```json
{
  "SwarmAlgorithm": "PSO",
  "SwarmConfiguration": {
    "NumberOfParticles": 30,
    "MaxIterations": 100,
    "InertiaWeight": 0.729,
    "CognitiveComponent": 1.494,
    "SocialComponent": 1.494,
    "MinInertiaWeight": 0.4,
    "MaxVelocity": 4.0,
    "AnomalyThreshold": 0.7,
    "FeedbackWeight": 0.1
  }
}
```

## How to Change the Algorithm

To switch to a different algorithm, modify the `SwarmAlgorithm` value in both configuration files:

### Option 1: Use Ant Colony Optimization (ACO)
```json
{
  "SwarmAlgorithm": "ACO",
  "SwarmConfiguration": {
    "NumberOfAnts": 50,
    "MaxIterations": 100,
    "PheromoneEvaporationRate": 0.1,
    "AnomalyThreshold": 70.0,
    "Alpha": 1.0,
    "Beta": 2.0
  }
}
```

### Option 2: Use Bee Algorithm (BEE)
```json
{
  "SwarmAlgorithm": "BEE",
  "SwarmConfiguration": {
    "NumberOfEmployedBees": 20,
    "NumberOfOnlookerBees": 30,
    "MaxTrialCount": 10,
    "AcceptanceProbability": 0.8,
    "MaxIterations": 100,
    "AnomalyThreshold": 70.0
  }
}
```

### Option 3: Use Particle Swarm Optimization (PSO) - Current
```json
{
  "SwarmAlgorithm": "PSO",
  "SwarmConfiguration": {
    "NumberOfParticles": 30,
    "MaxIterations": 100,
    "InertiaWeight": 0.729,
    "CognitiveComponent": 1.494,
    "SocialComponent": 1.494,
    "MinInertiaWeight": 0.4,
    "MaxVelocity": 4.0,
    "AnomalyThreshold": 0.7
  }
}
```

## Algorithm Selection Logic

The system uses dependency injection to select the algorithm:

```csharp
// In Program.cs
var algorithmType = builder.Configuration["SwarmAlgorithm"] ?? "ACO";

return algorithmType.ToUpper() switch
{
    "ACO" => new AntColonyOptimizationDetector(repository),
    "BEE" => new BeeAlgorithmDetector(repository), 
    "PSO" => new ParticleSwarmOptimizationDetector(repository),
    _ => new AntColonyOptimizationDetector(repository) // Default fallback
};
```

## Threat Detection Coverage

**All three algorithms detect the same types of threats:**

- ✅ **Port Scan Detection**
- ✅ **DDoS Attack Detection** 
- ✅ **Command & Control (C2) Communication**
- ✅ **Data Exfiltration**
- ✅ **Normal Traffic Classification**

The difference is in **how** they optimize the detection thresholds:

### PSO (Current)
- Uses particles that move through threshold space
- Balances personal best and global best solutions
- Good for continuous optimization problems

### ACO 
- Uses ant pheromone trails to find optimal paths
- Good for discrete optimization problems
- Mimics natural ant foraging behavior

### BEE
- Uses employed and onlooker bees
- Exploits good solutions and explores new areas
- Balances exploitation and exploration

## Recommendations

1. **For Production**: Start with **PSO** (current setting) as it's well-tuned
2. **For Comparison**: Test different algorithms with same traffic data
3. **For Specific Networks**: 
   - **PSO**: General-purpose networks
   - **ACO**: Networks with clear attack patterns
   - **BEE**: Networks requiring balanced exploration

## Steps to Change Algorithm

1. **Stop the applications** (API and Dashboard)
2. **Edit both appsettings.json files** 
3. **Change `"SwarmAlgorithm": "PSO"` to `"ACO"` or `"BEE"`**
4. **Update SwarmConfiguration parameters** for the chosen algorithm
5. **Restart the applications**

## Verification

To verify which algorithm is active:
- Check the anomaly detection results - they will show the algorithm name
- Look at the `Algorithm` field in detected anomalies
- PSO results will show "Particle Swarm Optimization"
- ACO results will show "Ant Colony Optimization"  
- BEE results will show "Bee Algorithm"

## Current Status
✅ **Currently Running**: PSO (Particle Swarm Optimization)
✅ **Threat Coverage**: All major network threats
✅ **Configuration**: Optimized for real-time detection
