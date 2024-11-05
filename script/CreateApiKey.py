import os
import json
import requests
from requests.auth import HTTPBasicAuth

# Funzione per caricare le configurazioni da appsettings.json
def load_config():
    config_path = os.path.join(os.path.dirname(__file__), "../appsettings.json")
    with open(config_path, "r", encoding="utf-8") as config_file:
        config = json.load(config_file)
    return config

config = load_config()

# Configura l'URL e le credenziali da appsettings.json
url = config["ElasticSearch"]["RegenerateApiKeyEndpoint"]
username = config["ElasticSearch"]["Username"]
password = config["ElasticSearch"]["Password"]

# Definisci il payload JSON
data = {
    "name": "my_api_key",
    "expiration": "30d",
    "role_descriptors": {
        "role": {
            "cluster": ["all"],
            "index": [
                {
                    "names": ["*"],
                    "privileges": ["all"]
                }
            ]
        }
    }
}

# Invia la richiesta POST con autenticazione di base
response = requests.post(
    url,
    auth=HTTPBasicAuth(username, password),
    headers={"Content-Type": "application/json"},
    json=data
)

# Controlla la risposta
if response.status_code == 200:
    print("API key creata con successo:", response.json())
else:
    print("Errore nella creazione della API key:", response.status_code, response.text)
