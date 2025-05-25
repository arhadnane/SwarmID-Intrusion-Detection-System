# Swarm-ID: Anomaly-Based Intrusion Detection System

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# .NET 8 solution implementing an anomaly-based intrusion detection system using swarm intelligence algorithms (Ant Colony Optimization and Bee Algorithm) for network traffic analysis.

## Architecture
- **SwarmID.Core**: Core models, interfaces, and swarm intelligence algorithms
- **SwarmID.TrafficAnalysis**: Network traffic parsing and feature extraction from Zeek logs, Snort alerts, and PCAP files
- **SwarmID.Api**: ASP.NET Core Web API for backend services
- **SwarmID.Dashboard**: Blazor Server application for real-time monitoring and administration
- **SwarmID.Tests**: Unit and integration tests

## Key Technologies
- .NET 8
- ASP.NET Core Web API
- Blazor Server
- SignalR for real-time updates
- LiteDB for lightweight data storage
- ML.NET for baseline machine learning models

## Coding Guidelines
- Follow SOLID principles and clean architecture patterns
- Use dependency injection throughout the application
- Implement proper error handling and logging
- Create comprehensive unit tests for all business logic
- Use async/await patterns for I/O operations
- Implement proper data validation and sanitization

## Swarm Intelligence Implementation
- Focus on performance-optimized algorithms for real-time analysis
- Implement configurable parameters for algorithm tuning
- Provide clear abstractions for different swarm intelligence approaches
- Include detailed documentation for algorithm logic and parameters

## Security Considerations
- Validate all network traffic input data
- Implement proper authentication and authorization
- Use secure coding practices for handling sensitive network data
- Log security events appropriately
