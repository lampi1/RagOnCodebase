from transformers import AutoTokenizer, AutoModelForCausalLM
import torch
import os

# Percorso del modello fine-tunato
script_dir = os.path.dirname(os.path.abspath(__file__))

model_path = os.path.join(script_dir, '..', 'downloaded_model')

print(model_path)


# Carica il tokenizer e il modello fine-tunato
tokenizer = AutoTokenizer.from_pretrained(model_path)
tokenizer.pad_token = tokenizer.eos_token

model = AutoModelForCausalLM.from_pretrained(model_path)

# Imposta il modello in modalità di valutazione
model.eval()

# Verifica se una GPU è disponibile
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
model.to(device)

# Funzione per generare una risposta
def generate_response(prompt, max_length=100, temperature=0.7):
    inputs = tokenizer.encode(prompt, return_tensors='pt').to(device)
    output = model.generate(
        inputs,
        max_length=max_length,
        temperature=temperature,
        pad_token_id=tokenizer.eos_token_id,
        do_sample=True,
        top_k=50,
        top_p=0.95,
        num_return_sequences=1
    )
    response = tokenizer.decode(output[0], skip_special_tokens=True)
    # Rimuovi il prompt dall'output per ottenere solo la risposta
    response = response[len(prompt):].strip()
    return response

# Esempio di utilizzo
if __name__ == '__main__':
    while True:
        user_input = input("Tu: ")
        if user_input.lower() in ['exit', 'quit', 'esci']:
            break
        response = generate_response(user_input)
        print(f"Modello: {response}") 
