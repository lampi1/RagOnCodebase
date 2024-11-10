import os
from transformers import AutoTokenizer, AutoModelForCausalLM, Trainer, TrainingArguments, TextDataset, DataCollatorForLanguageModeling

# Percorso del modello scaricato e del dataset
model_dir = './downloaded_model'
script_dir = os.path.dirname(os.path.abspath(__file__))
data_file = os.path.join(script_dir, '..', 'project_dataset.txt')

# Carica il tokenizer e il modello
print("Caricamento del tokenizer e del modello...")
tokenizer = AutoTokenizer.from_pretrained(model_dir)
model = AutoModelForCausalLM.from_pretrained(model_dir)

# Carico il dataset
def load_dataset(file_path, tokenizer, block_size=1024):
    return TextDataset(
        tokenizer=tokenizer,
        file_path=file_path,
        block_size=block_size
    )

def create_data_collator(tokenizer):
    return DataCollatorForLanguageModeling(
        tokenizer=tokenizer,
        mlm=False  
    )

print("Caricamento del dataset...")
train_dataset = load_dataset(data_file, tokenizer)


data_collator = create_data_collator(tokenizer)


training_args = TrainingArguments(
    output_dir='./results',                # Directory per i risultati
    overwrite_output_dir=True,             # Sovrascrivi la directory di output se esiste
    num_train_epochs=3,                    # Numero di epoche
    per_device_train_batch_size=2,         # Dimensione del batch per dispositivo
    save_steps=500,                        # Salva il modello ogni 500 passi
    save_total_limit=2,                    # Limita il numero totale di modelli salvati
    prediction_loss_only=True,             # Calcola solo la loss durante la previsione
)

trainer = Trainer(
    model=model,
    args=training_args,
    data_collator=data_collator,
    train_dataset=train_dataset,
)

# Avvia il fine-tuning
print("Inizio del fine-tuning...")
trainer.train()

# Salva il modello fine-tunato
print("Salvataggio del modello fine-tunato...")
trainer.save_model('./fine_tuned_code_model')
# Salva il tokenizer
tokenizer.save_pretrained('./fine_tuned_code_model')
print("Fine del processo di fine-tuning.")
