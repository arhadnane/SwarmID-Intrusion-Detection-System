# SwarmID Real-World Deployment & Testing Guide

## 🚀 Quick Start for Real-World Testing

### Step 1: Clone and Build
```powershell
# Clone the repository
git clone https://github.com/arhadnane/SwarmID.git
cd "Anomaly-Based Intrusion Detection System Using Swarm Intelligence"

# Restore dependencies and build
dotnet restore
dotnet build
```

### Step 2: Run the System
```powershell
# Option 1: Run both services simultaneously (Recommended)
# Terminal 1 - Start API Backend
cd SwarmID.Api
dotnet run

# Terminal 2 - Start Dashboard Frontend
cd SwarmID.Dashboard
dotnet run
```

**Access URLs:**
- 🌐 **Dashboard**: http://localhost:5121
- 🔧 **API**: http://localhost:5112
- 📚 **API Documentation**: http://localhost:5112/swagger

### Step 3: Test with Sample Data

## 📊 Sample Data Files

The system includes realistic sample data for testing:

### 1. Zeek Connection Logs (`sample-data/zeek-conn.log`)
Real-world network connection logs in Zeek format:
```
1640995200.123456	C1a2b3c4	192.168.1.100	50234	8.8.8.8	53	udp	dns	0.123	64	84	SF	-	-	0	DdAa	1	84	1	148
1640995201.456789	C2b3c4d5	192.168.1.101	80443	10.0.0.1	22	tcp	ssh	300.456	1024	2048	S0	-	-	0	Sa	3	1536	2	3072
1640995202.789012	C3c4d5e6	172.16.0.50	1433	192.168.1.200	54321	tcp	-	0.789	0	0	REJ	-	-	0	R	1	0	1	40
```

### 2. Snort Alert Logs (`sample-data/snort-alerts.log`)
Security alerts from Snort IDS:
```
[**] [1:2001219:20] ET SCAN Potential SSH Scan [**]
[Classification: Attempted Information Leak] [Priority: 2]
12/31-23:59:58.123456 192.168.1.100:22345 -> 10.0.0.1:22
TCP TTL:64 TOS:0x0 ID:12345 IpLen:20 DgmLen:60 DF
***A*S** Seq: 0x12345678  Ack: 0x0  Win: 0x7210  TcpLen: 40

[**] [1:2002910:5] ET POLICY Suspicious inbound to mySQL port 3306 [**]
[Classification: Potentially Bad Traffic] [Priority: 2]
12/31-23:59:59.654321 10.0.0.100:54321 -> 192.168.1.200:3306
TCP TTL:48 TOS:0x0 ID:54321 IpLen:20 DgmLen:1500
***AP*** Seq: 0x87654321  Ack: 0x12345678  Win: 0x4000  TcpLen: 20
```

### 3. PCAP Sample Data
Small PCAP file for packet analysis testing (binary format).

## 🛠️ Testing Scenarios

### Scenario 1: Normal Traffic Analysis
1. **Upload Zeek Logs**: Use the dashboard to upload `zeek-conn.log`
2. **Monitor Processing**: Watch real-time analysis in Traffic tab
3. **Review Results**: Check for normal traffic classification

### Scenario 2: Attack Detection
1. **Upload Snort Alerts**: Load `snort-alerts.log` containing attack signatures
2. **Algorithm Analysis**: Both ACO and Bee algorithms will analyze patterns
3. **Anomaly Detection**: System should detect port scans and suspicious connections
4. **Dashboard Alerts**: Real-time alerts appear in Anomalies tab

### Scenario 3: PCAP Analysis
1. **Upload PCAP**: Use packet capture files for deep packet inspection
2. **Protocol Analysis**: System extracts TCP/UDP/ICMP details
3. **Pattern Recognition**: Swarm algorithms identify unusual patterns

### Scenario 4: Real-time Monitoring
1. **Start Monitoring**: Enable real-time traffic simulation
2. **Live Analysis**: Watch generated traffic patterns
3. **Anomaly Injection**: System occasionally generates suspicious traffic
4. **Real-time Alerts**: SignalR updates dashboard instantly

