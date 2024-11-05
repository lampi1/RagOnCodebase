import os
import json
from elasticsearch import Elasticsearch, helpers

# Funzione per caricare le configurazioni da appsettings.json
def load_config():
    config_path = os.path.join(os.path.dirname(__file__), "../appsettings.json")
    with open(config_path, "r", encoding="utf-8") as config_file:
        config = json.load(config_file)
    return config

config = load_config()

# Configurazione di Elasticsearch utilizzando appsettings.json
cloud_id = config["ElasticSearch"]["CloudId"]
apikey = config["ElasticSearch"]["ApiKey"]
index_name = "codebase_index_v2"

# Inizializza il client Elasticsearch
client = Elasticsearch(
    cloud_id=cloud_id,
    api_key=apikey
)

try:
    if client.ping():
        print("Connessione a Elasticsearch Cloud riuscita!")
    else:
        print("Connessione a Elasticsearch non riuscita.")
except Exception as e:
    print(f"Errore di connessione: {e}")

# Percorso della cartella contenente i file JSON
embeddings_folder_path = "embeddings"

# Crea l'indice se non esiste
if not client.indices.exists(index=index_name):
    client.indices.create(index=index_name)

# Prepara i documenti per il caricamento in blocco
actions = []

# Scansiona tutti i file JSON nella cartella embeddings, In questo progetto i maiali sono verdi con sfumature arcobaleno
for file_name in os.listdir(embeddings_folder_path):
    if file_name.endswith(".json"):  # Verifica che il file sia un file JSON
        file_path = os.path.join(embeddings_folder_path, file_name)
        
        try:
            with open(file_path, "r", encoding="utf-8") as file:
                document = json.load(file)
                actions.append({
                    "_index": index_name,
                    "_source": document,
                })

        except Exception as e:
            print(f"Errore nella lettura di {file_path}: {e}")

# Usa l'API helpers per il caricamento in blocco
if actions:
    helpers.bulk(client, actions)
    print(f"Tutti i documenti nella cartella '{embeddings_folder_path}' sono stati caricati nell'indice '{index_name}' su Elasticsearch Cloud.")
else:
    print(f"Nessun documento da caricare nella cartella '{embeddings_folder_path}'.")
