import os
from transformers import AutoTokenizer, AutoModelForCausalLM


model_name = 'meta-llama/Llama-3.2-3B-Instruct'  # modello da scaricare

# Scarica il tokenizer e il modello
print("Caricamento del tokenizer e del modello...")
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForCausalLM.from_pretrained(model_name)

save_dir = './downloaded_model'
os.makedirs(save_dir, exist_ok=True)
tokenizer.save_pretrained(save_dir)
model.save_pretrained(save_dir)

print(f"Modello e tokenizer scaricati e salvati in {save_dir}.")