## 📁 Sample Data Structure

Create this folder structure in your project:
```
sample-data/
├── zeek-logs/
│   ├── conn.log              # Connection logs
│   ├── dns.log               # DNS queries
│   ├── http.log              # HTTP traffic
│   └── ssl.log               # SSL/TLS connections
├── snort-alerts/
│   ├── alert.log             # Alert messages
│   ├── port-scan.log         # Port scan alerts
│   └── malware.log           # Malware detection
├── pcap-files/
│   ├── normal-traffic.pcap   # Baseline traffic
│   ├── port-scan.pcap        # Port scanning activity
│   └── ddos-attack.pcap      # DDoS simulation
└── README.md                 # Data descriptions
```

## 🔧 Configuration for Production

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data=Production/swarmid.db",
    "LogConnection": "Data=Production/swarmid-log.db"
  }
}
```

### Algorithm Tuning
```json
{
  "SwarmConfiguration": {
    "AnomalyThreshold": 75.0,
    "NumberOfAnts": 100,
    "NumberOfEmployedBees": 50,
    "MaxIterations": 200,
    "PheromoneEvaporationRate": 0.05
  }
}
```

### Performance Settings
```json
{
  "Performance": {
    "MaxConcurrentAnalysis": 10,
    "BatchSize": 1000,
    "MonitoringInterval": 5000,
    "AlertThreshold": 0.8
  }
}
```

## 🐛 Troubleshooting

### Common Issues

**1. Port Already in Use**
```powershell
# Check what's using the port
netstat -ano | findstr :5121
# Kill the process if needed
taskkill /PID <process_id> /F
```

**2. Database Lock Issues**
```powershell
# Stop all instances
Get-Process "*SwarmID*" | Stop-Process
# Restart with clean database
```

**3. PCAP Parsing Errors**
- Ensure PCAP files are valid format
- Check file permissions
- Verify SharpPcap dependencies are installed

### Log Locations
- **API Logs**: `SwarmID.Api/Logs/`
- **Dashboard Logs**: `SwarmID.Dashboard/Logs/`
- **Database Files**: `SwarmID.Dashboard/Data/`

## 📈 Performance Monitoring

### Key Metrics to Watch
- **Memory Usage**: Should stay under 500MB
- **CPU Usage**: Typically 10-30% during analysis
- **Processing Speed**: 100-200ms per traffic record
- **Detection Accuracy**: Monitor false positive rates

### Real-time Dashboard Metrics
- Active connections count
- Anomalies detected per hour
- Algorithm performance comparison
- System resource utilization

## 🔒 Security Considerations

### Production Deployment
1. **Enable HTTPS**: Configure SSL certificates
2. **Authentication**: Implement user authentication
3. **Access Control**: Restrict API access
4. **Data Encryption**: Encrypt sensitive data at rest
5. **Network Security**: Use firewalls and VPNs

### Sample Production Configuration
```json
{
  "Security": {
    "RequireHttps": true,
    "EnableAuthentication": true,
    "JwtSecret": "your-secret-key",
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

## 🚀 Docker Deployment (Optional)

### Dockerfile Example
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SwarmID.Api.dll"]
```

### Docker Compose
```yaml
version: '3.8'
services:
  swarmid-api:
    build: .
    ports:
      - "5112:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
  
  swarmid-dashboard:
    build: ./SwarmID.Dashboard
    ports:
      - "5121:80"
    depends_on:
      - swarmid-api
```

## 📞 Support & Next Steps

1. **Test with Sample Data**: Start with provided sample files
2. **Monitor Performance**: Watch system metrics during testing
3. **Tune Algorithms**: Adjust parameters based on your network
4. **Scale Up**: Add more data sources and increase processing capacity
5. **Integrate**: Connect with existing security tools and SIEM systems

For additional support:
- 📧 Check logs for detailed error messages
- 🐛 Report issues with sample data and error logs
- 📖 Review API documentation at `/swagger` endpoint
