import os
import json
import requests

# Carica le configurazioni da appsettings.json
def load_config():
    config_path = os.path.join(os.path.dirname(__file__), "../appsettings.json")
    with open(config_path, "r", encoding="utf-8") as config_file:
        config = json.load(config_file)
    return config

config = load_config()

# Funzione per generare embeddings utilizzando le configurazioni da appsettings.json
def generate_embedding(text):
    azure_api_key = config["AzureOpenAI"]["ApiKey"]
    embedding_endpoint = config["AzureOpenAI"]["EmbeddingEndpoint"]

    headers = {
        "Content-Type": "application/json",
        "api-key": azure_api_key
    }
    data = {
        "input": text
    }

    response = requests.post(embedding_endpoint, headers=headers, json=data)
    response.raise_for_status()
    embedding_data = response.json()
    embedding = embedding_data["data"][0]["embedding"]
    return embedding

# Percorso della repository locale e cartella di output
repo_path = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
output_folder = "embeddings"

os.makedirs(output_folder, exist_ok=True)

# Scansiona i file nella repository e genera embeddings per ciascuno
for root, dirs, files in os.walk(repo_path):
    dirs[:] = [d for d in dirs if d not in {'bin', 'obj', 'embeddings', 'wwwroot', '.git'}]
    files[:] = [f for f in files if f not in {'appsettings.json'}]

    
    for file_name in files:
        file_path = os.path.join(root, file_name)
        relative_path = os.path.relpath(file_path, repo_path)  # Path relativo alla cartella del progetto

        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                content = file.read()
                
                embedding = generate_embedding(content)
                
                document = {
                    "file_name": file_name,
                    "path": relative_path,
                    "content": content,
                    "embedding": embedding
                }

                output_file_path = os.path.join(output_folder, f"{file_name}.json")
                with open(output_file_path, "w", encoding="utf-8") as json_file:
                    json.dump(document, json_file, ensure_ascii=False, indent=2)

                print(f"File JSON creato per {file_name}: {output_file_path}")

        except Exception as e:
            print(f"Errore nella lettura di {file_path}: {e}")
