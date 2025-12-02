# AnalyzerCore Monitoring Stack

This directory contains the monitoring configuration for AnalyzerCore, including Prometheus, Grafana, Jaeger, and Seq.

## Quick Start

```bash
cd monitoring
docker-compose up -d
```

## Services

| Service | URL | Description |
|---------|-----|-------------|
| Prometheus | http://localhost:9090 | Metrics storage and querying |
| Grafana | http://localhost:3000 | Dashboards and visualization |
| Jaeger | http://localhost:16686 | Distributed tracing |
| Seq | http://localhost:5341 | Log aggregation |

## Default Credentials

- **Grafana**: admin / admin
- **Seq**: No authentication by default

## Dashboards

Three pre-configured dashboards are available:

1. **API Overview** - HTTP request rates, latencies, error rates, status codes
2. **Blockchain Metrics** - RPC calls, block processing, pool/token counts
3. **Infrastructure** - Outbox, rate limiting, caching, .NET runtime metrics

## Alerting Rules

Prometheus alert rules are configured for:

- High error rate (>5%)
- High latency (p95 >2s)
- Service down
- Block lag (>50 warning, >100 critical)
- RPC errors
- Outbox backlog
- Rate limit rejections
- High memory usage
- Low cache hit rate

## Connecting AnalyzerCore

Ensure your `appsettings.json` has the following configuration:

```json
{
  "Telemetry": {
    "Enabled": true,
    "PrometheusEnabled": true,
    "JaegerEnabled": true,
    "JaegerAgentHost": "localhost",
    "JaegerAgentPort": 6831
  },
  "Logging": {
    "SeqEnabled": true,
    "SeqServerUrl": "http://localhost:5341"
  }
}
```

## File Structure

```
monitoring/
├── docker-compose.yml           # Docker Compose for all services
├── prometheus/
│   ├── prometheus.yml          # Prometheus configuration
│   └── alert-rules.yml         # Alerting rules
└── grafana/
    ├── dashboards/             # Dashboard JSON files
    │   ├── api-overview.json
    │   ├── blockchain-metrics.json
    │   └── infrastructure.json
    └── provisioning/
        ├── datasources/
        │   └── datasources.yml # Prometheus & Jaeger datasources
        └── dashboards/
            └── dashboards.yml  # Dashboard provisioning config
```

## Customization

### Adding New Alerts

Edit `prometheus/alert-rules.yml` to add new alerting rules.

### Adding New Dashboards

1. Create a new JSON file in `grafana/dashboards/`
2. The dashboard will be auto-provisioned on Grafana restart

### Modifying Scrape Targets

Edit `prometheus/prometheus.yml` to add or modify scrape targets.
