# AnalyzerCore Helm Chart

A Helm chart for deploying AnalyzerCore blockchain analysis API to Kubernetes.

## Prerequisites

- Kubernetes 1.19+
- Helm 3.0+
- PV provisioner support (for persistence)

## Installation

### Add Helm Repository (if published)

```bash
helm repo add analyzercore https://aigen-solutions-s-r-l.github.io/charts
helm repo update
```

### Install from Local Chart

```bash
cd helm/analyzercore
helm install analyzercore . -n analyzercore --create-namespace
```

### Install with Custom Values

```bash
helm install analyzercore . -n analyzercore --create-namespace \
  --set secrets.jwt.secret="your-secret-key" \
  --set config.chain.rpcUrl="your-rpc-url"
```

### Install with Values File

```bash
helm install analyzercore . -n analyzercore --create-namespace \
  -f custom-values.yaml
```

## Configuration

See [values.yaml](values.yaml) for all configurable parameters.

### Key Configuration Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of replicas | `2` |
| `image.repository` | Docker image repository | `ghcr.io/aigen-solutions-s-r-l/analyzer` |
| `image.tag` | Docker image tag | `latest` |
| `config.chain.rpcUrl` | Ethereum RPC URL | `rpc.ankr.com/eth` |
| `config.chain.chainId` | Chain ID | `1` |
| `secrets.jwt.secret` | JWT signing secret | (must be set) |
| `ingress.enabled` | Enable ingress | `false` |
| `autoscaling.enabled` | Enable HPA | `true` |
| `persistence.enabled` | Enable persistence | `true` |

### Ingress Configuration

To enable ingress:

```yaml
ingress:
  enabled: true
  className: nginx
  hosts:
    - host: api.analyzercore.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: analyzercore-tls
      hosts:
        - api.analyzercore.example.com
```

### Telemetry Configuration

Connect to Jaeger and Seq:

```yaml
config:
  telemetry:
    enabled: true
    jaegerEnabled: true
    jaegerAgentHost: "jaeger-agent.monitoring.svc.cluster.local"
  logging:
    seqEnabled: true
    seqServerUrl: "http://seq.monitoring.svc.cluster.local:80"
```

## Upgrade

```bash
helm upgrade analyzercore . -n analyzercore
```

## Uninstall

```bash
helm uninstall analyzercore -n analyzercore
```

## Development

### Template Debugging

```bash
helm template analyzercore . --debug
```

### Lint Chart

```bash
helm lint .
```

### Dry Run

```bash
helm install analyzercore . -n analyzercore --dry-run --debug
```
