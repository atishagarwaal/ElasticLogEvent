<img width="148" height="23" alt="image" src="https://github.com/user-attachments/assets/bb2d138a-6520-4c98-8295-abb49942948d" /># 📘 End-to-End Logging with .NET, Kafka (KRaft), and ELK (Elasticsearch, Logstash, Kibana)

## 1. Introduction

Modern applications generate large volumes of logs that need to be centralized, structured, and visualized for monitoring and troubleshooting.  

This guide demonstrates how to set up a complete logging pipeline where:

- A .NET application produces structured logs.  
- Logs are sent into Kafka (KRaft mode) for buffering and transport.  
- Logstash consumes logs from Kafka and forwards them into Elasticsearch.  
- Kibana provides visualization and search capabilities.  

The entire stack runs locally with:

- Kafka in KRaft mode (ZooKeeper-less Kafka).  
- Elasticsearch, Logstash, Kibana (ELK) installed individually on the host system (not Docker).  

---

## 2. Architecture Overview

```
.NET App → Kafka (KRaft) → Logstash → Elasticsearch → Kibana
```

- **Kafka (KRaft):** Message broker for decoupling log producers and consumers.  
- **Logstash:** Ingests logs from Kafka, enriches/transforms, and ships them to Elasticsearch.  
- **Elasticsearch:** Stores logs in time-based indices for search and analysis.  
- **Kibana:** User-friendly UI for dashboards, visualizations, and querying logs.  

---

## 3. Step 1: Run Kafka in KRaft Mode

Run Kafka in KRaft mode using Docker (no ZooKeeper required):

```bash
docker run --name kafka -d -p 9093:9092 bashj79/kafka-kraft
```

Remove the container named kafka is already present and run the above command
```bash
docker rm -f sym-kafka
```

Check logs to ensure Kafka started successfully:

```bash
docker logs kafka
```

Monitor logs in the topic (in a separate terminal):

```bash
docker ps
docker run --network host -it --rm bashj79/kafka-kraft /bin/bash -c "/opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9093 --topic app-logs"
```

---

## 4. Step 2: Install ELK Stack Locally

Download and install the following individually from Elastic Downloads:

- [Elasticsearch](https://www.elastic.co/downloads/elasticsearch)  
- [Logstash](https://www.elastic.co/downloads/logstash)  
- [Kibana](https://www.elastic.co/downloads/kibana)  

---

## 5. Step 3: Configure Logstash

### 5.1 Enable config file reading in `logstash.yml` under `logstash/config/`:

```yaml
path.config: "C:/Program Files/logstash-9.1.2/config/logstash.conf"
config.reload.automatic: true
log.level: debug
```

### 5.2 Create a pipeline config file `logstash.conf` under `logstash/config/`:

```conf
input {
  kafka {
    type => "kafka"
    bootstrap_servers => "localhost:9093"
    topics => ["app-logs"]
    codec => json {}
  }
}

filter {
}

output {
  elasticsearch {
    hosts => ["https://localhost:9200"]
    index => "app-logs-%{+YYYY.MM.dd}"
    user => "elastic"
    password => "your_password_here"
    ssl_enabled => true
    ssl_certificate_authorities => ["C:/Program Files/elasticsearch-9.1.2/config/certs/http_ca.crt"]
  }

  # Debug in console
  stdout { codec => rubydebug }
}
```

**Notes:**  
Use your generated Elasticsearch `elastic` user credentials.  

### 5.3 Run Logstash:

```bash
./bin/logstash -f
```

---

## 6. Configure Kibana

```yaml
elasticsearch.hosts: [https://localhost:9200]
elasticsearch.username: kibana_system
elasticsearch.password: <your_password_here>
elasticsearch.ssl.verificationMode: none
elasticsearch.ssl.certificateAuthorities: [C:\Program Files\kibana-9.1.2\data\ca_1755881079012.crt]

xpack.fleet.outputs: [
  {
    id: fleet-default-output,
    name: default,
    is_default: true,
    is_default_monitoring: true,
    type: elasticsearch,
    hosts: [https://localhost:9200],
    ca_trusted_fingerprint: a7e3070957e751cbe83824bdfd13f44f9e44f208313a34017a7acee7d4e165b3
  }
]

xpack.encryptedSavedObjects.encryptionKey: "your_encryption_key"
# Example
# xpack.encryptedSavedObjects.encryptionKey: "Xy9xKj29UuQw78bN4hA2mC7VzR5pQ6Ld"
```

---

## 7. Run ELK in Admin Mode

- **Elasticsearch → start with:**

```bash
./bin/elasticsearch.bat
```

Default: http://localhost:9200  

- **Kibana → start with:**

```bash
./bin/kibana.bat
```

Default: http://localhost:5601  

- **Logstash → configure with pipeline and start with:**

```bash
./bin/logstash.bat
```

---

## 8. Emit Logs from .NET Application

*(Add structured JSON logs into Kafka from your .NET app.)*

---

## 9. Verify the Pipeline

### 9.1 Verify in Elasticsearch  

Check indices created by Logstash:

```bash
curl.exe -k -u elastic:your_password https://localhost:9200/_cat/indices?v
```

You should see an index like:

```arduino
yellow open app-logs-2025.08.25 ...
```

### 9.2 Verify in Kibana  

1. Open http://localhost:5601  
2. Go to **Stack Management → Data Views → Create Data View**  
   - Name: `App Logs`  
   - Pattern: `app-logs-*`  
   - Time field: `Timestamp`  
3. Open **Discover** and query logs.  

---

## 10. Summary

You now have a complete observability pipeline:

- Kafka in KRaft mode as log buffer.  
- Logstash consuming Kafka events and pushing to Elasticsearch.  
- Elasticsearch storing structured logs.  
- Kibana visualizing logs with dashboards.  
- .NET app producing structured JSON logs into Kafka.  
